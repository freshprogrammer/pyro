//#define TestEnvironment

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Snake
{
    class DrMarioGameManager : ObjectManager
    {
        //constant control variables
        public const int SlotSize = 45;
        private const int ColorCodeCount = 3;
        private const int MatchesNeededToKill = 4;
        private const int BoardXOffset = 465;
        private const int BoardYOffset = 26;
        private const int GameWidthInSlots = 8;
        private const int GameHeightInSlots = 15;
        private const int GameSlotCount = GameHeightInSlots * GameWidthInSlots;
        private readonly Point futurePillSetLocation = new Point(1000, 100);
        private readonly Point playPillDropLocation = new Point(3, 0);
        private readonly Point gravityVector = new Point(0, 1);
        private readonly VibrationConfig killVibration = new VibrationConfig(0.5f, 0.5f, 0.25f);
        private const float playerPillDropTickDelay_Low = 0.55f;
        private const float playerPillDropTickDelay_Med = 0.35f;
        private const float playerPillDropTickDelay_High = 0.25f;
        private const float gravityTickDelay = 0.30f;
        private const float timePerDownPress = 0.05f;

        //HUD & score variables
        public static int Score = 0;
        public static int HighScore = 1000;
        public static int LevelNo = 0;
        public static int Speed = 0;
        public static int RemainingViruses = 0;
        private int topSpace = 4;
        private int virusKillsThisPill = 0;

        //game slots - fixed and mobile
        private FixedSizeArray<PillSlot> slots = new FixedSizeArray<PillSlot>(GameSlotCount);
        private PillSlot futurePill1;
        private PillSlot futurePill2;
        private PillSlot playerPill1;
        private PillSlot playerPill2;
        private PillSlot workspacePill1;
        private PillSlot workspacePill2;
        private PillSlot workspacePill3;
        private FixedSizeArray<PillSlot> activePills;
        private FixedSizeArray<PillSlot> killWorkspace;
        private FixedSizeArray<PillSlot> killScanWorkspace;

        //input
        private PlayerController lastInput;
        private float lastDownPressedTime = 0;
        private bool moveAgainstGravity = false;

        //timing and game logic
        public static GameState gameState;
        private float lastTickTime = -5000;
        private float playerPillDropTickDelay = playerPillDropTickDelay_Med;
        private Random random;

        //assets
        private SoundEffect lockInSound;
        private SoundEffect killSound;
        private SoundEffect killVirusSound;
        private SoundEffect killLastVirusSound;


        public enum GameState
        {
            Loading,
            PlayerPillDrop,
            PlayerPillActive,
            CheckKills,
            GravityStep,
        }

        public DrMarioGameManager()
            : base()
        {
            random = new Random();
#if TestEnvironment
            random = new Random(2);
#endif
            slots = GenerateSlots();
            playerPill1 = new PillSlot(0, 0);
            playerPill2 = new PillSlot(0, 0);
            futurePill1 = new PillSlot(0, 0);
            futurePill2 = new PillSlot(0, 0);
            workspacePill1 = new PillSlot(0, 0);
            workspacePill2 = new PillSlot(0, 0);
            workspacePill3 = new PillSlot(0, 0);

            activePills = new FixedSizeArray<PillSlot>(GameSlotCount);
            killWorkspace = new FixedSizeArray<PillSlot>(GameSlotCount);
            killScanWorkspace = new FixedSizeArray<PillSlot>(GameHeightInSlots);//longest possible single direction

            lockInSound = sSystemRegistry.Game.Content.Load<SoundEffect>(@"sounds\button-3");
            killSound = sSystemRegistry.Game.Content.Load<SoundEffect>(@"sounds\shotgun_cock_01");
            killVirusSound = sSystemRegistry.Game.Content.Load<SoundEffect>(@"sounds\Sonic_Vanish");
            killLastVirusSound = sSystemRegistry.Game.Content.Load<SoundEffect>(@"sounds\Sonic_Continue");
            
            gameState = GameState.Loading;
        }

        public override void Reset()
        {

        }

        public FixedSizeArray<PillSlot> GenerateSlots()
        {
            int slotCount = GameWidthInSlots * (GameHeightInSlots);
            FixedSizeArray<PillSlot> result = new FixedSizeArray<PillSlot>(slotCount);

            for (int xx = 0; xx < slotCount; xx++)
            {
                int xPos = xx % GameWidthInSlots;
                int yPos = xx / GameWidthInSlots;
                PillSlot s = new PillSlot(xPos, yPos);
                result.Add(s);
            }

            return result;
        }

        public void FillNewLevel(int LevelNo)
        {
            int virusCount = (LevelNo + 1) * 4;
            //TODO handle max level when there are no more empty slots for viruses

#if TestEnvironment
            //4 virus test
            //SpawnVirus(1, 10, 1);
            //SpawnVirus(1, 11, 1);
            //SpawnVirus(1, 12, 1);
            //SpawnVirus(1, 13, 1);


            //SpawnVirus(1, 9,  PillColor.Red);
            //SpawnVirus(2, 9,  PillColor.Red);
            //SpawnVirus(3, 9,  PillColor.Red);

            //SpawnVirus(4, 11, PillColor.Red);
            //SpawnVirus(4, 12, PillColor.Red);
            //SpawnVirus(4, 13, PillColor.Red);

            //SpawnVirus(5, 10, PillColor.Red);
            //SpawnPill( 5, 9,  PillColor.Red);
            //SpawnPill( 5, 8,  PillColor.Red);


            ////vertical pill test - 5 in a row test
            //SpawnPill(6, 10, PillColor.Green);
            //SpawnVirus(6, 11, PillColor.Green);
            //SpawnPill(6, 12, PillColor.Green);
            //SpawnVirus(6, 13, PillColor.Green);
            //SpawnVirus(6, 14, PillColor.Green);


            //wrap right to left test
            SpawnPill(0, 14, PillColor.Blue);
            SpawnPill(1, 14, PillColor.Blue);

            SpawnPill( 5, 13, PillColor.Green);
            SpawnVirus(5, 14, PillColor.Blue);
            SpawnVirus(6, 13, PillColor.Green);
            SpawnPill( 6, 14, PillColor.Blue);
            SpawnPill( 7, 13, PillColor.Green);
            SpawnVirus(7, 14, PillColor.Blue);

            RemainingViruses = 3;
#else
            RemainingViruses = 0;
            FixedSizeArray<Point> virusLocs = GenerateSpawnPoints(virusCount, topSpace);

            foreach (Point pt in virusLocs)
            {
                SpawnVirus(pt.X, pt.Y);
                RemainingViruses++;
            }
#endif
            //TODO - check for kills - 4 viruses in a row - replace 2 with 2 different colors
        }

        public FixedSizeArray<Point> GenerateSpawnPoints(int slotsCount, int topSpace)
        {
            int spawnSpotCount = GameWidthInSlots * (GameHeightInSlots - topSpace);
            FixedSizeArray<Point> result = new FixedSizeArray<Point>(spawnSpotCount);

            for (int xx = 0; xx < spawnSpotCount; xx++)
            {
                int xPos = xx % GameWidthInSlots;
                int yPos = xx / GameWidthInSlots;
                Point pt = new Point(xPos, yPos + topSpace);
                result.Add(pt);
            }

            while (result.Count > slotsCount)
            {
                result.Remove(random.Next(result.Count));
            }

            return result;
        }

        public void SpawnVirus(int x, int y)
        {
            int rndColorCode = random.Next(ColorCodeCount);
            SpawnVirus(x, y, (PillColor)rndColorCode);
        }

        public void SpawnVirus(int x, int y, PillColor color)
        {
            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            DrMarioGameObjectFactory factory = (DrMarioGameObjectFactory)sSystemRegistry.GameObjectFactory;

            DrMarioGameGameObjectTypes virusType;
            switch (color)
            {
                case PillColor.Red: virusType = DrMarioGameGameObjectTypes.Red_Virus; break;
                case PillColor.Green: virusType = DrMarioGameGameObjectTypes.Green_Virus; break;
                case PillColor.Blue: virusType = DrMarioGameGameObjectTypes.Blue_Virus; break;
                default: throw new Exception("currently only supports " + ColorCodeCount + " color viruses");
            }

            Vector2 screenPos = GetSlotLocation(x, y);
            GameObject virusGameObject = factory.SpawnVirus(screenPos.X, screenPos.Y, virusType);

            slots[GetPillSlotIndex(x, y)].Setup(PillEnd.None, SlotType.Virus, color, virusGameObject);

            manager.Add(virusGameObject);
        }

        public void GeneratePillInSlot(int slotX, int slotY)
        {
            int rndColorCode = random.Next(ColorCodeCount);
            GeneratePillInSlot(slotX, slotY, (PillColor)rndColorCode, PillEnd.None);
        }

        public void GeneratePillInSlot(int slotX, int slotY, PillColor color, PillEnd pillEnd)
        {
            Vector2 screenPos = GetSlotLocation(slotX, slotY);

            workspacePill1 = slots[GetPillSlotIndex(slotX, slotY)];
            GeneratePillInSlot(screenPos.X, screenPos.Y, color, pillEnd, ref workspacePill1);
        }

        private void GeneratePillInSlot(float screenX, float screenY, PillColor color, PillEnd pillEnd, ref PillSlot slot)
        {

            GameObjectManager manager = sSystemRegistry.GameObjectManager;
            DrMarioGameObjectFactory factory = (DrMarioGameObjectFactory)sSystemRegistry.GameObjectFactory;

            DrMarioGameGameObjectTypes pillObjectType = GetPillTypeFromColorCode(color);
            GameObject pillGameObject = factory.SpawnPill(screenX, screenY, pillObjectType);
            manager.Add(pillGameObject);

            slot.Setup(pillEnd, SlotType.Pill, color, pillGameObject);
        }

        private void ProcessInput(float gameTime)
        {
            PlayerController input = DrMarioGame.PlayerController;
            if (input.LeftPressed && !lastInput.LeftPressed)
            {
                //Move Left Pressed
                MoveActivePillSet(-1, 0, playerPill1, playerPill2);
            }
            else if (input.RightPressed && !lastInput.RightPressed)
            {
                //Move Right Pressed
                MoveActivePillSet(1, 0, playerPill1, playerPill2);
            }
            else if (input.UpPressed && !lastInput.UpPressed)
            {
                //Move Up Pressed
                if (moveAgainstGravity)
                    MoveActivePillSet(0, -1, playerPill1, playerPill2);
                else
                    RotateActivePillSet(true);
            }
            else if (input.DownPressed && (!lastInput.DownPressed || gameTime - lastDownPressedTime > timePerDownPress))
            {
                //Move Down Pressed
                lastDownPressedTime = gameTime;
                MoveActivePillSet(0, 1, playerPill1, playerPill2);
            }
            else if (input.RotLeftPressed && !lastInput.RotLeftPressed)
            {
                //LockInActivePillSet();
                //DropNewPillSet();

                //Rotate Left Pressed
                RotateActivePillSet(false);
            }
            else if (input.RotRightPressed && !lastInput.RotRightPressed)
            {
                //Rotate Right Pressed
                RotateActivePillSet(true);
            }

            lastInput = DrMarioGame.PlayerController.Snapshot();
        }

        public void ProcessActivePills(Point pt)
        {
            //process core game logic like pill breaking and dropping and such
        }

        private void LockInActivePillSet()
        {
            sSystemRegistry.SoundSystem.PlaySoundEffect(lockInSound, false);

            //1st pill
            bool killPill1 = false;
            workspacePill1 = GetPillSlot(playerPill1.Position);
            if (workspacePill1 != null)
            {
                //this is a valid pill slot
                workspacePill1.CloneFrom(playerPill1);
                playerPill1.EmptySlot();
            }
            else
            {
                killPill1 = true;//this has to stay alive for test on partner
                playerPill2.SetPillEnd(PillEnd.None);
            }
            //2nd pill
            workspacePill2 = GetPillSlot(playerPill2.Position);
            if (workspacePill2 != null)
            {
                //this is a valid pill slot
                workspacePill2.CloneFrom(playerPill2);
                playerPill2.EmptySlot();
            }
            else
            {
                playerPill2.KillImedietly();
                workspacePill1.SetPillEnd(PillEnd.None);
            }

            if (killPill1)
                playerPill1.KillImedietly();
        }

        private bool RotateActivePillSet(bool clockwise)
        {
            //TODO this would peobabyl need to be adjusted if the gravity is not normal
            /*
             * clockwise - notice - rotating up - not down into pills
             *                    - must checl let and right for empties
             * 
             * 1)   AB
             *      
             *      A
             * 2)   B
             *      
             * 3)   BA
             *      
             *      B
             * 4)   A
             * 
            */

            bool validMove = true;

            if (playerPill1.X == playerPill2.X)
            {
                //vertical - 
                //set workspacePil1 as  the top one
                if (playerPill1.Y < playerPill2.Y)
                {
                    workspacePill1 = playerPill1;
                    workspacePill2 = playerPill2;
                }
                else
                {
                    workspacePill2 = playerPill1;
                    workspacePill1 = playerPill2;
                }
                //workspacePil1 is now  the top one

                workspacePill3 = GetPillSlot(workspacePill2.X + 1, workspacePill2.Y);
                if (workspacePill3 == null || (workspacePill3 != null && !workspacePill3.IsEmpty))
                {
                    workspacePill3 = GetPillSlot(workspacePill2.X - 1, workspacePill2.Y);
                    if (workspacePill3 == null || (workspacePill3 != null && !workspacePill3.IsEmpty))
                    {
                        //no horizontal space
                        validMove = false;
                    }
                    else
                    {
                        //shift Left
                        workspacePill1.Move(-1, 0);
                        workspacePill2.Move(-1, 0);
                    }
                }

                if (validMove)
                {
                    if (clockwise)
                    {
                        workspacePill1.Move(1, 1);
                        workspacePill1.SetPillEnd(PillEnd.Right);

                        workspacePill2.SetPillEnd(PillEnd.Left);

                    }
                    else
                    {
                        workspacePill1.Move(0, 1);
                        workspacePill1.SetPillEnd(PillEnd.Left);

                        workspacePill2.Move(1, 0);
                        workspacePill2.SetPillEnd(PillEnd.Right);
                    }
                }
            }
            else if (playerPill1.Y == playerPill2.Y)
            {

                //horizontal
                //set workspacePil1 as  the left one
                if (playerPill1.X < playerPill2.X)
                {
                    workspacePill1 = playerPill1;
                    workspacePill2 = playerPill2;
                }
                else
                {
                    workspacePill2 = playerPill1;
                    workspacePill1 = playerPill2;
                }
                //workspacePil1 is now  the left one

                if (workspacePill1.Y - 1 != -1)
                {
                    //not on top row
                    workspacePill3 = GetPillSlot(workspacePill1.X, workspacePill1.Y - 1);
                    if (!workspacePill3.IsEmpty)
                    {
                        workspacePill3 = GetPillSlot(workspacePill2.X, workspacePill2.Y - 1);
                        if (!workspacePill3.IsEmpty)
                        {
                            //no vertical space
                            validMove = false;
                        }
                        else
                        {
                            //shift Right
                            workspacePill1.Move(1, 0);
                            workspacePill2.Move(1, 0);
                        }
                    }
                }

                if (validMove)
                {
                    if (clockwise)
                    {
                        workspacePill1.Move(0, -1);
                        workspacePill1.SetPillEnd(PillEnd.Top);

                        workspacePill2.Move(-1, 0);
                        workspacePill2.SetPillEnd(PillEnd.Bottom);
                    }
                    else
                    {
                        workspacePill1.SetPillEnd(PillEnd.Bottom);

                        workspacePill2.Move(-1, -1);
                        workspacePill2.SetPillEnd(PillEnd.Top);
                    }
                }
            }
            else
            {
                throw new Exception("Active Pill set is not touching. This should not be possible.");
            }
            return validMove;
        }

        private bool IsCloseSlotEmpty(int xDif, int yDif, PillSlot pill)
        {
            Point newPillPos = new Point(pill.X + xDif, pill.Y + yDif);
            if (newPillPos.X >= 0 && newPillPos.X <= GameWidthInSlots - 1)
            {
                if (newPillPos.Y >= 0 && newPillPos.Y <= GameHeightInSlots - 1)
                {
                    if (GetPillSlot(newPillPos.X, newPillPos.Y).IsEmpty)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool MoveActivePillSet(int xDif, int yDif, PillSlot pill1, PillSlot pill2)
        {
            if (IsCloseSlotEmpty(xDif, yDif, pill1))
            {
                if (IsCloseSlotEmpty(xDif, yDif, pill2))
                {
                    pill1.Move(xDif, yDif);
                    pill2.Move(xDif, yDif);
                    return true;
                }
            }
            return false;
        }

        public DrMarioGameGameObjectTypes GetPillTypeFromColorCode(PillColor colorCode)
        {
            switch (colorCode)
            {
                case PillColor.Red: return DrMarioGameGameObjectTypes.Red_Pill;
                case PillColor.Green: return DrMarioGameGameObjectTypes.Green_Pill;
                case PillColor.Blue: return DrMarioGameGameObjectTypes.Blue_Pill;
                default: throw new Exception("currently only supports " + ColorCodeCount + " color Pills");
            }
        }

        private bool SwapInFuturePillSet()
        {
            bool validSwapIn = true;
            //TODO, this should stretch to center if not an 8 with level - not 3,0, and 4,0 - if gravity is not normal

            playerPill1.TransferPillFrom(futurePill1);
            playerPill1.SetPosition(playPillDropLocation);
            playerPill2.TransferPillFrom(futurePill2);
            playerPill2.SetPosition(playPillDropLocation.X + 1, playPillDropLocation.Y);

            if (!GetPillSlot(playerPill1.X, playerPill1.Y).IsEmpty || !GetPillSlot(playerPill2.X, playerPill2.Y).IsEmpty)
            {
                validSwapIn = false;
            }

            //generate new future pills
            GeneratePillSet(futurePillSetLocation.X, futurePillSetLocation.Y, ref futurePill1, ref futurePill2);

            return validSwapIn;
        }

        private void GeneratePillSet(float screenX, float screenY, ref PillSlot leftPill, ref PillSlot rightPill)
        {
            PillColor color1 = (PillColor)random.Next(ColorCodeCount);
            PillColor color2 = (PillColor)random.Next(ColorCodeCount);

            GeneratePillInSlot(screenX, screenY, color1, PillEnd.Left, ref leftPill);
            GeneratePillInSlot(screenX + SlotSize, screenY, color2, PillEnd.Right, ref rightPill);
        }

        public override void Update(float timeDelta, BaseObject parent)
        {
            if (gameState != GameState.Loading)
            {
                float gameTime = sSystemRegistry.TimeSystem.getGameTime();

                float timePerTick;
                if (gameState == GameState.PlayerPillActive)
                {
                    //processInput must be outsude the tick so it can be called faster for faster controls and quick dropping
                    ProcessInput(gameTime);
                    timePerTick = playerPillDropTickDelay;
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
            //lock in
            //while check for kills
            //  if check for floaters
            //      act gravity
            //  else
            //      break for new drop

            TickStart: 

            switch (gameState)
            {
                case GameState.Loading:
                    //do nothing
                    break;
                case GameState.PlayerPillDrop:
                    virusKillsThisPill = 0;
                    bool validSwapIn = SwapInFuturePillSet();
                    if (validSwapIn)
                    {
                        gameState = GameState.PlayerPillActive;
                    }
                    else
                    {
                        //game over
                        LevelOver(true);
                    }
                    break;
                case GameState.PlayerPillActive:
                    if (!MoveActivePillSet(gravityVector.X, gravityVector.Y, playerPill1, playerPill2))
                    {
                        //Pill didn't move down - hit something - lock in
                        LockInActivePillSet();
                        gameState = GameState.CheckKills;
                        goto TickStart;
                    }
                    break;
                case GameState.CheckKills:
                    //Pill didn't move down - hit something - lock in
                    if (CheckKills())
                    {
                        //killed
                        int kills = ProcessKills();

                        UpdateScore();

                        if (RemainingViruses == 0)
                        {
                            //-1 kills triggers the final kill sound
                            kills = -1;
                            LevelOver(false);
                        }
                        else
                        {
                            gameState = GameState.GravityStep;
                        }

                        PlayKillSound(kills);
                        sSystemRegistry.VibrationSystem.Vibrate(1, killVibration);

                        //goto TickStart;
                    }
                    else
                    {
                        gameState = GameState.PlayerPillDrop;
                        goto TickStart;
                    }
                    break;
                case GameState.GravityStep:
                    if (StepGravity())
                    {
                        //moved something - so step this again
                    }
                    else
                    {
                        gameState = GameState.CheckKills;
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

        private bool StepGravity()
        {
            bool movedOne = false;
            //Step through each slot from the bottom up
            //TODO could optomize by not checking very bottom (based on gravity) - would be GameHeightInSlots -2
            for (int yy = GameHeightInSlots - 1; yy >= 0; yy--)
            {
                for (int xx = GameWidthInSlots - 1; xx >= 0; xx--)
                {
                    PillSlot slot = GetPillSlot(xx, yy);
                    if (IsPillAndFloating(slot, out workspacePill1))
                    {
                        MovePillSlot(gravityVector.X, gravityVector.Y, slot);
                        movedOne = true;

                        if (!workspacePill1.IsEmpty)
                        {
                            MovePillSlot(gravityVector.X, gravityVector.Y, workspacePill1);
                        }
                    }
                }
            }
            return movedOne;
        }

        public bool IsPillAndFloating(PillSlot pill, out PillSlot partner)
        {
            bool floating = false;
            partner = PillSlot.Blank;
            if (pill.Type == SlotType.Pill)
            {
                if (IsCloseSlotEmpty(gravityVector.X, gravityVector.Y, pill))
                {
                    if (pill.PillEnd == PillEnd.None)
                    {
                        //solitary
                        floating = true;
                    }
                    else
                    {
                        bool pairIsAbove = false;
                        //part of set
                        Point pairOffset = new Point(0, 0);
                        switch (pill.PillEnd)
                        {
                            default: throw new Exception("Pill marked as part of pair cant find partner");
                            case PillEnd.Left: pairOffset = new Point(1, 0); break;
                            case PillEnd.Right: pairOffset = new Point(-1, 0); break;
                            case PillEnd.Top: pairOffset = new Point(0, 1); break;
                            case PillEnd.Bottom: pairOffset = new Point(0, -1); pairIsAbove = true; break;
                        }
                        partner = GetPillSlot(pill.X + pairOffset.X, pill.Y + pairOffset.Y);

                        if (pairIsAbove || IsCloseSlotEmpty(gravityVector.X, gravityVector.Y, partner))
                        {
                            floating = true;
                        }
                    }
                }
            }
            return floating;
        }

        public void MovePillSlot(int xDif, int yDif, PillSlot pill)
        {
            PillSlot newSlot = GetPillSlot(pill.X + xDif, pill.Y + yDif);
            newSlot.TransferPillFrom(pill);
        }

        private void CalcScore()
        {
            //baced on this http://faqs.ign.com/articles/382/382495p1.html

            Math.Pow(2, virusKillsThisPill - 1);
            Score += (100 * (int)Math.Pow(2, virusKillsThisPill - 1)) * (Speed+1);

            if (Score > HighScore)
                HighScore = Score;
        }

        private bool CheckKills()
        {
            killWorkspace.Clear();
            bool goodToKill = false;
            bool sequenceBroke = false;
            PillColor lastColor;
            //check vertical cols
            for (int xx = 0; xx < GameWidthInSlots; xx++)
            {
                lastColor = PillColor.None;
                killScanWorkspace.Clear();
                for (int yy = 0; yy < GameHeightInSlots; yy++)
                {
                    //if (xx == 6 && yy == 8)
                    //{
                    //    int y = 5;
                    //}

                    bool addedThis = false;
                    PillSlot slot = GetPillSlot(xx, yy);
                    if (!slot.IsEmpty)
                    {
                        if (lastColor == PillColor.None)
                            lastColor = slot.Color;//first one
                        if (slot.Color == lastColor)
                        {
                            addedThis = true;
                            killScanWorkspace.Add(slot);
                            if (killScanWorkspace.Count >= MatchesNeededToKill)
                            {
                                //dont kill here... there could be more than 4
                                goodToKill = true;
                            }
                        }
                    }
                    lastColor = slot.Color;

                    if (!addedThis)
                        sequenceBroke = true;
                    if (sequenceBroke)
                    {
                        if (goodToKill)
                        {
                            killWorkspace.AddDistinctArray(killScanWorkspace, true);
                            goodToKill = false;
                        }
                        killScanWorkspace.Clear();
                        if (!slot.IsEmpty)
                            killScanWorkspace.Add(slot);
                        sequenceBroke = false;
                    }
                }
                if (killScanWorkspace.Count >= MatchesNeededToKill)
                {
                    killWorkspace.AddDistinctArray(killScanWorkspace, true);
                    goodToKill = false;
                }
            }
            //check horizontal rows
            for (int yy = 0; yy < GameHeightInSlots; yy++)
            {
                lastColor = PillColor.None;
                killScanWorkspace.Clear();
                for (int xx = 0; xx < GameWidthInSlots; xx++)
                {
                    bool addedThis = false;
                    PillSlot slot = GetPillSlot(xx, yy);
                    if (!slot.IsEmpty)
                    {
                        if (lastColor == PillColor.None)
                            lastColor = slot.Color;//first one
                        if (slot.Color == lastColor)
                        {
                            addedThis = true;
                            killScanWorkspace.Add(slot);
                            if (killScanWorkspace.Count >= MatchesNeededToKill)
                            {
                                //dont kill here... there could be more than 4
                                goodToKill = true;
                            }
                        }
                    }
                    lastColor = slot.Color;

                    if (!addedThis)
                        sequenceBroke = true;
                    if (sequenceBroke)
                    {
                        if (goodToKill)
                        {
                            killWorkspace.AddDistinctArray(killScanWorkspace, true);
                            goodToKill = false;
                        }
                        killScanWorkspace.Clear();
                        if (!slot.IsEmpty)
                            killScanWorkspace.Add(slot);
                        sequenceBroke = false;
                    }
                }
                if (killScanWorkspace.Count >= MatchesNeededToKill)
                {
                    killWorkspace.AddDistinctArray(killScanWorkspace, true);
                    goodToKill = false;
                }
            }
            return killWorkspace.Count > 0;
        }

        private int ProcessKills()
        {
            int virusKills = 0;
            foreach (PillSlot slot in killWorkspace)
            {
                if (slot.Type == SlotType.Virus)
                {
                    virusKillsThisPill++;
                    RemainingViruses--;
                    virusKills++;
                }

                if (slot.PillEnd != PillEnd.None)
                {
                    //this was part of a Pill set adjust paired slot

                    switch (slot.PillEnd)
                    {
                        case PillEnd.Top: workspacePill1 = GetPillSlot(slot.X, slot.Y + 1); break;
                        case PillEnd.Bottom: workspacePill1 = GetPillSlot(slot.X, slot.Y - 1); break;
                        case PillEnd.Right: workspacePill1 = GetPillSlot(slot.X - 1, slot.Y); break;
                        case PillEnd.Left: workspacePill1 = GetPillSlot(slot.X + 1, slot.Y); break;
                    }
                    workspacePill1.SetPillEnd(PillEnd.None);
                }

                slot.Kill();
                slot.EmptySlot();
            }
            return virusKills;
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
            foreach (PillSlot slot in slots)
            {
                if (slot.Pill != null)
                {
                    slot.KillImedietly();
                }
                slot.EmptySlot();
            }

            playerPill1.KillImedietly();
            playerPill2.KillImedietly();
            futurePill1.KillImedietly();
            futurePill2.KillImedietly();

            workspacePill1.KillImedietly();
            workspacePill2.KillImedietly();
            workspacePill3.KillImedietly();
        }

        public void StartGame(int level, int speed)
        {
            Score = 0;
            LevelNo = level;
            Speed = speed;


            switch (Speed)
            {
                default:
                case 0: playerPillDropTickDelay = playerPillDropTickDelay_Low; break;
                case 1: playerPillDropTickDelay = playerPillDropTickDelay_Med; break;
                case 2: playerPillDropTickDelay = playerPillDropTickDelay_High; break;
            }
            //totalPlayTime = new TimeSpan();

            lastInput = new PlayerController();

            StartLevel();
        }

        public void StartLevel()
        {
            ClearSlots();
            FillNewLevel(LevelNo);

            //generate future pills
            GeneratePillSet(futurePillSetLocation.X, futurePillSetLocation.Y, ref futurePill1, ref futurePill2);

            gameState = GameState.PlayerPillDrop;
        }

        public static string GetSpeedName(int speed)
        {
            switch (speed)
            {
                default:
                case 0: return "Low";
                case 1: return "Med";
                case 2: return "Hi";
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

        private PillSlot GetPillSlot(int x, int y)
        {
            if (y < 0 || x < 0 || y >= GameHeightInSlots || x >= GameWidthInSlots)//off game grid
                return null;
            return slots[GetPillSlotIndex(x, y)];
        }

        private PillSlot GetPillSlot(Point pt)
        {
            return GetPillSlot(pt.X, pt.Y);
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

    enum PillEnd
    {
        Dead,
        None,
        Top,
        Bottom,
        Left,
        Right
    }

    enum PillColor
    {
        None = -1,
        Red = 0,
        Green = 1,
        Blue = 2,
    }

    enum SlotType
    {
        Empty,
        Pill,
        Virus
    }

    class PillSlot
    {
        public static PillSlot Blank = new PillSlot(0, 0);

        public GameObject Pill;
        private PillEnd pillEnd = PillEnd.None;
        public SlotType Type = SlotType.Empty;
        public PillColor Color = PillColor.None;
        private Point position = Point.Zero;

        public bool IsEmpty { get { return Type == SlotType.Empty; } }

        public int X { get { return position.X; } }
        public int Y { get { return position.Y; } }
        public Point Position { get { return position; } }
        public PillEnd PillEnd { get { return pillEnd; } }


        public PillSlot(int x, int y)
        {
            position.X = x;
            position.Y = y;
        }

        public void SetGameObject(GameObject o)
        {
            Pill = o;
        }

        public void EmptySlot()
        {
            Pill = null;
            pillEnd = PillEnd.None;
            Color = PillColor.None;
            Type = SlotType.Empty;
        }

        public void Setup(PillEnd pillEnd, SlotType type, PillColor ColorCode, GameObject o)
        {
            Pill = o;
            this.Color = ColorCode;
            this.Type = type;
            SetPillEnd(pillEnd);
        }

        public void Move(int xDif, int yDif)
        {
            position.X += xDif;
            position.Y += yDif;
            if (Pill != null)
            {
                Pill.SetPosition(DrMarioGameManager.GetSlotLocation(position));
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

            if (Pill != null)
            {
                Pill.SetPosition(DrMarioGameManager.GetSlotLocation(position));
            }
        }

        public void Kill()
        {
            SetPillEnd(PillEnd.Dead);
            Pill.FindByType<LifetimeComponent>().SetTimeUntilDeath(DrMarioGameObjectFactory.DeathAnimationDuration);
        }

        public void KillImedietly()
        {
            //SetPillEnd(PillEnd.Dead);
            if(Pill!=null)
                Pill.life = 0;
            //Pill.FindByType<LifetimeComponent>().SetTimeUntilDeath(0);
        }

        public void SetPillEnd(PillEnd pillEnd)
        {
            this.pillEnd = pillEnd;
            //if(Type == SlotType.Pill)
            if (Pill != null)
            {
                switch (pillEnd)
                {
                    case PillEnd.Bottom: Pill.FindByType<SpriteComponent>().PlayAnimation((int)Animations.Big_Bottom); break;
                    case PillEnd.Top: Pill.FindByType<SpriteComponent>().PlayAnimation((int)Animations.Big_Top); break;
                    case PillEnd.Right: Pill.FindByType<SpriteComponent>().PlayAnimation((int)Animations.Big_Right); break;
                    case PillEnd.Left: Pill.FindByType<SpriteComponent>().PlayAnimation((int)Animations.Big_Left); break;
                    case PillEnd.None: Pill.FindByType<SpriteComponent>().PlayAnimation((int)Animations.Idle); break;
                    case PillEnd.Dead: Pill.FindByType<SpriteComponent>().PlayAnimation((int)Animations.Death); break;
                }
                
            }
        }

        public void CloneFrom(PillSlot src)
        {
            position.X = src.position.X;
            position.Y = src.position.Y;
            Pill = src.Pill;
            pillEnd = src.pillEnd;
            Color = src.Color;
            Type = src.Type;
        }

        public PillSlot Clone()
        {
            PillSlot result = new PillSlot(position.X, position.Y);
            result.Pill = Pill;
            result.pillEnd = pillEnd;
            result.Color = Color;
            result.Type = Type;
            return result;
        }

        public void TransferPillFrom(PillSlot src)
        {
            Pill = src.Pill;
            pillEnd = src.pillEnd;
            Color = src.Color;
            Type = src.Type;
            src.EmptySlot();

            Pill.SetPosition(DrMarioGameManager.GetSlotLocation(X, Y));
        }

        public override string ToString()
        {
            return "PillSlot(" + X + "," + Y + "," + Type + "," + Color + "," + PillEnd + ")";
        }
    }
}
