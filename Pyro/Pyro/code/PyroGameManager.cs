//#define TestEnvironment

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;

namespace Pyro
{
    class PyroGameManager : ObjectManager
    {
        //constant control variables
        public const int SlotSize = 32;
        public static bool TimeBasedMovement = true;

        private const int FireDefaultLifetime = 20;//start length
        private static int BoardXOffset = 50;
        private static int BoardYOffset = 50;
        private const int GameWidthInSlots = 20;
        private const int GameHeightInSlots = 20;
        private const int GameSlotCount = GameHeightInSlots * GameWidthInSlots;
        private readonly VibrationConfig killVibration = new VibrationConfig(0.5f, 0.5f, 0.25f);
        private const float playerMoveTickDelay_Low = 0.55f;
        private const float playerMoveTickDelay_Med = 0.15f;
        private const float playerMoveTickDelay_High = 0.25f;
        private const float gravityTickDelay = 0.10f;
        private const float timePerDownPress = 0.05f;

        //HUD & score variables
        public static int Score = 0;
        public static int HighScore = 1000;
        public static int LevelNo = 0;
        public static int Speed = 0;

        //game slots - fixed and mobile
        private FixedSizeArray<GameSlot> slots = new FixedSizeArray<GameSlot>(GameSlotCount);
        private FixedSizeArray<GameSlot> fires = new FixedSizeArray<GameSlot>(GameSlotCount);
        private FixedSizeArray<GameSlot> deadFires= new FixedSizeArray<GameSlot>(GameSlotCount);
        private GameSlot playerSlot;
        
        //input
        private PlayerController lastInput;

        //timing and game logic
        public static GameState gameState;
        private float lastTickTime = -5000;
        private float playerMoveTickDelay = 0.01f;
        private Random random;
        private int fireDurration;

        //assets
        private SoundEffect lockInSound;


        public enum GameState
        {
            Loading,
            PlayerMoving,
        }

        public PyroGameManager()
            : base()
        {
            random = new Random();
#if TestEnvironment
            random = new Random(2);
#endif
            slots = GenerateSlots();

            //center tiles
            BoardXOffset = sSystemRegistry.ContextParameters.GameWidth / 2 - (GameWidthInSlots * SlotSize)/2;
            BoardYOffset = sSystemRegistry.ContextParameters.GameHeight / 2 - (GameHeightInSlots * SlotSize) / 2;

            playerSlot = new GameSlot(0, 0);

            lockInSound = sSystemRegistry.Game.Content.Load<SoundEffect>(@"sounds\button-3");
            
            gameState = GameState.Loading;
        }

        public override void Reset()
        {

        }

        public FixedSizeArray<GameSlot> GenerateSlots()
        {
            int slotCount = GameWidthInSlots * (GameHeightInSlots);
            FixedSizeArray<GameSlot> result = new FixedSizeArray<GameSlot>(slotCount);

            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

            for (int xx = 0; xx < slotCount; xx++)
            {
                int xPos = xx % GameWidthInSlots;
                int yPos = xx / GameWidthInSlots;

                GameObject emptyTile = factory.SpawnTileEmpty(xPos, yPos);
                manager.Add(emptyTile);

                GameSlot slot = new GameSlot(xPos, yPos);

                result.Add(slot);
            }

            return result;
        }

        public void SpawnLevelTiles()
        {
            foreach( GameSlot slot in slots)
            {
                GameObjectManager manager = sSystemRegistry.GameObjectManager;
                PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

                GameObject tile = factory.SpawnTileEmpty(0,0);
                manager.Add(tile);

                slot.Setup(GameSlotStatus.Empty, tile);

                tile.SetPosition(GetSlotLocation(slot.Position));
            }
        }

        public void FillNewLevel(int LevelNo)
        {
            SpawnPlayer();

            SpawnLevelTiles();
            //TODO build random level?

            SpawnFood();
        }

