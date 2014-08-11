//#define DeveleperModeEnabled

using Archives;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;
using System.Reflection;

namespace Snake
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class DrMarioGame : Microsoft.Xna.Framework.Game, IFreshGame
    {
        private const string settingsFilePath = @"Content\scripts\default.conf";
        private VariablesFile settingsFile;

        enum FPSMode
        {
            Off,
            On,
            Verbose
        }

        //debug vars
        private bool debugMenuEnabled = true;
        private bool debugShowVersion = true;
        private FPSMode debugFPSMode = FPSMode.Verbose;
        private bool debugShowScoreAndTime = true;
        private bool debugShowObjectManagerInfo = true;
        private bool debugShowInputInfo = false;
        private bool debugShowVolumeInfo = false;
        private bool debugShowMemoryInfo = true;
        private bool debugShowCameraFollowDistance = false; // not in debug menu
        private SpriteFont debugFont;

        //cheat vars
        private bool cheatsEnabled = true;
        private bool cheatGodMode = false;
        private bool cheatNoClip = false;
        private CheatCodeSystem cheatCodeSystem;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private bool fullScreenMode = false;

        //FPS profile info
        private const int FPS_PROFILE_REPORT_DELAY = 1000;
        private DateTime lastFrameTime = DateTime.Now;
        private int frameNo = 0;
        private double profileFrameTime = 0;
        private int profileFrames = 0;
        private double profileLastFPS = -1;
        private double profileMinFrameTime = double.MaxValue;
        private double profileMaxFrameTime = double.MinValue;
        private double lastProfileMinFrameTime = -1;
        private double lastProfileMaxFrameTime = -1;
        private double lastProfileTotalTime = -1;
        private double lastProfileTotalFrames = -1;
        private bool fixedRenderSpeed = true;

        // state info
        public GameStates previousGameState;
        public GameStates gameState;
        private int lastGameEvent = -1;
        private float timeTillGameEvent = 0;

        private TimeSpan totalPlayTime;
        private double lastUpdateTime = 0;
        private double lastDrawTime = 0;

        //input Vars
        private bool controllerInputEnabled = true;
        private KeyboardState oldKeyState;
        private GamePadState oldGamePadState;
        public static PlayerController PlayerController;

        private Texture2D backgroundPic;

        //Sounds
        private bool pauseMusicOnPause = true;
        private SoundEffect menuSelectionSound;
        private SoundEffect shotgunCockSound;
        private SoundEffect ringSound;
        private SoundEffect gameOverSound;
        private SoundEffect droppedRingsSound;
        private SoundEffect saveMarkSound;
        private SoundEffect vanishSound;
        private SoundEffect bossSound;
        private SoundEffect drowningSound;
        private SoundEffect continueSound;
        private SoundEffect themeSongSound;
        private Song backgroundMenuMusic;
        private Song backgroundGameMusic;

        //Menu Vars
        private SpriteFont menuFont;
        private MenuTree mainMenuTree;
        private Menu mainMenu;
        private SoundEffectInstance menuSoundEffectVollumeTest;
        private Texture2D menuSelectorPic;
        private int menuSoundEffectVollumeTestID = -1;
        //private SoundEffectInstance menuMusicVollumeTest;
        //private int menuMusicVollumeTestID = -1;
        private bool menuWrapSelection = false;
        private string menuTitle = "Dr Fresh Mario";
        private int menuTitleHeight = 150;
        private float menuTransitionTime = 1;
        private Menu pauseMenu;

        //System Variables
        private SoundSystem soundSystem;
        private ObjectManager gameRoot;
        private RenderSystem renderSystem;
        private GameObjectManager gameManager;
        private DrMarioGameManager drMarioManager;
        private LevelSystem levelSystem;
        private GameObjectFactory objectFactory;

        //game vaiables
        private int newGameLevelNo = 0;
        private int newGameSpeedValue = 1;

        public DrMarioGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            ContextParameters paramaters = new ContextParameters();
            BaseObject.sSystemRegistry.Game = this;
            BaseObject.sSystemRegistry.ContextParameters = paramaters;
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            //TestCode();

            //load script variables - Before everything else
            LoadScriptVariablesPart1();

            //safty stop all vibration
            for (int xx = 0; xx < 4; xx++)
                GamePad.SetVibration((PlayerIndex)xx, 0, 0);
            PlayerController = new PlayerController();
            PlayerController.Reset();

            base.Initialize();
            IsMouseVisible = true;
            IsFixedTimeStep = fixedRenderSpeed;
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.ApplyChanges();


            spriteBatch = new SpriteBatch(GraphicsDevice);
            BaseObject.sSystemRegistry.SpriteBatch = spriteBatch;

            BaseObject.sSystemRegistry.MainGame = this;

            //initialize Context paramaters
            ContextParameters paramaters = BaseObject.sSystemRegistry.ContextParameters;
            SetGameSize(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);

            const int MaxObjects = 10001;

            //InputSystem input = new InputSystem();
            //BaseObject.sSystemRegistry.inputSystem = input;
            //BaseObject.sSystemRegistry.registerForReset(input);

            //InputGameInterface inputInterface = new InputGameInterface();
            //gameRoot.add(inputInterface);
            //BaseObject.sSystemRegistry.inputGameInterface = inputInterface;

            levelSystem = new LevelSystem();
            levelSystem.LoadOneLevel(new DrMarioLevel());
            BaseObject.sSystemRegistry.LevelSystem = levelSystem;

            CollisionSystem collision = new CollisionSystem();
            BaseObject.sSystemRegistry.CollisionSystem = collision;
            collision.LoadCollisionTiles(TiledLevel.DefaultTileSize);
            BaseObject.sSystemRegistry.HitPointPool = new HitPointPool();

            //GameManager goes here
            gameManager = new GameObjectManager(paramaters.GameWidth * 1.3f, MaxObjects);//TODO put right number here (activation radius, object max count)
            BaseObject.sSystemRegistry.GameObjectManager = gameManager;

            drMarioManager = new DrMarioGameManager();

            //factory was removed from the registry for generalazation reasons
            objectFactory = new DrMarioGameObjectFactory();
            BaseObject.sSystemRegistry.GameObjectFactory = objectFactory;
            objectFactory.PreloadEffects();

            //BaseObject.sSystemRegistry.hotSpotSystem = new HotSpotSystem();


            // Camera must come after the game manager so that the camera target moves before the camera centers.
            CameraSystem camera = new CameraSystem();
            BaseObject.sSystemRegistry.CameraSystem = camera;
            BaseObject.sSystemRegistry.registerForReset(camera);

            GameObjectCollisionSystem dynamicCollision = new GameObjectCollisionSystem();
            BaseObject.sSystemRegistry.GameObjectCollisionSystem = dynamicCollision;
            dynamicCollision.SetDebugPrefs(false, false, false);

            //Sound System
            soundSystem = new SoundSystem(400, 4);
            BaseObject.sSystemRegistry.SoundSystem = soundSystem;
            soundSystem.VolumeAdjustIncrement = 0.05f;

            //Vibration System
            VibrationSystem vibeSys = new VibrationSystem(1);
            BaseObject.sSystemRegistry.VibrationSystem = vibeSys;

            renderSystem = new RenderSystem(MaxObjects, 2);
            BaseObject.sSystemRegistry.RenderSystem = renderSystem;

            BaseObject.sSystemRegistry.VectorPool = new VectorPool();
            BaseObject.sSystemRegistry.DrawableFactory = new DrawableFactory(MaxObjects);
            BaseObject.sSystemRegistry.DebugSystem = new DebugSystem(Content);

            DrMarioHudSystem hud = new DrMarioHudSystem();
            hud.Setup();
            BaseObject.sSystemRegistry.HudSystem = hud;


            //Cheat Code System
            cheatCodeSystem = new CheatCodeSystem();
            if (cheatsEnabled)
            {
                //Enum[] code_contra = { Buttons.DPadUp, Buttons.DPadUp, Buttons.DPadDown, Buttons.DPadDown, Buttons.DPadLeft, Buttons.DPadRight, Buttons.DPadLeft, Buttons.DPadRight, Buttons.B, Buttons.A, Buttons.Start };
                //cheatCodeSystem.AddCode(new CheatCode(code_contra, HurtPlayer));

                Enum[] code_godMode = { Keys.I, Keys.D, Keys.D, Keys.Q, Keys.D };
                cheatCodeSystem.AddCode(new CheatCode(code_godMode, ToggleGodMode));

                Enum[] code_godMode2 = { Buttons.DPadUp, Buttons.DPadDown, Buttons.DPadLeft, Buttons.DPadRight, Buttons.A, Buttons.B, Buttons.A, Buttons.B };
                cheatCodeSystem.AddCode(new CheatCode(code_godMode2, ToggleGodMode));

                Enum[] code_noClipMode = { Keys.I, Keys.D, Keys.N, Keys.O, Keys.C, Keys.L, Keys.I, Keys.P };
                cheatCodeSystem.AddCode(new CheatCode(code_noClipMode, ToggleNoClip));

                Enum[] code_noClipMode2 = { Buttons.DPadUp, Buttons.DPadRight, Buttons.DPadDown, Buttons.DPadLeft, Buttons.A, Buttons.B, Buttons.A, Buttons.B };
                cheatCodeSystem.AddCode(new CheatCode(code_noClipMode2, ToggleNoClip));

                //Enum[] code_maxAmmo = { Keys.I, Keys.D, Keys.K, Keys.F, Keys.A };
                //cheatCodeSystem.AddCode(new CheatCode(code_maxAmmo, HurtPlayer));
            }

            gameRoot = new MainLoop();
            //gameRoot.add(inputInterface);
            gameRoot.Add(gameManager);
            gameRoot.Add(drMarioManager);
            gameRoot.Add(camera);
            gameRoot.Add(dynamicCollision);
            gameRoot.Add(hud);
            gameRoot.Add(collision);

            //load script variables - after systems so it can tweak them
            LoadScriptVariablesPart2();

            //initialize input states
            oldKeyState = Keyboard.GetState();

            //load menu - pause is used in level editor
            InitializeMenus();

            {
                GotoMainMenu();
            }
        }

        private void SetGameSize(int width, int height)
        {
            ContextParameters paramaters = BaseObject.sSystemRegistry.ContextParameters;

            paramaters.GameWidth = width;
            paramaters.GameHeight = height;
            paramaters.ViewWidth = paramaters.GameWidth;
            paramaters.ViewHeight = paramaters.GameHeight;
            paramaters.ViewScaleX = (float)paramaters.ViewWidth / paramaters.GameWidth;
            paramaters.ViewScaleY = (float)paramaters.ViewHeight / paramaters.GameHeight;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content. - Called before initialize
        /// </summary>
        protected override void LoadContent()
        {
            Profiler.Start();
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            debugFont = Content.Load<SpriteFont>("SpriteFont1");
            menuFont = Content.Load<SpriteFont>("MenuFont");

            //background pic
            //backgroundPic = Content.Load<Texture2D>(@"pics\misc\stars");
            backgroundPic = Content.Load<Texture2D>(@"pics\background");
            menuSelectorPic = Content.Load<Texture2D>(@"pics\misc\menuSelector");

            //load sounds
            shotgunCockSound = Content.Load<SoundEffect>(@"sounds\shotgun_cock_01");
            menuSelectionSound = Content.Load<SoundEffect>(@"sounds\button-3");
            gameOverSound = Content.Load<SoundEffect>(@"sounds\Sonic_End");
            ringSound = Content.Load<SoundEffect>(@"sounds\SonicRing");
            droppedRingsSound = Content.Load<SoundEffect>(@"sounds\Sonic_Rings_Out");
            saveMarkSound = Content.Load<SoundEffect>(@"sounds\Sonic_Save_Mark");
            vanishSound = Content.Load<SoundEffect>(@"sounds\Sonic_Vanish");
            //music
            bossSound = Content.Load<SoundEffect>(@"sounds\Sonic_2-boss");
            drowningSound = Content.Load<SoundEffect>(@"sounds\Sonic_Drowning");
            continueSound = Content.Load<SoundEffect>(@"sounds\Sonic_Continue");
            themeSongSound = Content.Load<SoundEffect>(@"sounds\Sonic_Theme");


            backgroundMenuMusic = Content.Load<Song>(@"songs\Sonic_the_Hedgehog_Subsonic_Sparkle_OC_ReMix");
            backgroundGameMusic = Content.Load<Song>(@"songs\02 DotA (Radio Edit)");

            Profiler.Stop();
        }

        private void TestCode()
        {
            Profiler.Start();

            ////XmlSerializer serializer = Content.Load<XmlSerializer>("stats");

            //XmlSerializer missionSerializer = new XmlSerializer(typeof(Mission));
            //string fileName = "Content\\stats2.xml";
            //FileStream fs = new FileStream(fileName, FileMode.Open);
            //XmlReader reader = XmlReader.Create(fs);

            //Mission m;
            //m = (Mission)missionSerializer.Deserialize(reader);
            //fs.Close();



            //const string keywordEnd = "end";
            //const string keywordStart = "start";

            //string s = "end";
            //switch (s)
            //{
            //    case keywordEnd:

            //        break;
            //    case keywordStart:

            //        break;
            //    case "End":

            //        break;
            //}



            //string scriptFile = @"Content\testLevelScript.txt";
            //FreshScript testScript = new FreshScript(scriptFile, null);




            //FixedSizeArray<Point> line = FreshArchives.BresenhamLine(0, 0, 10, 5);

            //const int max = 30;
            //FixedSizeArray<Point> line2 = FreshArchives.BresenhamLine(FreshArchives.Random.Next(0, max), FreshArchives.Random.Next(0, max), FreshArchives.Random.Next(0, max), FreshArchives.Random.Next(0, max));

            Profiler.Stop();
        }

        private void GotoMainMenu()
        {
            MediaPlayer.Stop();
            MediaPlayer.Play(backgroundMenuMusic);
            MediaPlayer.IsRepeating = true;

            levelSystem.ClearObjects();

            gameState = GameStates.MainMenu;
            mainMenuTree.SetMenu(mainMenu);
        }

        private void InitializeMenus()
        {
            mainMenuTree = new MenuTree(menuFont);
            mainMenuTree.WrapSelection = menuWrapSelection;
            mainMenuTree.title = menuTitle;
            mainMenuTree.titleHeight = menuTitleHeight;
            mainMenuTree.transitionDuration = menuTransitionTime;
            mainMenuTree.SelectionChangeSound = menuSelectionSound;
            mainMenuTree.EnterActionSound = menuSelectionSound;
            mainMenuTree.BackActionSound = menuSelectionSound;
            mainMenuTree.SetSlector(menuSelectorPic);


#if DeveleperModeEnabled
            //Debug Menu
            MenuItem debugMenuItem = null;
            if (debugMenuEnabled)
            {
                MenuItem debugShowVersionItem = new MenuItem();
                debugShowVersionItem.ToggleCallbackAction = MenuItem_Debug_ShowVersion;
                debugShowVersionItem.SetName("CODE Version", "CODE", debugShowVersion ? "Hide" : "Show");
                MenuItem debugShowFpsItem = new MenuItem();
                debugShowFpsItem.ToggleCallbackAction = MenuItem_Debug_ShowFPS_ChangeUp;
                debugShowFpsItem.ChangeLeftCallbackAction = MenuItem_Debug_ShowFPS_ChangeDown;
                debugShowFpsItem.SetName("FPS Mode: CODE", "CODE", "" + debugFPSMode);
                MenuItem debugShowScoreItem = new MenuItem();
                debugShowScoreItem.ToggleCallbackAction = MenuItem_Debug_ShowScore;
                debugShowScoreItem.SetName("CODE Score", "CODE", debugShowScoreAndTime ? "Hide" : "Show");
                MenuItem debugShowVolumeItem = new MenuItem();
                debugShowVolumeItem.ToggleCallbackAction = MenuItem_Debug_ShowVolume;
                debugShowVolumeItem.SetName("CODE Volume", "CODE", debugShowVolumeInfo ? "Hide" : "Show");
                MenuItem debugShowInputItem = new MenuItem();
                debugShowInputItem.ToggleCallbackAction = MenuItem_Debug_ShowInput;
                debugShowInputItem.SetName("CODE Input", "CODE", debugShowInputInfo ? "Hide" : "Show");
                MenuItem debugShowObjectCountsItem = new MenuItem();
                debugShowObjectCountsItem.ToggleCallbackAction = MenuItem_Debug_ShowObjectMnagerInfo;
                debugShowObjectCountsItem.SetName("CODE Object Counts", "CODE", debugShowObjectManagerInfo ? "Hide" : "Show");
                MenuItem debugShowMemoryItem = new MenuItem();
                debugShowMemoryItem.ToggleCallbackAction = MenuItem_Debug_ShowMemory;
                debugShowMemoryItem.SetName("CODE Memory", "CODE", debugShowMemoryInfo ? "Hide" : "Show");
                Menu debugInfoMenu = mainMenuTree.CreateMenu(new MenuItem[] { debugShowFpsItem, debugShowVersionItem, debugShowScoreItem, debugShowVolumeItem, debugShowInputItem, debugShowObjectCountsItem, debugShowMemoryItem });

                MenuItem debugFixedStepItem = new MenuItem(null);
                debugFixedStepItem.ToggleCallbackAction = MenuItem_Debug_FixedStep;
                debugFixedStepItem.SetName("Fixed Render Speed: CODE", "CODE", fixedRenderSpeed ? "Enabled" : "Disabled");
                MenuItem debugCameraBoundItem = new MenuItem();
                debugCameraBoundItem.ToggleCallbackAction = MenuItem_Debug_CameraBound;
                debugCameraBoundItem.SetName("Camera Bound To Level: CODE", "CODE", BaseObject.sSystemRegistry.CameraSystem.BoundToLevel ? "Enabled" : "Disabled");
                MenuItem debugInfoItem = new MenuItem("Info", debugInfoMenu);
                Menu debugMenu = mainMenuTree.CreateMenu(new MenuItem[] { debugInfoItem, debugFixedStepItem, debugCameraBoundItem });

                //this is the item to add to other menus
                debugMenuItem = new MenuItem("Debug", debugMenu);
            }
#endif

            string soundLabel = "Sound";
            string musicLabel = "Music";
            string code = "CODE";
            string volumeLabel = " Volume: " + code + "%";
            SoundSystem soundSys = BaseObject.sSystemRegistry.SoundSystem;

            //Sound Menu - code set here - details will be set on the fly
            MenuItem soundEnabledItem = new MenuItem();
            soundEnabledItem.ToggleCallbackAction = MenuItem_SoundEffectToggleMute;
            soundEnabledItem.SetName(soundLabel + ": " + code, code, "Temp");
            MenuItem soundVolumeItem = new MenuItem("");
            soundVolumeItem.ChangeLeftCallbackAction = MenuItem_SoundEffectVolumeDown;
            soundVolumeItem.ChangeRightCallbackAction = MenuItem_SoundEffectVolumeUp;
            soundVolumeItem.HoverCallbackAction = MenuItem_SoundEffectsVolumeHover;
            soundVolumeItem.UnHoverCallbackAction = MenuItem_SoundEffectsVolumeUnHover;
            soundVolumeItem.SetName(soundLabel + volumeLabel, code, "Temp");
            MenuItem musicEnabledItem = new MenuItem();
            musicEnabledItem.ToggleCallbackAction = MenuItem_MusicToggleMute;
            musicEnabledItem.SetName(musicLabel + ": " + code, code, "Temp");
            MenuItem musicVolumeItem = new MenuItem("");
            musicVolumeItem.ChangeLeftCallbackAction = MenuItem_MusicVolumeDown;
            musicVolumeItem.ChangeRightCallbackAction = MenuItem_MusicVolumeUp;
            musicVolumeItem.SetName(musicLabel + volumeLabel, code, "Temp");

            //update details
            MenuItem_UpdateVolumeNumbers(soundEnabledItem, true, false);
            MenuItem_UpdateVolumeNumbers(soundVolumeItem, true, true);
            MenuItem_UpdateVolumeNumbers(musicEnabledItem, false, false);
            MenuItem_UpdateVolumeNumbers(musicVolumeItem, false, true);

            Menu soundMenu = mainMenuTree.CreateMenu(new MenuItem[] { soundEnabledItem, soundVolumeItem, musicEnabledItem, musicVolumeItem });

            //options menu
            MenuItem soundItem = new MenuItem("Sound", soundMenu);
            MenuItem fullScreenItem = new MenuItem();
            fullScreenItem.ToggleCallbackAction = MenuItem_ToggleFullScreen;
            fullScreenItem.SetName("Full Screen CODE", "CODE", fullScreenMode ? "Enabled" : "Disabled");
            MenuItem vibrationItem = new MenuItem();
            vibrationItem.ToggleCallbackAction = MenuItem_ToggleVibration;
            vibrationItem.SetName("Vibration CODE", "CODE", BaseObject.sSystemRegistry.VibrationSystem.IsEnabled(PlayerIndex.One) ? "Enabled" : "Disabled");
            Menu optionsMenu = mainMenuTree.CreateMenu(new MenuItem[] { soundItem, vibrationItem, fullScreenItem });//will be populated with updateSoundMenu method - any text here would be duplicated

            MenuItem newGameLevel = new MenuItem("");
            newGameLevel.ChangeLeftCallbackAction = MenuItem_NewGameLevelDown;
            newGameLevel.ChangeRightCallbackAction = MenuItem_NewGameLevelUp;
            newGameLevel.SetName("Level: " + code, code, newGameLevelNo.ToString());
            MenuItem newGameSpeed = new MenuItem("");
            newGameSpeed.ChangeLeftCallbackAction  = MenuItem_NewGameSpeedDown;
            newGameSpeed.ChangeRightCallbackAction = MenuItem_NewGameSpeedUp;
            newGameSpeed.SetName("Speed: " + code, code, DrMarioGameManager.GetSpeedName(newGameSpeedValue));
            MenuItem startGameItem = new MenuItem("Start Game", MenuItem_NewGame);
            Menu newGameMenu = mainMenuTree.CreateMenu(new MenuItem[] { newGameLevel, newGameSpeed, startGameItem });

            MenuItem[] mainItems;
            MenuItem newGameItem = new MenuItem("New Game", newGameMenu);
            MenuItem optionsItem = new MenuItem("Options", optionsMenu);
            MenuItem aboutItem = new MenuItem("About"); //TODO add credits animaition script
            MenuItem exitItem = new MenuItem("Exit", SystemExit, null);

#if DeveleperModeEnabled
            if (debugMenuEnabled)
                mainItems = new MenuItem[] { newGameItem, optionsItem, aboutItem, exitItem, debugMenuItem };
            else
#endif
            mainItems = new MenuItem[] { newGameItem, optionsItem, aboutItem, exitItem };
            mainMenu = mainMenuTree.CreateMenu(mainItems);


            //pause Menu
            MenuItem resumeItem = new MenuItem("Resume", MenuItem_ResumeGame);
            MenuItem abortGameItem = new MenuItem("Exit To Menu", MenuItem_ExitToMainMenu);
            MenuItem[] pauseItems;
#if DeveleperModeEnabled
            if (debugMenuEnabled)
                pauseItems = new MenuItem[] { resumeItem, optionsItem, abortGameItem, debugMenuItem };
            else
#endif
            pauseItems = new MenuItem[] { resumeItem, optionsItem, abortGameItem };


            pauseMenu = mainMenuTree.CreateMenu(pauseItems);



            ////Initialize other non-menu objects that are part of the main menu
            //Menu mainMenu_ = new Menu(new string[] { "Start Game", "Options", "About" });

            //LoadTileMapFromFile(@"maps\map00-Menu.txt");
            //ResetPanelSize();
            //ResetCharacterPositions();

            //ghosts[0].setGoal(PacMan.MovableObject.Dir.East, tileMap);
            //ghosts[1].setGoal(PacMan.MovableObject.Dir.South, tileMap);
            //ghosts[2].setGoal(PacMan.MovableObject.Dir.West, tileMap);
            //ghosts[3].setGoal(PacMan.MovableObject.Dir.North, tileMap);
        }

        private void MenuItem_NewGame(MenuItem parent)
        {
            mainMenuTree.SwipeAwayMenu(GotoFirstLevel_Animated);
            //mainMenuTree.SwipeAwayMenu(GotoFirstLevel_NotAnimated);
        }

        private void MenuItem_NewGameLevelUp(MenuItem parent)
        {
            if (newGameLevelNo < 20)
                newGameLevelNo++;
            parent.SetNameDetail(newGameLevelNo.ToString());
        }

        private void MenuItem_NewGameLevelDown(MenuItem parent)
        {
            if (newGameLevelNo > 0)
                newGameLevelNo--;
            parent.SetNameDetail(newGameLevelNo.ToString());
        }

        private void MenuItem_NewGameSpeedDown(MenuItem parent)
        {
            newGameSpeedValue--;
            if (newGameSpeedValue < 0)
                newGameSpeedValue = 2;
            parent.SetNameDetail(DrMarioGameManager.GetSpeedName(newGameSpeedValue));
        }

        private void MenuItem_NewGameSpeedUp(MenuItem parent)
        {
            newGameSpeedValue++;
            if (newGameSpeedValue > 2)
                newGameSpeedValue = 0;
            parent.SetNameDetail(DrMarioGameManager.GetSpeedName(newGameSpeedValue));
        }

        private void MenuItem_Debug_ShowVersion(MenuItem parent)
        {
            debugShowVersion = !debugShowVersion;
            parent.SetNameDetail(debugShowVersion ? "Hide" : "Show");
        }

        private void MenuItem_Debug_ShowFPS_ChangeUp(MenuItem parent)
        {
            debugFPSMode = (FPSMode)FreshArchives.GetNextEnum<FPSMode>((int)debugFPSMode, true);
            parent.SetNameDetail("" + debugFPSMode);
        }

        private void MenuItem_Debug_ShowFPS_ChangeDown(MenuItem parent)
        {
            debugFPSMode = (FPSMode)FreshArchives.GetNextEnum<FPSMode>((int)debugFPSMode, false);
            parent.SetNameDetail("" + debugFPSMode);
        }

        private void MenuItem_Debug_ShowVolume(MenuItem parent)
        {
            debugShowVolumeInfo = !debugShowVolumeInfo;
            parent.SetNameDetail(debugShowVolumeInfo ? "Hide" : "Show");
        }

        private void MenuItem_Debug_ShowScore(MenuItem parent)
        {
            debugShowScoreAndTime = !debugShowScoreAndTime;
            parent.SetNameDetail(debugShowScoreAndTime ? "Hide" : "Show");
        }

        private void MenuItem_Debug_ShowObjectMnagerInfo(MenuItem parent)
        {
            debugShowObjectManagerInfo = !debugShowObjectManagerInfo;
            parent.SetNameDetail(debugShowObjectManagerInfo ? "Hide" : "Show");
        }

        private void MenuItem_Debug_ShowInput(MenuItem parent)
        {
            debugShowInputInfo = !debugShowInputInfo;
            parent.SetNameDetail(debugShowInputInfo ? "Hide" : "Show");
        }

        private void MenuItem_Debug_ShowMemory(MenuItem parent)
        {
            debugShowMemoryInfo = !debugShowMemoryInfo;
            parent.SetNameDetail(debugShowMemoryInfo ? "Hide" : "Show");
        }

        private void MenuItem_Debug_CameraBound(MenuItem parent)
        {
            CameraSystem camera = BaseObject.sSystemRegistry.CameraSystem;
            bool bound = !camera.BoundToLevel;
            camera.BoundToLevel = bound;
            parent.SetNameDetail(bound ? "Enabled" : "Disabled");
        }

        private void MenuItem_Debug_FixedStep(MenuItem parent)
        {
            fixedRenderSpeed = !fixedRenderSpeed;
            this.IsFixedTimeStep = fixedRenderSpeed;
            parent.SetNameDetail(fixedRenderSpeed ? "Enabled" : "Disabled");
        }

        private void MenuItem_UpdateVolumeNumbers(MenuItem parent, bool sound, bool volume)
        {
            string detail;
            if (volume)
            {
                float vol = sound ? BaseObject.sSystemRegistry.SoundSystem.SoundEffectsVolume : BaseObject.sSystemRegistry.SoundSystem.MusicVolume;
                detail = "" + +(int)(Math.Round(vol, 2) * 100);
            }
            else
            {
                bool enabled = sound ? !BaseObject.sSystemRegistry.SoundSystem.MuteSoundEffects : !BaseObject.sSystemRegistry.SoundSystem.MuteMusic;
                detail = enabled ? "Enabled" : "Disabled";
            }

            parent.SetNameDetail(detail);
        }

        private void MenuItem_UpdateNewGameLevel(MenuItem parent)
        {
            string detail;

            detail = "" + new Random().Next(100);

            parent.SetNameDetail(detail);
        }

        private void MenuItem_UpdateNewGameSpeed(MenuItem parent)
        {
            string detail;

            detail = "" + new Random().Next(3);

            parent.SetNameDetail(detail);
        }

        private void MenuItem_ToggleFullScreen(MenuItem parent)
        {
            fullScreenMode = !fullScreenMode;

            graphics.IsFullScreen = fullScreenMode;
            graphics.ApplyChanges();

            string detail = fullScreenMode ? "Enabled" : "Disabled";
            parent.SetNameDetail(detail);
        }

        private void MenuItem_ToggleVibration(MenuItem parent)
        {
            PlayerIndex playerNo = PlayerIndex.One;
            VibrationSystem vibSys = BaseObject.sSystemRegistry.VibrationSystem;
            bool enabled = !vibSys.IsEnabled(playerNo);
            vibSys.SetEnabled(playerNo, enabled);

            if (enabled)
                vibSys.Vibrate(playerNo, 0.75f, 0.75f, 0.5);

            string detail = enabled ? "Enabled" : "Disabled";

            parent.SetNameDetail(detail);
        }

        private void MenuItem_SoundEffectsVolumeHover(MenuItem parent)
        {
            menuSoundEffectVollumeTestID++;
            switch (menuSoundEffectVollumeTestID)
            {
                case 0:
                    menuSoundEffectVollumeTest = BaseObject.sSystemRegistry.SoundSystem.PlaySoundEffect(ringSound, true);
                    break;
                case 1:
                    menuSoundEffectVollumeTest = BaseObject.sSystemRegistry.SoundSystem.PlaySoundEffect(droppedRingsSound, true);
                    break;
                case 2:
                    menuSoundEffectVollumeTest = BaseObject.sSystemRegistry.SoundSystem.PlaySoundEffect(saveMarkSound, true);
                    break;
                case 3:
                    menuSoundEffectVollumeTest = BaseObject.sSystemRegistry.SoundSystem.PlaySoundEffect(continueSound, true);
                    break;
                case 4:
                    menuSoundEffectVollumeTest = BaseObject.sSystemRegistry.SoundSystem.PlaySoundEffect(vanishSound, true);
                    break;
                default:
                    menuSoundEffectVollumeTestID = -1;
                    MenuItem_SoundEffectsVolumeHover(parent);
                    break;

            }
        }

        private void MenuItem_SoundEffectsVolumeUnHover(MenuItem parent)
        {
            if (menuSoundEffectVollumeTest != null && (menuSoundEffectVollumeTest.State == SoundState.Playing || menuSoundEffectVollumeTest.State == SoundState.Paused))
            {
                menuSoundEffectVollumeTest.Stop();
            }
        }

        private void MenuItem_SoundEffectVolumeUp(MenuItem parent)
        {
            BaseObject.sSystemRegistry.SoundSystem.TurnUpSoundEffectsVolume();
            MenuItem_UpdateVolumeNumbers(parent, true, true);
        }

        private void MenuItem_SoundEffectVolumeDown(MenuItem parent)
        {
            BaseObject.sSystemRegistry.SoundSystem.TurnDownSoundEffectsVolume();
            MenuItem_UpdateVolumeNumbers(parent, true, true);
        }

        private void MenuItem_SoundEffectToggleMute(MenuItem parent)
        {
            BaseObject.sSystemRegistry.SoundSystem.MuteSoundEffects = !BaseObject.sSystemRegistry.SoundSystem.MuteSoundEffects;
            MenuItem_UpdateVolumeNumbers(parent, true, false);
        }

        private void MenuItem_MusicVolumeUp(MenuItem parent)
        {
            BaseObject.sSystemRegistry.SoundSystem.TurnUpMusicVolume();
            MenuItem_UpdateVolumeNumbers(parent, false, true);
        }

        private void MenuItem_MusicVolumeDown(MenuItem parent)
        {
            BaseObject.sSystemRegistry.SoundSystem.TurnDownMusicVolume();
            MenuItem_UpdateVolumeNumbers(parent, false, true);
        }

        private void MenuItem_MusicToggleMute(MenuItem parent)
        {
            BaseObject.sSystemRegistry.SoundSystem.MuteMusic = !BaseObject.sSystemRegistry.SoundSystem.MuteMusic;
            MenuItem_UpdateVolumeNumbers(parent, false, false);
        }

        private void MenuItem_ResumeGame()
        {
            Paused();
        }

        private void MenuItem_ExitToMainMenu()
        {
            {
                GotoMainMenu();
            }
        }

        private void Menu_ClearCurrentMenu()
        {
            mainMenuTree.SetMenu(null);
        }

        /// <summary>
        /// Load the scipt file and its variables from the default script file - Called at the head of the Initialize method for setting up variables ahead of time
        /// </summary>
        private void LoadScriptVariablesPart1()
        {
            settingsFile = new VariablesFile(settingsFilePath, null, false);
            VariableLibrary vars = settingsFile.variables;

            //debug variables
            vars.GetVariable("debugMenuEnabled", ref debugMenuEnabled, true);
            vars.GetVariable("debugShowVersion", ref debugShowVersion, true);
            vars.GetVariable("debugShowScoreAndTime", ref debugShowScoreAndTime, true);
            vars.GetVariable("debugShowInputInfo", ref debugShowInputInfo, true);
            vars.GetVariable("debugShowVolumeInfo", ref debugShowVolumeInfo, true);
            vars.GetVariable("debugShowMemoryInfo", ref debugShowMemoryInfo, true);
            vars.GetVariable("debugShowObjectManagerInfo", ref debugShowObjectManagerInfo, true);

            int val = (int)debugFPSMode;
            vars.GetVariable("debugShowFPS", ref val, true);
            debugFPSMode = (FPSMode)val;

            //graphics fixed step
            vars.GetVariable("FixedRenderSpeed", ref fixedRenderSpeed, true);

            //menu Varibles
            vars.GetVariable("menuTransitionTime", ref menuTransitionTime, true);
            vars.GetVariable("menuTitleHeight", ref menuTitleHeight, true);
            vars.GetVariable("menuTitle", ref menuTitle, true);
            vars.GetVariable("menuWrapSelection", ref menuWrapSelection, true);
            //vars.getVariable("MovementSpeed",
            //    ref MovableObject.MOVE_RATE, true);

            ////Movement Guide variables
            //vars.getVariable("MovementGuideEnabled",
            //    ref MovableObject.MovementGuideEnabled, true);
            //vars.getVariable("MovementGuideSpeedRelativeToDistance",
            //    ref MovableObject.MovementGuideSpeedRelativeToDistance, true);
            //vars.getVariable("MovementGuideMoveTillTouchingWall",
            //    ref MovableObject.MovementGuideMoveTillTouchingWall, true);
            //vars.getVariable("MovementGuideTouchingWallDistance",
            //    ref MovableObject.MovementGuideTouchingWallDistance, true);
        }

        /// <summary>
        /// Implements script variables - called at the end of the initialize, after the systems have been setup
        /// </summary>
        private void LoadScriptVariablesPart2()
        {
            Variable workspaceVar;
            bool found = false;

            VariableLibrary vars = settingsFile.variables;

            //Set Camera Bounded
            workspaceVar = vars.GetVariable("CameraBoundToLevel", BaseObject.sSystemRegistry.CameraSystem.BoundToLevel, out found);
            if (found) BaseObject.sSystemRegistry.CameraSystem.BoundToLevel = workspaceVar.Boolean;

            //sound variables - extended properties that cant be passed so using out found var
            workspaceVar = vars.GetVariable("SoundEffectsVolume", BaseObject.sSystemRegistry.SoundSystem.SoundEffectsVolume, out found);
            if (found) BaseObject.sSystemRegistry.SoundSystem.SoundEffectsVolume = workspaceVar.Float;
            workspaceVar = vars.GetVariable("SoundEffectsMute", BaseObject.sSystemRegistry.SoundSystem.MuteSoundEffects, out found);
            if (found) BaseObject.sSystemRegistry.SoundSystem.MuteSoundEffects = workspaceVar.Boolean;
            workspaceVar = vars.GetVariable("MusicVolume", BaseObject.sSystemRegistry.SoundSystem.MusicVolume, out found);
            if (found) BaseObject.sSystemRegistry.SoundSystem.MusicVolume = workspaceVar.Float;
            workspaceVar = vars.GetVariable("MusicMute", BaseObject.sSystemRegistry.SoundSystem.MuteMusic, out found);
            if (found) BaseObject.sSystemRegistry.SoundSystem.MuteMusic = workspaceVar.Boolean;

            //load game variables
            string gameVariablesPath = @"Content\scripts\gameVariables.conf";
            vars.GetVariable("gameVariablesPath", ref gameVariablesPath, true);

            VariablesFile gameVariablesFile = new VariablesFile(gameVariablesPath, null, true);
            VariableLibrary gameVars = gameVariablesFile.variables;

            DrMarioGameObjectFactory factory = (DrMarioGameObjectFactory)BaseObject.sSystemRegistry.GameObjectFactory;
            factory.LoadSettings(gameVars);
        }

        /// <summary>
        /// make sure script variables that can be changed in game are updated in script file and Save them out in the script file
        /// </summary>
        private void SaveVariables()
        {
            settingsFile.variables.SetValue("debugMenuEnabled", "" + debugMenuEnabled);
            settingsFile.variables.SetValue("debugShowVersion", "" + debugShowVersion);
            settingsFile.variables.SetValue("debugShowScoreAndTime", "" + debugShowScoreAndTime);
            settingsFile.variables.SetValue("debugShowInputInfo", "" + debugShowInputInfo);
            settingsFile.variables.SetValue("debugShowMemoryInfo", "" + debugShowMemoryInfo);
            settingsFile.variables.SetValue("debugShowVolumeInfo", "" + debugShowVolumeInfo);
            settingsFile.variables.SetValue("debugShowObjectManagerInfo", "" + debugShowObjectManagerInfo);
            settingsFile.variables.SetValue("debugShowFPS", "" + (int)debugFPSMode);

            settingsFile.variables.SetValue("FixedRenderSpeed", "" + fixedRenderSpeed);

            settingsFile.variables.SetValue("CameraBoundToLevel", "" + BaseObject.sSystemRegistry.CameraSystem.BoundToLevel);

            settingsFile.variables.SetValue("SoundEffectsVolume", "" + Math.Round(BaseObject.sSystemRegistry.SoundSystem.SoundEffectsVolume, 2));
            settingsFile.variables.SetValue("SoundEffectsMute", "" + BaseObject.sSystemRegistry.SoundSystem.MuteSoundEffects);
            settingsFile.variables.SetValue("MusicVolume", "" + Math.Round(BaseObject.sSystemRegistry.SoundSystem.MusicVolume, 2));
            settingsFile.variables.SetValue("MusicMute", "" + BaseObject.sSystemRegistry.SoundSystem.MuteMusic);

            settingsFile.SaveAs(settingsFilePath);
        }

        public void StartLevel()
        {
            //called after loading level completes
            
            //center camera on game area
            BaseObject.sSystemRegistry.CameraSystem.SetFocusPosition(BaseObject.sSystemRegistry.ContextParameters.GameWidth / 2, BaseObject.sSystemRegistry.ContextParameters.GameHeight / 2);
            BaseObject.sSystemRegistry.CameraSystem.ExternalControl = true;

            drMarioManager.StartGame(newGameLevelNo, newGameSpeedValue);
            
            gameState = GameStates.MainLoop;
        }

        private void GotoFirstLevel_Animated() { GotoFirstLevel(true); }
        private void GotoFirstLevel_NotAnimated() { GotoFirstLevel(false); }

        private void GotoFirstLevel(bool animated)
        {
            ///continue...
            MediaPlayer.Stop();
            MediaPlayer.Play(backgroundGameMusic);
            MediaPlayer.IsRepeating = true;

            //BUG reset systems - like vibration - playerInput - gameEvents
            gameState = GameStates.LoadingLevel;

            levelSystem.GotoFirstLevel(animated, StartLevel);
        }

        private void GotoNextLevel_Animated(bool animated, Action postAnimationAction)
        {
            gameState = GameStates.LoadingLevel;

            levelSystem.GotoNextLevel(animated, StartLevel);
        }

        public void ToggleGodMode()
        {
            BaseObject.sSystemRegistry.SoundSystem.PlaySoundEffect(shotgunCockSound, false);
            cheatGodMode = !cheatGodMode;

            GameObject player = gameManager.GetPlayer();
            if (player != null)
            {
                HitReactionComponent reactComp = player.FindByType<HitReactionComponent>();
                if (reactComp != null)
                {
                    reactComp.setForceInvincible(cheatGodMode);
                }
            }
        }

        public void ToggleNoClip()
        {
            BaseObject.sSystemRegistry.SoundSystem.PlaySoundEffect(shotgunCockSound, false);
            cheatNoClip = !cheatNoClip;

            GameObject player = gameManager.GetPlayer();
            if (player != null)
            {
                BackgroundCollisionComponent backCol = player.FindByType<BackgroundCollisionComponent>();
                if (backCol != null)
                {
                    backCol.Enabled = !cheatNoClip;
                }
            }
        }

        public void SendGameEvent(int eventID, float secondsDelay)
        {
            timeTillGameEvent = secondsDelay;
            lastGameEvent = eventID;
        }

        public void HandleGameEvent(float updateTime)
        {
            if (lastGameEvent != -1)
            {
                if (timeTillGameEvent > 0)
                {
                    timeTillGameEvent -= updateTime;
                }

                if (timeTillGameEvent <= 0)
                {
                    switch (lastGameEvent)
                    {
                        default:
                        case -1:
                            //do nothing - -1 isn't realy possible here but still a good catch all
                            break;
                        case 0:
                            PlayerDied();
                            break;
                        case 1:
                            CompletedLevel();
                            break;
                    }
                    lastGameEvent = -1;
                }
            }
        }

        private void PlayerDied()
        {
            {
                RestartLevel();
            }
        }

        public void CompletedLevel()
        {
            //should save progress, stats, and do ther level end stuff - maybe unload level content
            //BaseObject.sSystemRegistry.SoundSystem.PlaySoundEffect(vanishSound, false);


            bool returnToMainMenuAfterLevels = true;
            bool finishedAllLevels = levelSystem.OnLastLevel;
            if (!finishedAllLevels || !returnToMainMenuAfterLevels)
                GotoNextLevel_Animated(false, null);
            else
                GotoMainMenu();
        }

        public void RestartLevel()
        {
            levelSystem.RebuildCurrentLevel();
            BaseObject.sSystemRegistry.SoundSystem.ClearAll();
            StartLevel();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {

        }

        protected override void OnExiting(Object sender, EventArgs args)
        {
            base.OnExiting(sender, args);

            // Stop the threads'
            Profiler.SaveDataToFile(@"Content\scripts\profileData.txt", false);
            SystemExit();
        }

        /// <summary>
        /// Game Ending - Saves any data, closes open files, and end threads
        /// </summary>
        private void SystemExit()
        {

            //only necisary to save out the variables if they have been changed since the app was launched - visa-vi in app settings/stats/ect
            //Menu_ClearCurrentMenu();//probably not nececisay
            SaveVariables();

            //safty stop all vibration
            for (int xx = 0; xx < 4; xx++)
                GamePad.SetVibration((PlayerIndex)xx, 0, 0);

            this.Exit();
        }

        private void UpdatePlayerInput(GamePadState gamePad, KeyboardState keyState)
        {
            //PlayerController.MovementDir = new Vector2(0, 0);
            Point moveDir = Point.Zero;
            bool rotLeftPressed = false;
            bool rotRightPressed = false;

            if (controllerInputEnabled)
            {
                ////save controller data so it can be accessed by the player component
                //moveDir.X += (int)Math.Round(gamePad.ThumbSticks.Left.X, 0);
                ////negative Y because 0,0 is the top left of the screen
                //moveDir.Y -= (int)Math.Round(gamePad.ThumbSticks.Left.Y, 0);

                if (gamePad.IsButtonDown(Buttons.DPadUp) || gamePad.IsButtonDown(Buttons.LeftThumbstickUp))
                    moveDir.Y += -1;
                if (gamePad.IsButtonDown(Buttons.DPadDown) || gamePad.IsButtonDown(Buttons.LeftThumbstickDown))
                    moveDir.Y += 1;
                if (gamePad.IsButtonDown(Buttons.DPadLeft) || gamePad.IsButtonDown(Buttons.LeftThumbstickLeft))
                    moveDir.X += -1;
                if (gamePad.IsButtonDown(Buttons.DPadRight) || gamePad.IsButtonDown(Buttons.LeftThumbstickRight))
                    moveDir.X += 1;

                if (gamePad.Buttons.A == ButtonState.Pressed || gamePad.Buttons.X == ButtonState.Pressed)
                    rotLeftPressed = true;
                if (gamePad.Buttons.B == ButtonState.Pressed || gamePad.Buttons.Y == ButtonState.Pressed)
                    rotRightPressed = true;
            }


            //movementDir - add all directions and normalize to 1
            Keys[] keys = keyState.GetPressedKeys();
            foreach (Keys k in keys)
            {
                switch (k)
                {
                    case Keys.W:
                    case Keys.Up:
                        moveDir.Y += -1;
                        break;
                    case Keys.S:
                    case Keys.Down:
                        moveDir.Y += 1;
                        break;
                    case Keys.A:
                    case Keys.Left:
                        moveDir.X += -1;
                        break;
                    case Keys.D:
                    case Keys.Right:
                        moveDir.X += 1;
                        break;
                    case Keys.Z:
                        rotLeftPressed = true;
                        break;
                    case Keys.X:
                        rotRightPressed = true;
                        break;
                }
            }

            if (moveDir.X < -1)
                moveDir.X = -1;
            else if (moveDir.X > 1)
                moveDir.X = 1;
            if (moveDir.Y < -1)
                moveDir.Y = -1;
            else if (moveDir.Y > 1)
                moveDir.Y = 1;

            PlayerController.MovementDir = moveDir;
            PlayerController.RotLeftPressed = rotLeftPressed;
            PlayerController.RotRightPressed = rotRightPressed;
        }

        private void UpdateAllInput()
        {
            if (cheatsEnabled)
                cheatCodeSystem.Update();

            if (controllerInputEnabled)
            {
                GamePadState gamePad = GamePad.GetState(PlayerIndex.One);
                if (gamePad.IsConnected)
                {
                    if (gamePad.PacketNumber != oldGamePadState.PacketNumber)
                    {
                        //act on controller events - not held buttons - mostly for menu
                        Buttons[] bottonsToListenFor = { };

                        if (gameState == GameStates.MainLoop || gameState == GameStates.LoadingLevel)
                        {
                            bottonsToListenFor = new Buttons[] { Buttons.Back, Buttons.Start, Buttons.LeftShoulder, Buttons.RightShoulder };
                        }
                        else if (gameState == GameStates.MainMenu || gameState == GameStates.Paused)
                        {
                            bottonsToListenFor = new Buttons[] { Buttons.LeftThumbstickDown, Buttons.LeftThumbstickUp, Buttons.LeftThumbstickRight, Buttons.LeftThumbstickLeft, Buttons.DPadDown, Buttons.DPadUp, Buttons.DPadLeft, Buttons.DPadRight, Buttons.A, Buttons.B, Buttons.Start };
                        }

                        foreach (Buttons b in bottonsToListenFor)
                        {
                            if (gamePad.IsButtonDown(b))
                            {
                                // If not down last update, key has just been pressed.
                                if (oldGamePadState.IsButtonUp(b))
                                {
                                    //Key pressed
                                    if (gameState == GameStates.MainLoop || gameState == GameStates.LoadingLevel)
                                    {
                                        ControllerEvent_Game(b, true);
                                    }
                                    else if (gameState == GameStates.MainMenu || gameState == GameStates.Paused)
                                        ControllerEvent_MainMenu(b, true);
                                }
                            }
                            else if (oldGamePadState.IsButtonDown(b))
                            {
                                //Controller event released
                                if (gameState == GameStates.MainLoop || gameState == GameStates.LoadingLevel)
                                {
                                    ControllerEvent_Game(b, false);
                                }
                                else if (gameState == GameStates.MainMenu || gameState == GameStates.Paused)
                                    ControllerEvent_MainMenu(b, false);
                            }
                        }
                    }
                }
                oldGamePadState = GamePad.GetState(PlayerIndex.One);
            }

            //keyboard  press/release event Input - not held Keys - directions left here for menu
            KeyboardState keyState = Keyboard.GetState();
            Keys[] keysToListenFor = { Keys.F2, Keys.Escape, Keys.W, Keys.A, Keys.S, Keys.D, Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.Space, Keys.Enter, Keys.OemTilde, Keys.D1, Keys.D2, Keys.D3, Keys.Z, Keys.X };


            foreach (Keys key in keysToListenFor)
            {
                if (keyState.IsKeyDown(key))
                {
                    // If not down last update, key has just been pressed.
                    if (!oldKeyState.IsKeyDown(key))
                    {
                        //Key pressed
                        if (gameState == GameStates.MainLoop || gameState == GameStates.LoadingLevel)
                            KeyEvent_Game(key, true);
                        else if (gameState == GameStates.MainMenu || gameState == GameStates.Paused)
                            KeyEvent_MainMenu(key, true);
                    }
                }
                else if (oldKeyState.IsKeyDown(key))
                {
                    //Key released
                    if (gameState == GameStates.MainLoop || gameState == GameStates.LoadingLevel)
                        KeyEvent_Game(key, false);
                    else if (gameState == GameStates.MainMenu || gameState == GameStates.Paused)
                        KeyEvent_MainMenu(key, false);
                }
            }

            // Update saved state.
            oldKeyState = keyState;

            if (gameState == GameStates.MainLoop)
            {
                UpdatePlayerInput(oldGamePadState, oldKeyState);
            }
        }

        private void ControllerEvent_MainMenu(Buttons button, bool pressed)
        {
            Keys keyMaped = Keys.None;
            switch (button)
            {
                case Buttons.DPadUp:
                case Buttons.LeftThumbstickUp:
                    keyMaped = Keys.Up;
                    break;
                case Buttons.DPadDown:
                case Buttons.LeftThumbstickDown:
                    keyMaped = Keys.Down;
                    break;
                case Buttons.DPadLeft:
                case Buttons.LeftThumbstickLeft:
                    keyMaped = Keys.Left;
                    break;
                case Buttons.DPadRight:
                case Buttons.LeftThumbstickRight:
                    keyMaped = Keys.Right;
                    break;
                case Buttons.A:
                    keyMaped = Keys.Enter;
                    break;
                case Buttons.Start:
                case Buttons.B:
                    keyMaped = Keys.Escape;
                    break;
            }
            KeyEvent_MainMenu(keyMaped, pressed);
        }

        private void ControllerEvent_Game(Buttons button, bool pressed)
        {
            Keys keyMaped = Keys.None;
            switch (button)
            {
                case Buttons.Back:
                    break;
                case Buttons.Start:
                    keyMaped = Keys.Escape;
                    break;
                case Buttons.LeftShoulder:
                    keyMaped = Keys.OemTilde;
                    break;
                case Buttons.RightShoulder:
                    keyMaped = Keys.D1;
                    break;
            }
            KeyEvent_Game(keyMaped, pressed);
        }

        private void KeyEvent_MainMenu(Keys key, bool pressed)
        {
            switch (key)
            {
                case Keys.Escape:
                    if (pressed)
                    {
                        if (mainMenuTree.CurrentMenu == pauseMenu)
                        {
                            MenuItem_ResumeGame();
                        }
                        else
                        {
                            mainMenuTree.BackPressed();
                        }
                    }
                    break;
                case Keys.W:
                case Keys.Up:
                    if (pressed)
                        mainMenuTree.UpPressed();
                    break;
                case Keys.A:
                case Keys.Left:
                    if (pressed)
                        mainMenuTree.LeftPressed();
                    break;
                case Keys.S:
                case Keys.Down:
                    if (pressed)
                        mainMenuTree.DownPressed();
                    break;
                case Keys.D:
                case Keys.Right:
                    if (pressed)
                        mainMenuTree.RightPressed();
                    break;
                case Keys.Space:
                case Keys.Enter:
                    if (pressed)
                        mainMenuTree.EnterPressed();
                    break;
            }
        }

        private void KeyEvent_Game(Keys key, bool pressed)
        {
            switch (key)
            {
                case Keys.Escape:
                    if (pressed)
                        Paused();
                    //dont care about release
                    break;
                case Keys.F2:
                    if (pressed)
                    {
                        RestartLevel();
                    }
                    //dont care about release
                    break;
                case Keys.OemTilde://dev stuff
                    if (pressed)
                    {
                        //re-run this level animation - and reload level
                        //gameState = GameStatus.LoadingLevel;
                        //CurrentLevel.LoadFromFile();
                        //AnimateLevelLoad();

                        RestartLevel();
                    }
                    //dont care about release
                    break;
                case Keys.D1://dev stuff
                    if (pressed)
                    {
                        //load next level
                        GotoNextLevel_Animated(false, StartLevel);
                    }
                    //dont care about release
                    break;
                case Keys.D2://dev stuff
                    if (pressed)
                    {
                        //return to main menu
                        //GotoMainMenu();
                        //BaseObject.sSystemRegistry.VibrationSystem.Vibrate(PlayerIndex.One, 0.7f, 0.7f, 0.75f);
                    }
                    //dont care about release
                    break;
                case Keys.D3://dev stuff
                    break;

            }
        }

        /// <summary>
        /// pauses all game logic - this is only called from main game loop
        /// </summary>
        public void Paused()
        {
            if (gameState == GameStates.MainLoop || gameState == GameStates.LoadingLevel)
            {
                //pause game
                previousGameState = gameState;
                gameState = GameStates.Paused;

                if (pauseMusicOnPause)
                    BaseObject.sSystemRegistry.SoundSystem.PauseAllMusic();
                BaseObject.sSystemRegistry.SoundSystem.PauseAllSounds();

                mainMenuTree.SetMenu(pauseMenu);
            }
            else if (gameState == GameStates.Paused)
            {
                //clear any keys that were pressed when game was paused
                PlayerController.Reset();

                //unpause
                //could add action here to delar resume of the game till this animation is complete
                mainMenuTree.SwipeAwayMenu(Menu_ClearCurrentMenu);

                gameState = previousGameState;
                if (pauseMusicOnPause)
                    BaseObject.sSystemRegistry.SoundSystem.ResumeAllMusic();
                BaseObject.sSystemRegistry.SoundSystem.ResumeAllSounds();
            }

            //play sound here - play after pause all
            BaseObject.sSystemRegistry.SoundSystem.PlaySoundEffect(mainMenuTree.EnterActionSound, false);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            double curTime = gameTime.TotalGameTime.TotalSeconds;
            float updateSeconds = (float)(curTime - lastUpdateTime);
            lastUpdateTime = curTime;

            if (this.IsActive)
            {
                {
                    Profiler.Start();

                    BaseObject.sSystemRegistry.HudSystem.Enabled = true;

                    UpdateAllInput();

                    //also catches sounds in main menu
                    soundSystem.Update(updateSeconds, null);
                    BaseObject.sSystemRegistry.VibrationSystem.Update(updateSeconds, null);

                    if (mainMenuTree.RootMenu == pauseMenu)
                    {
                        mainMenuTree.Update(updateSeconds);
                    }

                    if (gameState == GameStates.MainLoop || gameState == GameStates.LoadingLevel)
                    {
                        gameRoot.Update(updateSeconds, null);
                        renderSystem.SwapInNextQueue();

                        if (gameState == GameStates.MainLoop)
                        {
                            HandleGameEvent(updateSeconds);
                            totalPlayTime = totalPlayTime.Add(gameTime.ElapsedGameTime);
                        }
                        else if (gameState == GameStates.LoadingLevel)
                        {
                            BaseObject.sSystemRegistry.LevelSystem.Update(updateSeconds, null);
                        }
                    }
                    else if (gameState == GameStates.MainMenu)
                    {
                        mainMenuTree.Update(updateSeconds);
                    }

                    base.Update(gameTime);
                    Profiler.Stop();
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            frameNo++;

            double curTime = gameTime.TotalGameTime.TotalSeconds;
            float drawSeconds = (float)(curTime - lastDrawTime);
            lastDrawTime = curTime;

            spriteBatch.Begin();

            //drawBackground
            //spriteBatch.Draw(backgroundPic, Vector2.Zero, Color.White);

            GraphicsDevice.Clear(Color.Black);

            if (gameState == GameStates.MainLoop || gameState == GameStates.Paused || gameState == GameStates.LoadingLevel || gameState == GameStates.LevelEditor)
            {
                renderSystem.Draw(gameState != GameStates.MainLoop || !this.IsActive);

                if (mainMenuTree.RootMenu == pauseMenu)
                {
                    //draw pause menu
                    mainMenuTree.Draw(gameTime, spriteBatch);
                }
            }
            else if (gameState == GameStates.MainMenu)
            {
                mainMenuTree.Draw(gameTime, spriteBatch);
            }

            Draw_FPS(drawSeconds);

            spriteBatch.End();

            base.Draw(gameTime);

            lastFrameTime = DateTime.Now;
        }

        private void Draw_FPS(float updateSeconds)
        {
            int gameWidth = BaseObject.sSystemRegistry.ContextParameters.GameWidth;
            int gameHeight = BaseObject.sSystemRegistry.ContextParameters.GameHeight;

            double updateMiliseconds = updateSeconds * 1000;
            profileFrameTime += updateMiliseconds;
            //min and max
            if (updateMiliseconds < profileMinFrameTime)
                profileMinFrameTime = updateMiliseconds;
            if (updateMiliseconds > profileMaxFrameTime)
                profileMaxFrameTime = updateMiliseconds;

            profileFrames++;
            if (profileFrameTime > FPS_PROFILE_REPORT_DELAY)
            {

                lastProfileTotalTime = profileFrameTime;
                lastProfileTotalFrames = profileFrames;
                profileLastFPS = 1.0f * lastProfileTotalFrames / (lastProfileTotalTime / 1000f);
                lastProfileMinFrameTime = profileMaxFrameTime;//min time is max FPS
                lastProfileMaxFrameTime = profileMinFrameTime;

                profileFrameTime = 0;
                profileFrames = 0;
                profileMinFrameTime = double.MaxValue;
                profileMaxFrameTime = double.MinValue;
                //profileObjectCount = 0;
            }

            int fontSize = 15;
            Color FPSColor = Color.LimeGreen;
            Color FPSWarningColor = Color.Orange;
            Color FPSBadColor = Color.Red;
            const int singleFrameWarning = 50;
            const int fpsBad = 55;

            if (debugFPSMode != FPSMode.Off || debugShowVersion)
            {
                bool badFPSWarning = false;
                bool warningBadFrame = false;
                Point textAlign = new Point(0, 0);
                int lineHeight = fontSize;
                int xPos = 0;
                int yPos = 0;

                List<string> data = new List<string>();

                if (debugShowVersion)
                {
                    Version v = Assembly.GetExecutingAssembly().GetName().Version;
                    string versionString = v.Major + "." + v.Minor + "." + v.Build + "." + v.Revision;
                    data.Add("Version:" + versionString);
                }

                if (debugFPSMode == FPSMode.Verbose)
                {
                    string fpsFormat = "F1";
                    string seondFormat = "F2";
                    string miliseondFormat = "F2";
                    data.Add("FPS=" + profileLastFPS.ToString(fpsFormat) + "  (" + lastProfileTotalFrames + "/" + (lastProfileTotalTime / 1000).ToString(seondFormat) + "s)");
                    data.Add("Frame#" + frameNo);

                    //Min X FPS (time)
                    double minFPSEstimation = 1000 / lastProfileMinFrameTime;
                    string minFrameTimeST = "Min=" + minFPSEstimation.ToString(fpsFormat) + "(" + lastProfileMinFrameTime.ToString(miliseondFormat) + "ms)";
                    double maxFPSEstimation = 1000 / lastProfileMaxFrameTime;
                    string maxFrameTimeST = "Max=" + maxFPSEstimation.ToString(fpsFormat) + "(" + lastProfileMaxFrameTime.ToString(miliseondFormat) + "ms)";
                    data.Add(minFrameTimeST + " - " + maxFrameTimeST);

                    if (cheatGodMode)
                        data.Add("GodMode Enabled");
                    if (cheatNoClip)
                        data.Add("NoClip Enabled");

                    if (minFPSEstimation <= singleFrameWarning)
                        warningBadFrame = true;
                }
                else if (debugFPSMode == FPSMode.On)
                {
                    string fpsFormat = "F2";
                    data.Add("FPS:" + profileLastFPS.ToString(fpsFormat));
                }

                data.Add("GameState=" + gameState);

                if (profileLastFPS < fpsBad)
                    badFPSWarning = true;

                Color outputColor;
                if (badFPSWarning)
                    outputColor = FPSBadColor;
                else if (warningBadFrame)
                    outputColor = FPSWarningColor;
                else
                    outputColor = FPSColor;

                FreshArchives.DrawStringAdvanced(spriteBatch, debugFont, data, xPos, yPos, lineHeight, outputColor, textAlign);
            }


            if (debugShowScoreAndTime)
            {
                Point textAlign = new Point(0, 0);
                int lineHeight = fontSize;
                int xPos = 0;
                int yPos = lineHeight * 6;

                List<string> data = new List<string>();

                //data.Add("Score: " + score + " - Level Index:" + levelSystem.CurrentLevelNo);
                data.Add("playTime: " + totalPlayTime);
                //data.Add("TotalTime: " + gameTime.TotalGameTime);

                FreshArchives.DrawStringAdvanced(spriteBatch, debugFont, data, xPos, yPos, lineHeight, FPSColor, textAlign);
            }

            if (debugShowMemoryInfo)
            {
                Point textAlign = new Point(1, 1);
                int lineHeight = -fontSize;
                int xPos = gameWidth;
                int yPos = gameHeight;

                List<string> data = new List<string>();

                Process myProcess = null;
                myProcess = Process.GetCurrentProcess();

                const string seperator = " - ";
                const int decimals = 2;
                //data.Add(FreshArchives.ByteCountToReadableString(myProcess.PagedMemorySize64, decimals) + seperator + "PagedMemorySize64");
                //data.Add(FreshArchives.ByteCountToReadableString(myProcess.PeakPagedMemorySize64, decimals) + seperator + "PeakPagedMemorySize64");
                //data.Add(FreshArchives.ByteCountToReadableString(myProcess.NonpagedSystemMemorySize64, decimals) + seperator + "NonpagedSystemMemorySize64");
                //data.Add(FreshArchives.ByteCountToReadableString(myProcess.VirtualMemorySize64, decimals) + seperator + "VirtualMemorySize64");
                //data.Add(FreshArchives.ByteCountToReadableString(myProcess.PeakVirtualMemorySize64, decimals) + seperator + "PeakVirtualMemorySize64");
                //data.Add(FreshArchives.ByteCountToReadableString(myProcess.WorkingSet64, decimals) + seperator + "WorkingSet64");
                //data.Add(FreshArchives.ByteCountToReadableString(myProcess.PeakWorkingSet64, decimals) + seperator + "PeakWorkingSet64");
                //data.Add(FreshArchives.ByteCountToReadableString(myProcess.PrivateMemorySize64, decimals) + seperator + "PrivateMemorySize64");

                data.Add("PagedMemorySize64" + seperator + FreshArchives.ByteCountToReadableString(myProcess.PagedMemorySize64, decimals));
                data.Add("PeakPagedMemorySize64" + seperator + FreshArchives.ByteCountToReadableString(myProcess.PeakPagedMemorySize64, decimals));
                data.Add("NonpagedSystemMemorySize64" + seperator + FreshArchives.ByteCountToReadableString(myProcess.NonpagedSystemMemorySize64, decimals));
                data.Add("VirtualMemorySize64" + seperator + FreshArchives.ByteCountToReadableString(myProcess.VirtualMemorySize64, decimals));
                data.Add("PeakVirtualMemorySize64" + seperator + FreshArchives.ByteCountToReadableString(myProcess.PeakVirtualMemorySize64, decimals));
                data.Add("WorkingSet64" + seperator + FreshArchives.ByteCountToReadableString(myProcess.WorkingSet64, decimals));
                data.Add("PeakWorkingSet64" + seperator + FreshArchives.ByteCountToReadableString(myProcess.PeakWorkingSet64, decimals));
                data.Add("PrivateMemorySize64" + seperator + FreshArchives.ByteCountToReadableString(myProcess.PrivateMemorySize64, decimals));

                data.Reverse(0, data.Count);
                FreshArchives.DrawStringAdvanced(spriteBatch, debugFont, data, xPos, yPos, lineHeight, FPSColor, textAlign);
            }


            if (debugShowObjectManagerInfo)
            {
                Point textAlign = new Point(0, 0);
                int lineHeight = fontSize;
                int xPos = 0;
                int yPos = lineHeight * 10;

                List<string> data = new List<string>();

                data.Add("Render Objects: " + BaseObject.sSystemRegistry.RenderSystem.RenderObjectsCount);
                data.Add("Game Objects: " + BaseObject.sSystemRegistry.GameObjectManager.TotalObjectsCount);
                data.Add("Game Objects Active: " + BaseObject.sSystemRegistry.GameObjectManager.ActiveObjectsCount);
                data.Add("Game Objects Inactive: " + BaseObject.sSystemRegistry.GameObjectManager.InactiveObjectsCount);
                data.Add("Game Objects Deleting: " + BaseObject.sSystemRegistry.GameObjectManager.MarkedForDeathObjectsCount);
                data.Add("Game Root count: " + gameRoot.Count);
                data.Add("Temp CollisionSegments: " + BaseObject.sSystemRegistry.CollisionSystem.TemporarySegments);
                data.Add("Dynamic CollisionObjects: " + BaseObject.sSystemRegistry.GameObjectCollisionSystem.ActiveCollisionObjects);
                //data.Add("Sounds: " + BaseObject.sSystemRegistry.SoundSystem.);

                FreshArchives.DrawStringAdvanced(spriteBatch, debugFont, data, xPos, yPos, lineHeight, FPSColor, textAlign);
            }

            //show Input Info
            if (debugShowInputInfo)
            {
                Point textAlign = new Point(0, 1);
                int lineHeight = -fontSize;
                int xPos = 0;
                int yPos = gameHeight;

                List<string> data = new List<string>();

                //data.Add("Mouse Location = " + oldMouseState.X + "," + oldMouseState.Y);
                //data.Add("Mouse ScrollWheelValue = " + oldMouseState.ScrollWheelValue);
                //data.Add("Mouse Left Button pressed = " + (oldMouseState.LeftButton == ButtonState.Pressed));
                //data.Add("Mouse Right Button pressed = " + (oldMouseState.RightButton == ButtonState.Pressed));
                //data.Add("Mouse Middle Button pressed = " + (oldMouseState.MiddleButton == ButtonState.Pressed));
                //data.Add("Mouse XButton1 pressed = " + (oldMouseState.XButton1 == ButtonState.Pressed));
                //data.Add("Mouse XButton2 pressed = " + (oldMouseState.XButton2 == ButtonState.Pressed));

                //gamePad Info
                int playerNo = 0;
                //foreach (GamePadState gamePadState in oldGamePadStates)
                GamePadState gamePadState = oldGamePadState;
                {
                    playerNo++;
                    bool connected = gamePadState.IsConnected;
                    data.Add("GamePad#" + playerNo + " Connected = " + connected);

                    //if (playerNo == 1) connected = true;

                    if (connected)
                    {
                        data.Add("--GamePad#" + playerNo + " Left Thumb Stick Value = " + gamePadState.ThumbSticks.Left.X + "," + gamePadState.ThumbSticks.Left.Y);
                        data.Add("--GamePad#" + playerNo + " Right Thumb Stick Value = " + gamePadState.ThumbSticks.Right.X + "," + gamePadState.ThumbSticks.Right.Y);
                        data.Add("--GamePad#" + playerNo + " D-Pad Up pressed = " + (gamePadState.DPad.Up == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " D-Pad Right pressed = " + (gamePadState.DPad.Right == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " D-Pad Down pressed = " + (gamePadState.DPad.Down == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " D-Pad Left pressed = " + (gamePadState.DPad.Left == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button A pressed = " + (gamePadState.Buttons.A == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button B pressed = " + (gamePadState.Buttons.B == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button X pressed = " + (gamePadState.Buttons.X == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button Y pressed = " + (gamePadState.Buttons.Y == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button Back pressed = " + (gamePadState.Buttons.Back == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button Start pressed = " + (gamePadState.Buttons.Start == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button BigButton pressed = " + (gamePadState.Buttons.BigButton == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button LeftShoulder pressed = " + (gamePadState.Buttons.LeftShoulder == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button LeftStick pressed = " + (gamePadState.Buttons.LeftStick == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button RightShoulder pressed = " + (gamePadState.Buttons.RightShoulder == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Button RightStick pressed = " + (gamePadState.Buttons.RightStick == ButtonState.Pressed));
                        data.Add("--GamePad#" + playerNo + " Left Trigger Value = " + gamePadState.Triggers.Left);
                        data.Add("--GamePad#" + playerNo + " Right Trigger Value = " + gamePadState.Triggers.Right);
                    }
                }

                data.Reverse(0, data.Count);
                FreshArchives.DrawStringAdvanced(spriteBatch, debugFont, data, xPos, yPos, lineHeight, FPSColor, textAlign);
            }

            //show volume info
            if (debugShowVolumeInfo)
            {
                Point textAlign = new Point(1, 0);
                int lineHeight = fontSize;
                int xPos = gameWidth;
                int yPos = 0;

                List<string> data = new List<string>();

                if (BaseObject.sSystemRegistry.SoundSystem.MuteSoundEffects)
                    data.Add("Sound Effects Disabled");
                else
                    data.Add("Sound Effects Enabled");

                if (BaseObject.sSystemRegistry.SoundSystem.MuteMusic)
                    data.Add("Music Disabled");
                else
                    data.Add("Music Enabled");

                data.Add("Sound Effects Volume: " + BaseObject.sSystemRegistry.SoundSystem.SoundEffectsVolume);
                data.Add("Music Volume: " + BaseObject.sSystemRegistry.SoundSystem.MusicVolume);

                FreshArchives.DrawStringAdvanced(spriteBatch, debugFont, data, xPos, yPos, lineHeight, FPSColor, textAlign);
            }

            if (debugShowCameraFollowDistance)
            {
                Texture2D whitePic = FreshArchives.GetWhiteSquare(GraphicsDevice);

                float x = gameWidth / 2 - CameraSystem.X_RIGHT_FOLLOW_DISTANCE;
                float y = gameHeight / 2 - CameraSystem.Y_DOWN_FOLLOW_DISTANCE;
                float width = CameraSystem.X_LEFT_FOLLOW_DISTANCE + CameraSystem.X_RIGHT_FOLLOW_DISTANCE;
                float height = CameraSystem.Y_DOWN_FOLLOW_DISTANCE + CameraSystem.Y_UP_FOLLOW_DISTANCE;

                Rectangle rec = new Rectangle((int)x, (int)y, (int)width, (int)height);

                FreshArchives.DrawRectangle(spriteBatch, whitePic, rec, 1, Color.Blue);
            }
        }
    }
    public class PlayerController
    {
        public bool UpPressed { get { return MovementDir.Y == -1; } }
        public bool DownPressed { get { return MovementDir.Y == 1; } }
        public bool RightPressed { get { return MovementDir.X == 1; } }
        public bool LeftPressed { get { return MovementDir.X == -1; } }

        public Point MovementDir = Point.Zero;
        public bool RotLeftPressed = false;
        public bool RotRightPressed = false;

        public void Reset()
        {
            MovementDir = Point.Zero;
            RotLeftPressed = false;
            RotRightPressed = false;
        }

        public PlayerController Snapshot()
        {
            PlayerController result = new PlayerController();
            result.MovementDir = MovementDir;
            result.RotLeftPressed = RotLeftPressed;
            result.RotRightPressed = RotRightPressed;
            return result;
        }
    }
    public enum GameStates
    {
        LevelEditor,
        MainMenu,
        LoadingLevel,
        MainLoop,
        Paused
    }
}
