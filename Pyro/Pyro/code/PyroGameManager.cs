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
        private static bool aiEnabled = true;
        public static bool AIEnabled { set { ClearScoreIntoLastScore(); aiEnabled = value; } get { return aiEnabled; } }
        public static bool aiZigZag = false;
        public static bool CanWalkOnFire = false;

        private const int FireDefaultLifetime = 0;//start length
        private static int BoardXOffset = 50;
        private static int BoardYOffset = 0;
        private const int GameWidthInSlots = 20;
        private const int GameHeightInSlots = 20;
        private const int GameSlotCount = GameHeightInSlots * GameWidthInSlots;
        private readonly VibrationConfig killVibration = new VibrationConfig(0.5f, 0.5f, 0.25f);
        private const float playerMoveTickDelay_Low = 0.20f;
        private const float playerMoveTickDelay_Med = 0.15f;
        private const float playerMoveTickDelay_High = 0.10f;
        private const float aiMoveTickDelay_Low = 0.05f;
        private const float aiMoveTickDelay_Med = 0.01f;
        private const float aiMoveTickDelay_High = 0.004f;
        private const float gravityTickDelay = 0.10f;
        private const float timePerDownPress = 0.05f;

        //HUD & score variables
        public static int Speed = 0;
        private static int scoreSinceLastFuel = 0;
        public static int Score = 0;
        public static int FuelCollected = 0;
        public static int LastScore = 0;
        public static int HighScore = 10;
        public static int AIHighScore = 10;

        //game slots - fixed and mobile
        private FixedSizeArray<GameSlot> tileSlots = new FixedSizeArray<GameSlot>(GameSlotCount);
        private FixedSizeArray<GameSlot> fires = new FixedSizeArray<GameSlot>(GameSlotCount);
        private FixedSizeArray<GameSlot> deadFires = new FixedSizeArray<GameSlot>(GameSlotCount);
        private FixedSizeArray<GameSlot> scanWorkspace = new FixedSizeArray<GameSlot>(4);
        private int[] scanScoreWorkspace = {0,0,0,0};
        private GameSlot playerSlot;
        private GameSlot fuelSlot;
        
        //input
        private PlayerController lastInput;
        private Point lastMoveDirection = new Point(0, 0);

        //timing and game logic
        public static GameState gameState;
        private float lastTickTime = -5000;
        private float activeMoveTickDelay = 0.01f;
        private Random random;
        private int fireDurration;

        private static bool trackMoveList = true;//never actualy used
        private List<string> moveLog = new List<string>(500);

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
            Reset();

            Speed = speed;
            if (AIEnabled)
            {
                speed = 1;
                switch (Speed)
                {
                    default:
                    case 0: activeMoveTickDelay = aiMoveTickDelay_Low; break;
                    case 1: activeMoveTickDelay = aiMoveTickDelay_Med; break;
                    case 2: activeMoveTickDelay = aiMoveTickDelay_High; break;
                }
            }
            else
            {
                switch (Speed)
                {
                    default:
                    case 0: activeMoveTickDelay = playerMoveTickDelay_Low; break;
                    case 1: activeMoveTickDelay = playerMoveTickDelay_Med; break;
                    case 2: activeMoveTickDelay = playerMoveTickDelay_High; break;
                }
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
            ClearScoreIntoLastScore();
            FuelCollected = 0;
            scoreSinceLastFuel = 0;

            ClearSlots(tileSlots);
            ClearSlots(fires);
            fires.Clear();
            ClearSlots(deadFires);
            deadFires.Clear();

            moveLog.Clear();

            lastMoveDirection = new Point(0, 0);

            lastInput = new PlayerController();
            playerSlot = new GameSlot(-1, -1);
            playerSlot.Contents = GameSlotStatus.Player;
        }

        private static void ClearScoreIntoLastScore()
        {
            LastScore = Score;
            Score = 0;
            if (AIEnabled)
            {
                if (LastScore > AIHighScore)
                    AIHighScore = LastScore;
            }
            else
            {
                if (LastScore > HighScore)
                    HighScore = LastScore;
            }
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
            gameCenter = new Point(19,19);
            playerSlot.SetPosition(gameCenter.X,gameCenter.Y);
            playerSlot.Setup(GameSlotStatus.Player, playerGameObject);
            GetGameSlot(gameCenter).Setup(GameSlotStatus.Player, null);

            //spawn facing right
            playerSlot.Child.facingDirection.X = 1;
            playerSlot.Child.facingDirection.Y = 0;

            playerGameObject.SetPosition(GetSlotLocation(playerSlot.Position));
        }

        private void SpawnFuel()
        {
            fuelSlot = GetRandomEmptySlot();
            if (fuelSlot == null)
            {
                //game has no empty tiles - shorted tail by 1 to allow space then try again
                AdjustFireDurration(-1);
                fuelSlot = GetRandomEmptySlot();

                Debug.Assert(fuelSlot != null, "Failed to spawn another fuel");
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

        private void SpawnFire(GameSlot fireSlot)
        {
            if (fireDurration > 0)
            {
                if (fireSlot.Contents == GameSlotStatus.Fire)
                {
                    fireSlot.Child.facingDirection = playerSlot.Child.facingDirection;
                    fireSlot.Child.life = fireDurration;
                }
                else
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
            }
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
                    if (percentLife > 0.8f)      sprite.PlayAnimation((int)FireAnimation.Fire100);
                    else if (percentLife > 0.6f) sprite.PlayAnimation((int)FireAnimation.Fire80);
                    else if (percentLife > 0.4f) sprite.PlayAnimation((int)FireAnimation.Fire60);
                    else if (percentLife > 0.2f) sprite.PlayAnimation((int)FireAnimation.Fire40);
                    else if (percentLife > 0.0f) sprite.PlayAnimation((int)FireAnimation.Fire20);
                    else                         sprite.PlayAnimation((int)FireAnimation.Fire0);
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

        private void ProcessInput(float gameTime)
        {
           
            PlayerController input = PyroGame.PlayerController;
            int xDif = 0;
            int yDif = 0;
            bool newInput = false;

            if (input.LeftPressed && !lastInput.LeftPressed)
            {
                //Move Left Pressed
                xDif = -1;
                newInput = true;
            }
            else if (input.RightPressed && !lastInput.RightPressed)
            {
                //Move Right Pressed
                xDif = 1;
                newInput = true;
            }
            else if (input.UpPressed && !lastInput.UpPressed)
            {
                //Move Up Pressed
                yDif = -1;
                newInput = true;
            }
            else if (input.DownPressed && !lastInput.DownPressed)
            {
                //Move Down Pressed
                yDif = 1;
                newInput = true;
            }

            if (newInput)
            {
                if (AIEnabled)
                {
                    //on any input tick ai 1
                    AITick();
                }
                else
                {
                    if (!isOpositeDirection(lastMoveDirection.X, lastMoveDirection.Y, xDif, yDif))
                    {
                        if (xDif != 0 || yDif != 0)
                        {
                            //not exact oposite direction
                            playerSlot.Child.facingDirection.X = xDif;
                            playerSlot.Child.facingDirection.Y = yDif;
                            if (!TimeBasedMovement)
                            {
                                MovePlayer(xDif, yDif);
                            }
                        }
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

        private GameSlot GetGameSlot(Point pt, int offsetX, int offsetY)
        {
            int newX = playerSlot.X + offsetX;
            int newY = playerSlot.Y + offsetY;

            if (newX < 0) newX += GameWidthInSlots;
            else newX %= GameWidthInSlots;

            if (newY < 0) newY += GameHeightInSlots;
            else newY %= GameHeightInSlots;

            return GetGameSlot(newX, newY);
        }

        private GameSlot GetNextGameSlot()
        {
            return GetGameSlot(playerSlot.Position, (int)playerSlot.Child.facingDirection.X, (int)playerSlot.Child.facingDirection.Y);
        }

        private void MovePlayer(int xDif, int yDif)
        {
            GameSlot oldSlot = GetGameSlot(playerSlot.Position);
            GameSlot newSlot = GetGameSlot(playerSlot.Position,xDif,yDif);

            bool spawnNewFuel = false;
            bool movePlayer = false;

            playerSlot.Child.facingDirection.X = xDif;
            playerSlot.Child.facingDirection.Y = yDif;

            if (newSlot.Contents == GameSlotStatus.Fuel)
            {
                ConsumeFuel(newSlot);
                spawnNewFuel = true;
                movePlayer = true;
            }
            KillFiresBy1();//has to be after consume fuel to make sure last peice of trail doesnt move when consuming
            if (newSlot.Contents == GameSlotStatus.Fire)
            {
                movePlayer = HitFire(newSlot);
            }
            else if (newSlot.Contents == GameSlotStatus.Empty)
            {
                movePlayer = true;
            }

            if (movePlayer)
            {
                if(trackMoveList)
                    moveLog.Add("moved (" + oldSlot.X + "," + oldSlot.Y + ") to (" + newSlot.X + "," + newSlot.Y + ")");

                UpdateScore(ScoredAction.Move);
                //create fire
                ClearSlot(oldSlot);
                SpawnFire(oldSlot);

                //actualy move player
                if (newSlot.Contents == GameSlotStatus.Fire)
                {
                    //kill fire it will be recreated
                    ClearSlot(newSlot);
                }
                newSlot.Contents = GameSlotStatus.Player;
                playerSlot.SetPosition(newSlot.Position);
                lastMoveDirection.X = xDif;
                lastMoveDirection.Y = yDif;
            }

            if(spawnNewFuel)
                SpawnFuel();

            GenFireReport();
        }

        private void ClearSlot(GameSlot slot)
        {
            if (slot.Contents == GameSlotStatus.Fire)
            {
                slot.Child.life = 0;
                ClearDeadFires();
            }
            slot.Contents = GameSlotStatus.Empty;//clears player from Slot
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

        private bool HitFire(GameSlot slot)
        {
            bool continueMoving = true;
            if(!CanWalkOnFire)
            {
                continueMoving = false;
                KillPlayer();
                gameState = GameState.GameOver;
            }
            return continueMoving;
        }

        private void KillPlayer()
        {
            playerSlot.Child.life = 0;
            playerSlot.Child = null;
            playerSlot = null;//This is just a link to the tileSlots

            SpawnDeadPlayer(lastMoveDirection.X, lastMoveDirection.Y);
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

        private GameSlot GetFuelSlot()
        {
            return fuelSlot;
        }

        private static Vector2 RotateSimpleVector(Vector2 start, int rightTurns)
        {
            rightTurns %= 4;
            if (rightTurns == 1) return new Vector2(-1 * start.Y, start.X);//rotate right :: x=-y y=x
            else if (rightTurns == 2) return new Vector2(-1 * start.X, -1 * start.Y);
            else if (rightTurns == 3) return new Vector2(start.Y, -1 * start.X);//rotate right :: x=y y=-x
            else return start;//0
        }

        private void AISmartTick()
        {
            int scanDepth = 5;
            Point bestMoveDir = AISmartScan(scanDepth);
            playerSlot.Child.facingDirection.X = bestMoveDir.X;
            playerSlot.Child.facingDirection.Y = bestMoveDir.Y;

            MovePlayer((int)playerSlot.Child.facingDirection.X, (int)playerSlot.Child.facingDirection.Y);
        }

        /// <summary>
        /// Scan Available moves and return best direction
        /// </summary>
        /// <param name="moves"></param>
        private Point AISmartScan(int moves)
        {
            Point result = new Point(lastMoveDirection.X, lastMoveDirection.Y);

            Point directionToGoal = DirectionToSlot(playerSlot,fuelSlot);
            Point distanceToGoal = DistanceToSlot(playerSlot,fuelSlot);
            Point correctDirectionToGoal = WrappedDirectionToSlot(playerSlot,fuelSlot);

            scanWorkspace.Clear();
            string[] scanDirectionNames = { "Right", "Down", "Left", "Up"};
            int[] scanXDirections = { 1, 0, -1, 0 };
            int[] scanYDirections = { 0, 1, 0, -1 };
            int highestScore = Int32.MinValue;
            int highestScoreIndex = 0;
            int score = 0;
            for (int xx = 0; xx < 4; xx++)
            {
                GameSlot option = GetGameSlot(playerSlot.Position, scanXDirections[xx], scanYDirections[xx]);
                scanWorkspace.Add(option);

                if (!option.IsSafeToWalkOn())
                {
                    score = -1000;//death here
                }
                else
                {

                    int scoreFactorX = 1;
                    int scoreFactorY = 1;
                    if (aiZigZag)
                    {
                        scoreFactorX = Math.Abs(correctDirectionToGoal.X) + scanXDirections[xx];
                        scoreFactorY = Math.Abs(correctDirectionToGoal.Y) + scanYDirections[xx];
                    }
                    //calc score
                    score = 0;
                    if (scanXDirections[xx] == correctDirectionToGoal.X && scanXDirections[xx] == 0)//stright line
                        score += 2 * scoreFactorX;
                    else if ((scanXDirections[xx] < 0 && correctDirectionToGoal.X < 0) || (scanXDirections[xx] > 0 && correctDirectionToGoal.X > 0))//correct direction
                        score += 1 * scoreFactorX;
                    else if ((scanXDirections[xx] < 0 && correctDirectionToGoal.X > 0) || (scanXDirections[xx] > 0 && correctDirectionToGoal.X < 0))//oposite dir
                        score += -1 * scoreFactorX;

                    if (scanYDirections[xx] == correctDirectionToGoal.Y && scanYDirections[xx] == 0)//stright line
                        score += 2 * scoreFactorY;
                    else if ((scanYDirections[xx] < 0 && correctDirectionToGoal.Y < 0) || (scanYDirections[xx] > 0 && correctDirectionToGoal.Y > 0))//correct direction
                        score += 1 * scoreFactorY;
                    else if ((scanYDirections[xx] < 0 && correctDirectionToGoal.Y > 0) || (scanYDirections[xx] > 0 && correctDirectionToGoal.Y < 0))//oposite dir
                        score += -1 * scoreFactorY;
                }


                scanScoreWorkspace[xx] = score;
                if (score > highestScore)
                {
                    highestScore = score;
                    highestScoreIndex = xx;
                }
            }

            if (trackMoveList)
                Console.WriteLine(string.Format("AI Move Scores from ({4},{5}) to ({6},{7}) ({0},{1},{2},{3}) - will Move {8}", scanScoreWorkspace[0], scanScoreWorkspace[1], scanScoreWorkspace[2], scanScoreWorkspace[3], playerSlot.X, playerSlot.Y, fuelSlot.X, fuelSlot.Y, scanDirectionNames[highestScoreIndex]));

            result.X = scanXDirections[highestScoreIndex];
            result.Y = scanYDirections[highestScoreIndex];
            return result;
        }

        private Point DistanceToSlot(GameSlot a, GameSlot b)
        {
            return new Point(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        private Point DirectionToSlot(GameSlot a, GameSlot b)
        {
            return new Point(b.X - a.X, b.Y - a.Y);
        }

        private Point WrappedDirectionToSlot(GameSlot a, GameSlot b)
        {
            int x = b.X - a.X;
            int y = b.Y - a.Y;
            if (x > GameWidthInSlots / 2) x -= GameWidthInSlots;
            if (y > GameHeightInSlots / 2) y -= GameHeightInSlots;
            if (x < GameWidthInSlots / -2) x += GameWidthInSlots;
            if (y < GameHeightInSlots / -2) y += GameHeightInSlots;
            return new Point(x,y);
        }

        private void AISimpleTick()
        {
            AISimplePlanMove();
            MovePlayer((int)playerSlot.Child.facingDirection.X, (int)playerSlot.Child.facingDirection.Y);
        }

        //basicly rewritten by AISmartScan(1) - kept for nostalsia sake
        private void AISimplePlanMove()
        {
            Point newDir = new Point (0,0);
            GameSlot fuelSlot = GetFuelSlot();

            if (fuelSlot.X > playerSlot.X) newDir.X = 1;
            else if (fuelSlot.X < playerSlot.X) newDir.X = -1;
            else if (fuelSlot.Y > playerSlot.Y) newDir.Y = 1;
            else if (fuelSlot.Y < playerSlot.Y) newDir.Y = -1;

            playerSlot.Child.facingDirection.X = newDir.X;
            playerSlot.Child.facingDirection.Y = newDir.Y;

            GameSlot newSlot = GetNextGameSlot();
            if (!newSlot.IsSafeToWalkOn())//fire in front
            {
                playerSlot.Child.facingDirection = RotateSimpleVector(playerSlot.Child.facingDirection, 3);
                newSlot = GetNextGameSlot();
                if (!newSlot.IsSafeToWalkOn())// fire to left
                {
                    playerSlot.Child.facingDirection = RotateSimpleVector(playerSlot.Child.facingDirection, 2);
                    newSlot = GetNextGameSlot();
                    if (!newSlot.IsSafeToWalkOn())// fire to right - check backwards
                    {
                        //check  straight
                        playerSlot.Child.facingDirection = RotateSimpleVector(playerSlot.Child.facingDirection, 1);
                        newSlot = GetNextGameSlot();
                        if (!newSlot.IsSafeToWalkOn())// fire to right - check backwards
                        {
                            //crash straight
                            playerSlot.Child.facingDirection.X = lastMoveDirection.X;
                            playerSlot.Child.facingDirection.Y = lastMoveDirection.Y;
                        }
                    }
                }
            }
        }

        private void AITick()
        {
            //AISimpleTick();
            AISmartTick();
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
                    if (gameTime - lastTickTime > activeMoveTickDelay)
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
                            if (AIEnabled)
                                AITick();
                            else 
                                MovePlayer((int)playerSlot.Child.facingDirection.X, (int)playerSlot.Child.facingDirection.Y);
                        }
                    }
                    break;
            }
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

        public bool IsSafeToWalkOn(int movesFromNow = 0)
        {
            if (!PyroGameManager.CanWalkOnFire && Contents == GameSlotStatus.Fire)
            {
                return Child.life < movesFromNow + 1;
            }
            else
                return true;
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