        private void SpawnPlayer()
        {
            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

            GameObject playerGameObject = factory.SpawnPlayer(0, 0);
            manager.Add(playerGameObject);

            //spawn at center
            playerSlot.SetPosition(GameWidthInSlots / 2, GameHeightInSlots / 2);
            playerSlot.Setup(GameSlotStatus.Player, playerGameObject);

            //spawn facing right
            playerSlot.Child.facingDirection.X = 1;
            playerSlot.Child.facingDirection.Y = 0;

            playerGameObject.SetPosition(GetSlotLocation(playerSlot.Position));
        }

        private void SpawnFireAtPlayer()
        {
            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

            GameObject fireGameObject = factory.SpawnFire(0, 0, fireDurration);
            manager.Add(fireGameObject);

            GameSlot fireSlot = GetGameSlot(playerSlot.Position);
            fireSlot.Child = fireGameObject;
            fireSlot.Type = GameSlotStatus.Fire;

            fireGameObject.SetPosition(GetSlotLocation(playerSlot.Position));

            fires.Add(fireSlot);
        }

        private void KillFiresBy1()
        {
            foreach (GameSlot fire in fires)
            {
                if (fire.Child != null)
                {//in case this fire is already dead somehome... cuz collision is turned off and only one fire per tile
                    fire.Child.life--;
                    if (fire.Child.life <= 0)
                        deadFires.Add(fire);
                }
            }
            foreach (GameSlot fire in deadFires)
            {
                fires.Remove(fire,true);
                //clear slot
                fire.Child = null;
                fire.Type = GameSlotStatus.Empty;
            }
            deadFires.Clear();
        }


        /* Picks a random tile from the list, then returns first empty tile from there
         */
        private GameSlot GetRandomEmptySlot()
        {
            int max = slots.Count-1;
            int rndStart = random.Next(max);
            int rnd = rndStart;
            
            int tries = 0;
            while(tries<max)
            {
                if(slots[rnd].Type==GameSlotStatus.Empty)
                    return slots[rnd];
                else
                {
                    rnd = (++rnd)%max;
                }
                tries++;
            }
            return null;
        }

        private void SpawnFood()
        {
            GameSlot foodSlot = GetRandomEmptySlot();
            if (foodSlot == null)
            {
                //game has no empty tiles - shorted tail by 1 to allow space then try again
                AdjustFireDurration(-1);
                foodSlot = GetRandomEmptySlot();

                Debug.Assert(foodSlot !=null, "Failed to spawn another food");
                //Need to test this
            }

            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

            GameObject foodGameObject = factory.SpawnFood(0, 0);
            manager.Add(foodGameObject);

            //playerSlot.Setup(GameSlotStatus.Player, fireGameObject);
            foodSlot.Child = foodGameObject;
            foodSlot.Type = GameSlotStatus.Food;

            foodGameObject.SetPosition(GetSlotLocation(foodSlot.Position));

            //fires.Add(fireSlot);
        }

        private void ProcessInput(float gameTime)
        {
            PlayerController input = PyroGame.PlayerController;
            int xDif = 0;
            int yDif = 0;
            
            if (input.LeftPressed && !lastInput.LeftPressed)
            {
                //Move Left Pressed
                xDif = -1;
            }
            else if (input.RightPressed && !lastInput.RightPressed)
            {
                //Move Right Pressed
                xDif = 1;
            }
            else if (input.UpPressed && !lastInput.UpPressed)
            {
                //Move Up Pressed
                yDif = -1;
            }
            else if (input.DownPressed && !lastInput.DownPressed)
            {
                //Move Down Pressed
                yDif = 1;
            }
            
            if(!isOpositeDirection(playerSlot.Child.facingDirection.X,playerSlot.Child.facingDirection.Y,xDif,yDif))
            {
                if(xDif!=0 || yDif!=0)
                {
                    //not exact oposite direction
                    playerSlot.Child.facingDirection.X = xDif;
                    playerSlot.Child.facingDirection.Y = yDif;
                    if(!TimeBasedMovement)
                    {
                        MovePlayer(xDif, yDif);
                    }
                }
            }

            lastInput = PyroGame.PlayerController.Snapshot();
        }

