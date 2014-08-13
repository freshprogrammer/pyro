//#define TestEnvironment

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Pyro
{
    class PyroGameManager : ObjectManager
    {
        //constant control variables
        public const int SlotSize = 32;

        private const int fireDurration = 4;//start length
        private static int BoardXOffset = 50;
        private static int BoardYOffset = 50;
        private const int GameWidthInSlots = 30;
        private const int GameHeightInSlots = 20;
        private const int GameSlotCount = GameHeightInSlots * GameWidthInSlots;
        private readonly VibrationConfig killVibration = new VibrationConfig(0.5f, 0.5f, 0.25f);
        private const float playerMoveTickDelay_Low = 0.55f;
        private const float playerMoveTickDelay_Med = 0.35f;
        private const float playerMoveTickDelay_High = 0.25f;
        private const float gravityTickDelay = 0.10f;
        private const float timePerDownPress = 0.05f;

        //HUD & score variables
        public static int Score = 0;
        public static int HighScore = 1000;
        public static int LevelNo = 0;
        public static int Speed = 0;
        public static int RemainingViruses = 0;

        //game slots - fixed and mobile
        private FixedSizeArray<GameSlot> slots = new FixedSizeArray<GameSlot>(GameSlotCount);
        private GameSlot playerSlot;
        
        //input
        private PlayerController lastInput;
        private float lastDownPressedTime = 0;

        //timing and game logic
        public static GameState gameState;
        private float lastTickTime = -5000;
        private float playerMoveTickDelay = playerMoveTickDelay_Med;
        private Random random;

        //assets
        private SoundEffect lockInSound;
        private SoundEffect killSound;
        private SoundEffect killVirusSound;
        private SoundEffect killLastVirusSound;


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
            killSound = sSystemRegistry.Game.Content.Load<SoundEffect>(@"sounds\shotgun_cock_01");
            killVirusSound = sSystemRegistry.Game.Content.Load<SoundEffect>(@"sounds\Sonic_Vanish");
            killLastVirusSound = sSystemRegistry.Game.Content.Load<SoundEffect>(@"sounds\Sonic_Continue");
            
            gameState = GameState.Loading;
        }

        public override void Reset()
        {

        }

        public FixedSizeArray<GameSlot> GenerateSlots()
        {
            int slotCount = GameWidthInSlots * (GameHeightInSlots);
            FixedSizeArray<GameSlot> result = new FixedSizeArray<GameSlot>(slotCount);

            for (int xx = 0; xx < slotCount; xx++)
            {
                int xPos = xx % GameWidthInSlots;
                int yPos = xx / GameWidthInSlots;

                GameObjectManager manager = sSystemRegistry.GameObjectManager;
                PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

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
        }

        private void SpawnPlayer()
        {
            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;

            GameObject playerGameObject = factory.SpawnPlayer(0,0);
            manager.Add(playerGameObject);

            playerSlot.SetPosition(GameWidthInSlots / 2 + 1, GameHeightInSlots / 2 + 1);
            playerSlot.Setup(GameSlotStatus.Player, playerGameObject);
            
            playerGameObject.SetPosition(GetSlotLocation(playerSlot.Position));
        }

        private void ProcessInput(float gameTime)
        {
            PlayerController input = PyroGame.PlayerController;
            if (input.LeftPressed && !lastInput.LeftPressed)
            {
                //Move Left Pressed
                MovePlayer(-1, 0);
            }
            else if (input.RightPressed && !lastInput.RightPressed)
            {
                //Move Right Pressed
                MovePlayer(1, 0);
            }
            else if (input.UpPressed && !lastInput.UpPressed)
            {
                //Move Up Pressed
                MovePlayer(0, -1);
            }
            else if (input.DownPressed && !lastInput.DownPressed)
            {
                //Move Down Pressed
                lastDownPressedTime = gameTime;
                MovePlayer(0, 1);
            }

            lastInput = PyroGame.PlayerController.Snapshot();
        }

        private bool IsCloseSlotEmpty(int xDif, int yDif, GameSlot slot)
        {
            Point newSlotPos = new Point(slot.X + xDif, slot.Y + yDif);
            if (newSlotPos.X >= 0 && newSlotPos.X <= GameWidthInSlots - 1)
            {
                if (newSlotPos.Y >= 0 && newSlotPos.Y <= GameHeightInSlots - 1)
                {
                    if (GetGameSlot(newSlotPos.X, newSlotPos.Y).IsEmpty)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void SpawnFireAtPlayer()
        {

        }

        private bool MovePlayer(int xDif, int yDif)
        {
            if (IsCloseSlotEmpty(xDif, yDif, playerSlot))
            {
                //create fire

                SpawnFireAtPlayer();

                playerSlot.Move(xDif, yDif);
                playerSlot.Child.facingDirection.X = xDif;
                playerSlot.Child.facingDirection.Y = yDif;
                return true;
            }
            return false;
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
                    }
                    break;
            }
        }

        private void PlayKillSound(int virusKills)
        {
            switch (virusKills)
            {
                case 0:
                    sSystemRegistry.SoundSystem.PlaySoundEffect(killSound, false);
                    break;
                default:
                case 1:
                    sSystemRegistry.SoundSystem.PlaySoundEffect(killVirusSound, false);
                    break;
                case -1:
                    sSystemRegistry.SoundSystem.PlaySoundEffect(killLastVirusSound, false);
                    break;
            }
        }

        public void MovePillSlotContents(int xDif, int yDif, GameSlot slot)
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

        private int GetPillSlotIndex(int x, int y)
        {
            return y * GameWidthInSlots + x % GameWidthInSlots;
        }

        private int GetPillSlotIndex(Point pt)
        {
            return GetPillSlotIndex(pt.X, pt.Y);
        }

        private GameSlot GetGameSlot(int x, int y)
        {
            if (y < 0 || x < 0 || y >= GameHeightInSlots || x >= GameWidthInSlots)//off game grid
                return null;
            return slots[GetPillSlotIndex(x, y)];
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

        public void Kill()
        {
            Child.FindByType<LifetimeComponent>().SetTimeUntilDeath(PyroGameObjectFactory.DeathAnimationDuration);
        }

        public void KillImedietly()
        {
            //SetPillEnd(PillEnd.Dead);
            if(Child!=null)
                Child.life = 0;
            //Pill.FindByType<LifetimeComponent>().SetTimeUntilDeath(0);
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
