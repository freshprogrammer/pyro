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

        private const int FireDefaultLifetime = 0;//start length
        private static int BoardXOffset = 50;
        private static int BoardYOffset = 0;
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
        public static int Speed = 0;
        private static int scoreSinceLastFuel = 0;
        public static int Score = 0;
        public static int FuelCollected = 0;
        public static int LastScore = 0;
        public static int HighScore = 10;

        //game slots - fixed and mobile
        private FixedSizeArray<GameSlot> tileSlots = new FixedSizeArray<GameSlot>(GameSlotCount);
        private FixedSizeArray<GameSlot> fires = new FixedSizeArray<GameSlot>(GameSlotCount);
        private FixedSizeArray<GameSlot> deadFires= new FixedSizeArray<GameSlot>(GameSlotCount);
        private GameSlot playerSlot;
        
        //input
        private PlayerController lastInput;
        private Point lastMoveDirection = new Point(0, 0);

        //timing and game logic
        public static GameState gameState;
        private float lastTickTime = -5000;
        private float playerMoveTickDelay = 0.01f;
        private Random random;
        private int fireDurration;

        public enum GameState
        {
            Loading,
            PlayerMoving,
            GameOver,
        }

        public PyroGameManager()
            : base()
        {
            random = new Random();
#if TestEnvironment
            random = new Random(2);
#endif
            Score = 0;
            tileSlots = GenerateSlots();
            playerSlot = new GameSlot(0, 0);

            //center tiles
            BoardXOffset = BoardXOffset + sSystemRegistry.ContextParameters.GameWidth / 2 - (GameWidthInSlots * SlotSize) / 2;
            BoardYOffset = BoardYOffset + sSystemRegistry.ContextParameters.GameHeight / 2 - (GameHeightInSlots * SlotSize) / 2;

            gameState = GameState.Loading;
        }

        public void StartGame(int level, int speed)
        {
            LastScore = Score;
            Reset();

            Speed = speed;
            switch (Speed)
            {
                default:
                case 0: playerMoveTickDelay = playerMoveTickDelay_Low; break;
                case 1: playerMoveTickDelay = playerMoveTickDelay_Med; break;
                case 2: playerMoveTickDelay = playerMoveTickDelay_High; break;
            }
            //totalPlayTime = new TimeSpan();

            StartLevel();
        }

        public void StartLevel()
        {
            FillNewLevel();

            fireDurration = FireDefaultLifetime;

            gameState = GameState.PlayerMoving;
        }

        public override void Reset()
        {
            if (LastScore > HighScore)
                HighScore = LastScore;
            Score = 0;
            FuelCollected = 0;
            scoreSinceLastFuel = 0;

            ClearSlots(tileSlots);
            ClearSlots(fires);
            fires.Clear();
            ClearSlots(deadFires);
            deadFires.Clear();

            lastMoveDirection = new Point(0, 0);

            lastInput = new PlayerController();
            playerSlot = new GameSlot(-1, -1);
            playerSlot.Contents = GameSlotStatus.Player;
        }

        private void ClearSlots(FixedSizeArray<GameSlot> slots)
        {
            foreach (GameSlot slot in slots)
            {
                if (slot.Child != null)
                {
                    slot.KillImedietly();
                    slot.Child = null;
                }
                slot.EmptySlot();
            }
        }

        public FixedSizeArray<GameSlot> GenerateSlots()
        {
            int slotCount = GameWidthInSlots * (GameHeightInSlots);
            FixedSizeArray<GameSlot> result = new FixedSizeArray<GameSlot>(slotCount);

            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

            for (int xx = 0; xx < slotCount; xx++)
            {
                int xPos = xx / GameWidthInSlots;
                int yPos = xx % GameWidthInSlots;

                GameObject emptyTile = factory.SpawnTileEmpty(xPos, yPos);
                manager.Add(emptyTile);

                GameSlot slot = new GameSlot(xPos, yPos);

                result.Add(slot);
            }

            return result;
        }

        public void SpawnLevelTiles()
        {
            foreach( GameSlot slot in tileSlots)
            {
                GameObjectManager manager = sSystemRegistry.GameObjectManager;
                PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

                GameObject tile = factory.SpawnTileEmpty(0,0);
                manager.Add(tile);

                slot.Setup(GameSlotStatus.Empty, null);

                tile.SetPosition(GetSlotLocation(slot.Position));
            }
        }

        public void FillNewLevel()
        {
            SpawnLevelTiles();
            //TODO build random level?

            SpawnPlayer();
            SpawnFuel();
        }

        private void SpawnPlayer()
        {
            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

            GameObject playerGameObject = factory.SpawnPlayer(0, 0);
            manager.Add(playerGameObject);

            //spawn at center
            Point gameCenter = new Point(GameWidthInSlots / 2, GameHeightInSlots / 2);
            playerSlot.SetPosition(gameCenter.X,gameCenter.Y);
            playerSlot.Setup(GameSlotStatus.Player, playerGameObject);
            GetGameSlot(gameCenter).Setup(GameSlotStatus.Player, null);

            //spawn facing right
            playerSlot.Child.facingDirection.X = 1;
            playerSlot.Child.facingDirection.Y = 0;

            playerGameObject.SetPosition(GetSlotLocation(playerSlot.Position));
        }

        private void SpawnFire(GameSlot fireSlot)
        {
            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

            GameObject fireGameObject = factory.SpawnFire(0, 0, fireDurration);
            manager.Add(fireGameObject);

            fireSlot.Setup(GameSlotStatus.Fire, fireGameObject);

            fireGameObject.SetPosition(GetSlotLocation(fireSlot.Position));
            fireGameObject.facingDirection = playerSlot.Child.facingDirection;

            fires.Add(fireSlot);
        }

        private void KillFiresBy1()
        {
            foreach (GameSlot fire in fires)
            {
                if (fire.Child != null)
                {//in case this fire is already dead somehome... cuz collision is turned off and only one fire per tile
                    fire.Child.life--;
                }
            }
            ClearDeadFires();
            UpdateFireAnimations();
        }

        private void ClearDeadFires()
        {
            foreach (GameSlot fire in fires)
            {
                if (fire.Child != null)
                {//in case this fire is already dead somehome... cuz collision is turned off and only one fire per tile
                    if (fire.Child.life <= 0)
                        deadFires.Add(fire);
                }
            }
            foreach (GameSlot fire in deadFires)
            {
                fires.Remove(fire, true);
                //clear slot
                fire.Child = null;
                fire.Contents = GameSlotStatus.Empty;
            }
            deadFires.Clear();
        }

        private void UpdateFireAnimations()
        {
            float percentLife = 0;
            foreach (GameSlot fire in fires)
            {
                if (fire.Child != null)
                {
                    var sprite = fire.Child.FindByType<SpriteComponent>();

                    percentLife = 1f * fire.Child.life / fireDurration;
                    if (percentLife > 0.9f) sprite.PlayAnimation((int)FireAnimation.Fire100);
                    else if (percentLife > 0.8f) sprite.PlayAnimation((int)FireAnimation.Fire90);
                    else if (percentLife > 0.7f) sprite.PlayAnimation((int)FireAnimation.Fire80);
                    else if (percentLife > 0.6f) sprite.PlayAnimation((int)FireAnimation.Fire70);
                    else if (percentLife > 0.5f) sprite.PlayAnimation((int)FireAnimation.Fire60);
                    else if (percentLife > 0.4f) sprite.PlayAnimation((int)FireAnimation.Fire50);
                    else if (percentLife > 0.3f) sprite.PlayAnimation((int)FireAnimation.Fire40);
                    else if (percentLife > 0.2f) sprite.PlayAnimation((int)FireAnimation.Fire30);
                    else if (percentLife > 0.1f) sprite.PlayAnimation((int)FireAnimation.Fire20);
                    else if (percentLife > 0)    sprite.PlayAnimation((int)FireAnimation.Fire10);
                }
            }
        }

        /* Picks a random tile from the list, then returns first empty tile from there
         */
        private GameSlot GetRandomEmptySlot()
        {
            int max = tileSlots.Count-1;
            int rndStart = random.Next(max);
            int rnd = rndStart;
            
            int tries = 0;
            while(tries<=max)
            {
                if(tileSlots[rnd].Contents==GameSlotStatus.Empty)
                    return tileSlots[rnd];
                else
                {
                    rnd = (++rnd)%(max+1);
                }
                tries++;
            }
            return null;
        }

        private void SpawnDeadPlayer(int facingX, int facingY)
        {
            //stub - should happen automaticly via lifetime component
            //playerSlot.Child.life = 0;
            //playerSlot.Child = 
        }

        private void SpawnFuel()
        {
            GameSlot fuelSlot = GetRandomEmptySlot();
            if (fuelSlot == null)
            {
                //game has no empty tiles - shorted tail by 1 to allow space then try again
                AdjustFireDurration(-1);
                fuelSlot = GetRandomEmptySlot();

                Debug.Assert(fuelSlot !=null, "Failed to spawn another fuel");
                //Need to test this
            }

            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

            GameObject fuelGameObject = factory.SpawnFuel(0, 0);
            manager.Add(fuelGameObject);

            //playerSlot.Setup(GameSlotStatus.Player, fireGameObject);
            fuelSlot.Setup(GameSlotStatus.Fuel, fuelGameObject);

            fuelGameObject.SetPosition(GetSlotLocation(fuelSlot.Position));

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

            if (!isOpositeDirection(lastMoveDirection.X, lastMoveDirection.Y, xDif, yDif))
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

            GameSlot oldSlot = GetGameSlot(playerSlot.Position);
            GameSlot newSlot = GetGameSlot(newX, newY);

            bool spawnNewFuel = false;
            bool moved = false;

            playerSlot.Child.facingDirection.X = xDif;
            playerSlot.Child.facingDirection.Y = yDif;

            if (newSlot.Contents == GameSlotStatus.Fuel)
            {
                ConsumeFuel(newSlot);
                spawnNewFuel = true;
                moved = true;
            }
            KillFiresBy1();//has to be after consume fuel to make sure last peice of trail doesnt move when consuming
            if (newSlot.Contents == GameSlotStatus.Fire)
            {
                HitFire(newSlot);

                SpawnDeadPlayer(xDif, yDif);
            }
            else if (newSlot.Contents == GameSlotStatus.Empty)
            {
                moved = true;
            }

            if (moved)
            {
                UpdateScore(ScoredAction.Move);
                //move player
                //create fire
                oldSlot.Contents = GameSlotStatus.Empty;
                if(fireDurration>0)
                    SpawnFire(oldSlot);
                newSlot.Contents = GameSlotStatus.Player;
                playerSlot.SetPosition(newSlot.Position);
                lastMoveDirection.X = xDif;
                lastMoveDirection.Y = yDif;
            }

            if(spawnNewFuel)
                SpawnFuel();

            GenFireReport();
        }

        private void GenFireReport()
        {
            int fires = 0;

            for (int xx = 0; xx < 16; xx++)
            {
                fires = 0;
                foreach (GameSlot s in tileSlots)
                {
                    if (s.Contents == GameSlotStatus.Fire && s.Child.life==xx)
                        fires++;
                }
                if(fires>1)
                    Console.WriteLine("{0} fires at {1} life", fires, xx);
            }
        }

        private void HitFire(GameSlot slot)
        {
            bool liveAfterFire = false;
            if (liveAfterFire)
            {
                slot.Child.life = 0;
            }
            else
            {
                KillPlayer();
                gameState = GameState.GameOver;
            }
        }

        private void KillPlayer()
        {
            playerSlot.Child.life = 0;
            playerSlot.Child = null;
            playerSlot = null;//TODO this should peoably be a reset and not a null
        }

        public bool AdjustFireDurration(int delta)
        {
            int oldFireDurration = fireDurration;
            int reserved = 3;
            fireDurration += delta;
            if (GameSlotCount - fireDurration < reserved)
                fireDurration = GameSlotCount - reserved;//minus 3 for player and 1 additional food and 1 blank space
            else if (fireDurration < 0)
                fireDurration = 0;
            Debug.Assert(fireDurration <= GameSlotCount - reserved);
            foreach (GameSlot fire in fires)
            {
                fire.Child.life += delta;
            }
            if(delta<0)
                ClearDeadFires();
            UpdateFireAnimations();
            return oldFireDurration == fireDurration;
        }

        private void ConsumeFuel(GameSlot slot)
        {
            FuelCollected++;
            AdjustFireDurration(1);
            UpdateScore(ScoredAction.CollectFuel);

            slot.Child.life--;
            if (slot.Child.life == 0)
            {
                slot.Child = null;
                slot.Contents = GameSlotStatus.Empty;
            }
        }

        public override void Update(float timeDelta, BaseObject parent)
        {
            if (gameState != GameState.Loading)
            {
                float gameTime = sSystemRegistry.TimeSystem.getGameTime();

                if (gameState == GameState.PlayerMoving)
                {
                    //processInput must be outsude the tick so it can be called faster for faster controls and quick dropping
                    ProcessInput(gameTime);
                    if (gameTime - lastTickTime > playerMoveTickDelay)
                    {
                        lastTickTime = gameTime;
                        Tick();
                    }
                }
                else if(gameState==GameState.GameOver)
                {
                    //do nothing
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

        private void UpdateScore(ScoredAction action)
        {
            switch (action)
            {
                case ScoredAction.Move:
                    if (scoreSinceLastFuel < FuelCollected)
                    {
                        Score--;//+1 each move up to the tail length
                        scoreSinceLastFuel++;
                    }
                    break;
                case ScoredAction.CollectFuel:
                    scoreSinceLastFuel = 0;
                    Score+=2*FuelCollected;
                    break;
            }
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
            return x * GameWidthInSlots + y % GameWidthInSlots;
        }

        private int GetGameSlotIndex(Point pt)
        {
            return GetGameSlotIndex(pt.X, pt.Y);
        }

        private GameSlot GetGameSlot(int x, int y)
        {
            if (y < 0 || x < 0 || y >= GameHeightInSlots || x >= GameWidthInSlots)//off game grid
                return null;
            return tileSlots[GetGameSlotIndex(x, y)];
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
        Fuel,
        Empty,
    }

    enum ScoredAction
    {
        Move,
        CollectFuel,
    }

    class GameSlot
    {
        public static GameSlot Blank = new GameSlot(0, 0);

        public GameObject Child;
        public GameSlotStatus Contents = GameSlotStatus.Empty;
        private Point position = Point.Zero;

        public bool IsEmpty { get { return Contents == GameSlotStatus.Empty; } }

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
            Contents = GameSlotStatus.Empty;
        }

        public void Setup(GameSlotStatus type, GameObject o)
        {
            Child = o;
            this.Contents = type;
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
            Contents = src.Contents;
        }

        public GameSlot Clone()
        {
            GameSlot result = new GameSlot(position.X, position.Y);
            result.Child = Child;
            result.Contents = Contents;
            return result;
        }

        public void TransferSlotFrom(GameSlot src)
        {
            Child = src.Child;
            Contents = src.Contents;
            src.EmptySlot();

            Child.SetPosition(PyroGameManager.GetSlotLocation(X, Y));
        }

        public override string ToString()
        {
            if (Child != null)
                return "GameSlot(" + X + "," + Y + "," + Contents + ") - Life:" + Child.life;
            else
                return "GameSlot(" + X + "," + Y + "," + Contents + ")";
        }
    }
}