        private static bool isOpositeDirection(Point pt, Point pt2)
        {
            return (pt.X * -1 == pt2.X && pt.Y * -1 == pt2.Y);
        }

        private static bool isOpositeDirection(float x1, float y1, float x2, float y2)
        {
            return (x1 * -1 ==x2 && y1 * -1 ==y2);
        }

        private void MovePlayer(int xDif, int yDif)
        {
            
            int newX = playerSlot.X + xDif;
            int newY = playerSlot.Y + yDif;

            if (newX < 0) newX += GameWidthInSlots;
            else newX %= GameWidthInSlots;

            if (newY < 0) newY += GameHeightInSlots;
            else newY %= GameHeightInSlots;

            GameSlot newSlot = GetGameSlot(newX, newY);
            if (newSlot.Type == GameSlotStatus.Food)
            {
                EatFood(newSlot);
            }
            KillFiresBy1();
            if (newSlot.Type == GameSlotStatus.Fire)
            {
                HitFire(newSlot);
            }

            //create fire
            SpawnFireAtPlayer();

            playerSlot.SetPosition(newSlot.Position);
            playerSlot.Child.facingDirection.X = xDif;
            playerSlot.Child.facingDirection.Y = yDif;
        }

        private void HitFire(GameSlot slot)
        {
            slot.Child.life = 0;
        }

        private void AdjustFireDurration(int delta)
        {
            fireDurration += delta;
            foreach (GameSlot fire in fires)
            {
                fire.Child.life += delta;
            }
        }

        private void EatFood(GameSlot slot)
        {
            AdjustFireDurration(1);
            Score++;

            slot.Child.life--;
            if (slot.Child.life == 0)
            {
                slot.Child = null;
                slot.Type = GameSlotStatus.Empty;
            }

            SpawnFood();
        }

        public override void Update(float timeDelta, BaseObject parent)
        {
            if (gameState != GameState.Loading)
            {
                float gameTime = sSystemRegistry.TimeSystem.getGameTime();

                float timePerTick;
                if (gameState == GameState.PlayerMoving)
                {
                    //processInput must be outsude the tick so it can be called faster for faster controls and quick dropping
                    ProcessInput(gameTime);
                    timePerTick = playerMoveTickDelay;
                }
                else
                {
                    timePerTick = gravityTickDelay;
                }

                if (gameTime - lastTickTime > timePerTick)
                {
                    lastTickTime = gameTime;
                    Tick();
                }
            }
        }

        public void Tick()
        {
            //move player till cant anymore
            
            switch (gameState)
            {
                case GameState.Loading:
                    //do nothing
                    break;
                case GameState.PlayerMoving:
                    //if (!MovePlayer(gravityVector.X, gravityVector.Y, playerSlot1, playerSlot2))
                    {
                        //Player hit something - game over
                        //game over
                        if (TimeBasedMovement)
                        {
                            MovePlayer((int)playerSlot.Child.facingDirection.X, (int)playerSlot.Child.facingDirection.Y);
                        }
                    }
                    break;
            }
        }

        public void MoveGameSlotContents(int xDif, int yDif, GameSlot slot)
        {
            GameSlot newSlot = GetGameSlot(slot.X + xDif, slot.Y + yDif);
            newSlot.TransferSlotFrom(slot);
        }

        private void CalcScore()
        {
            Score = -1;

            if (Score > HighScore)
                HighScore = Score;
        }

        public void LevelOver(bool fail)
        {
            if (!fail)
            {
                //advance to next level
                LevelNo++;
                StartLevel();
            }
            else
            {
                //failed level
                Score = 0;
                gameState = GameState.Loading;
            }
        }

        public void UpdateScore()
        {
            CalcScore();
        }

        private void ClearSlots()
        {
            foreach (GameSlot slot in slots)
            {
                if (slot.Child != null)
                {
                    slot.KillImedietly();
                }
                slot.EmptySlot();
            }
        }

        public void StartGame(int level, int speed)
        {
            Score = 0;
            LevelNo = level;
            Speed = speed;


            switch (Speed)
            {
                default:
                case 0: playerMoveTickDelay = playerMoveTickDelay_Low; break;
                case 1: playerMoveTickDelay = playerMoveTickDelay_Med; break;
                case 2: playerMoveTickDelay = playerMoveTickDelay_High; break;
            }
            //totalPlayTime = new TimeSpan();

            lastInput = new PlayerController();

            StartLevel();
        }

        public void StartLevel()
        {
            ClearSlots();
            FillNewLevel(LevelNo);

            fireDurration = FireDefaultLifetime;

            gameState = GameState.PlayerMoving;
        }

        public static string GetSpeedName(int speed)
        {
            switch (speed)
            {
                default:
                case 0: return "Low";
                case 1: return "Med";
                case 2: return "High";
            }
        }

        private int GetGameSlotIndex(int x, int y)
        {
            return y * GameWidthInSlots + x % GameWidthInSlots;
        }

        private int GetGameSlotIndex(Point pt)
        {
            return GetGameSlotIndex(pt.X, pt.Y);
        }

        private GameSlot GetGameSlot(int x, int y)
        {
            if (y < 0 || x < 0 || y >= GameHeightInSlots || x >= GameWidthInSlots)//off game grid
                return null;
            return slots[GetGameSlotIndex(x, y)];
        }

        private GameSlot GetGameSlot(Point pt)
        {
            return GetGameSlot(pt.X, pt.Y);
        }

        public static Vector2 GetSlotLocation(int x, int y)
        {
            return new Vector2(BoardXOffset + x * SlotSize, BoardYOffset + y * SlotSize);
        }

        public static Vector2 GetSlotLocation(Point pt)
        {
            return GetSlotLocation(pt.X, pt.Y);
        }
    }

    enum GameSlotStatus
    {
        Player,
        Fire,
        Food,
        Empty,
    }

    class GameSlot
    {
        public static GameSlot Blank = new GameSlot(0, 0);

        public GameObject Child;
        public GameSlotStatus Type = GameSlotStatus.Empty;
        private Point position = Point.Zero;

        public bool IsEmpty { get { return Type == GameSlotStatus.Empty; } }

        public int X { get { return position.X; } }
        public int Y { get { return position.Y; } }
        public Point Position { get { return position; } }

        public GameSlot(int x, int y)
        {
            position.X = x;
            position.Y = y;
        }

        public void SetGameObject(GameObject o)
        {
            Child = o;
        }

        public void EmptySlot()
        {
            Child = null;
            Type = GameSlotStatus.Empty;
        }

        public void Setup(GameSlotStatus type, GameObject o)
        {
            Child = o;
            this.Type = type;
        }

        public void Move(int xDif, int yDif)
        {
            position.X += xDif;
            position.Y += yDif;
            if (Child != null)
            {
                Child.SetPosition(PyroGameManager.GetSlotLocation(position));
            }
        }

        public void SetPosition(Point pt)
        {
            SetPosition(pt.X, pt.Y);
        }

        public void SetPosition(int x, int y)
        {
            position.X = x;
            position.Y = y;

            if (Child != null)
            {
                Child.SetPosition(PyroGameManager.GetSlotLocation(position));
            }
        }

        public void KillImedietly()
        {
            if(Child!=null)
                Child.life = 0;
        }

        public void CloneFrom(GameSlot src)
        {
            position.X = src.position.X;
            position.Y = src.position.Y;
            Child = src.Child;
            Type = src.Type;
        }

        public GameSlot Clone()
        {
            GameSlot result = new GameSlot(position.X, position.Y);
            result.Child = Child;
            result.Type = Type;
            return result;
        }

        public void TransferSlotFrom(GameSlot src)
        {
            Child = src.Child;
            Type = src.Type;
            src.EmptySlot();

            Child.SetPosition(PyroGameManager.GetSlotLocation(X, Y));
        }

        public override string ToString()
        {
            return "GameSlot(" + X + "," + Y + "," + Type + ")";
        }
    }
}
