﻿using Archives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace Pyro
{
    public class PyroGameObjectFactory : GameObjectFactory
    {
        private SoundEffect playerDeathSound;
        private SoundEffect fuelSound;

        public const float DeathAnimationDuration = 0.25f;

        public PyroGameObjectFactory()
            : base()
        {
        }

        public override void UpdateActivationRadii()
        {
            ContextParameters paramaters = sSystemRegistry.ContextParameters;
            //float halfHeight2 = (paramaters.GameHeight * 0.5f) * (paramaters.GameHeight * 0.5f);
            //float halfWidth2 = (paramaters.GameWidth * 0.5f) * (paramaters.GameWidth * 0.5f);
            //float screenSizeRadius = (float)Math.Sqrt(halfHeight2 + halfWidth2);

            //to londgside
            float screenSizeRadius;
            if (paramaters.GameWidth > paramaters.GameHeight)
                screenSizeRadius = paramaters.GameWidth / 2;
            else
                screenSizeRadius = paramaters.GameHeight / 2;

            mActivationRadiusTight = screenSizeRadius + 128.0f;
            mActivationRadiusNormal = screenSizeRadius * 1.25f;
            mActivationRadiusWide = screenSizeRadius * 2.0f;
            mActivationRadiusExtraWide = screenSizeRadius * 4.0f;
            mActivationRadius_AlwaysActive = -1.0f;
        }

        public override void PreloadEffects()
        {
            // These textures appear in every level, so they are long-term.

            ContentManager content = sSystemRegistry.Game.Content;

            playerDeathSound = content.Load<SoundEffect>(@"sounds\Sonic_Rings_Out");
            fuelSound = content.Load<SoundEffect>(@"sounds\SonicRing");
        }

        protected override void InitializeStaticData()
        {
            //instanciate static data
            int objectTypeCount = Enum.GetValues(typeof(PyroGameObjectTypes)).Length;
            mStaticData = new FixedSizeArray<FixedSizeArray<BaseObject>>(objectTypeCount);

            for (int x = 0; x < objectTypeCount; x++)
            {
                mStaticData.Add(null);
            }
        }

        protected override void InitializeComponentPools()
        {
            MaxGameObjects = 10000;
            mGameObjectPool = new GameObjectPool(MaxGameObjects);

            int renderedObjects = MaxGameObjects;//what game objects are inportant but not rendered?

            int particals = 1000;
            int collectables = 1500;
            int projectiles = 200;
            //int simplePhysicsEntities = 15;
            int players = 1;
            int enemies = 1500;
            //int wanderEnemies = 1500;
            //int patrolEnemies = 200;
            //int circularEnemies = 200;
            //int ninjas = 200;

            //int staticSets = 15;

            ComponentClass[] componentTypes = {
                    //new ComponentClass(AnimationComponent.class, 384),
                    //new ComponentClass(AttackAtDistanceComponent.class, 16),
                    //new ComponentClass(enemies+players+particals+projectiles, typeof(BackgroundCollisionComponent)),
                    //new ComponentClass(ButtonAnimationComponent.class, 32),
                    //new ComponentClass(CameraBiasComponent.class, 8),
                    //new ComponentClass(circularEnemies, typeof(CircularAIComponent)),
                    //new ComponentClass(ChangeComponentsComponent.class, 256),
                    //new ComponentClass(DoorAnimationComponent.class, 256),  //!
                    //new ComponentClass(enemies+players+projectiles, typeof(DynamicCollisionComponent)),
                    //new ComponentClass(EnemyAnimationComponent.class, 256),
                    //new ComponentClass(FadeDrawableComponent.class, 32),
                    //new ComponentClass(FixedAnimationComponent.class, 8),
                    //new ComponentClass(FrameRateWatcherComponent.class, 1),
                    //new ComponentClass(GenericAnimationComponent.class, 32),
                    //new ComponentClass(staticSets, typeof(GravityComponent)),
                    //new ComponentClass(collectables+enemies, typeof(HitPlayerComponent)),
                    //new ComponentClass(collectables+enemies+players, typeof(HitReactionComponent)),
                    //new ComponentClass(players, typeof(InventoryComponent)),
                    //new ComponentClass(players+ninjas, typeof(LaunchProjectileComponent)),
                    new ComponentClass(enemies+players+particals+collectables+projectiles, typeof(LifetimeComponent)),
                    //new ComponentClass(staticSets, typeof(MovementComponent)),
                    //new ComponentClass(NPCAnimationComponent.class, 8),
                    //new ComponentClass(NPCComponent.class, 8),
                    //new ComponentClass(OrbitalMagnetComponent.class, 1),
                    //new ComponentClass(patrolEnemies,typeof(PatrolAIComponent)),
                    //new ComponentClass(staticSets, typeof(PhysicsComponent)),
                    //new ComponentClass(players, typeof(PlayerComponent)),
                    //new ComponentClass(wanderEnemies,typeof(PursuitAIComponent)),
                    new ComponentClass(renderedObjects, typeof(RenderComponent)),
                    //new ComponentClass(SimpleCollisionComponent.class, 32),
                    //new ComponentClass(simplePhysicsEntities, typeof(SimplePhysicsComponent)),
                    //new ComponentClass(enemies, typeof(SolidSurfaceComponent)),
                    new ComponentClass(collectables+enemies+players, typeof(SpriteComponent)),
                    //new ComponentClass(wanderEnemies,typeof(WanderAIComponent)),
            };

            mComponentPools = new FixedSizeArray<FreshGameComponentPool>(componentTypes.Length, sComponentPoolComparator);
            for (int x = 0; x < componentTypes.Length; x++)
            {
                ComponentClass component = componentTypes[x];
                mComponentPools.Add(new FreshGameComponentPool(component.type, component.poolSize));
            }
            mComponentPools.Sort(true);

            mPoolSearchDummy = new FreshGameComponentPool(typeof(object), 1);
        }

        public void LoadSettings(VariableLibrary vars)
        {
            //collectables
            //vars.GetVariable("scorePerRing", ref scorePerRing, true);
        }

        public override GameObject Spawn(int objectType, float x, float y, Vector2 facingDir, int priority)
        {
            if(objectType ==(int)PyroGameObjectTypes.PlayerDead)
                return SpawnPlayerDead(x,y,facingDir);
            return null;
        }

        public GameObject SpawnBackgroundPlate(float positionX, float positionY)
        {
            const int width = 1280;
            const int height = 720;
            int type = (int)PyroGameObjectTypes.Background_Plate;
            GameObject result = mGameObjectPool.Allocate();
            result.SetPosition(positionX, positionY);
            result.ActivationRadius = mActivationRadiusExtraWide;
            result.width = width;
            result.height = height;

            ContentManager content = sSystemRegistry.Game.Content;

            FixedSizeArray<BaseObject> staticData = GetStaticData(type);
            if (staticData == null)
            {
                const int staticObjectCount = 1;
                staticData = new FixedSizeArray<BaseObject>(staticObjectCount);

                //InventoryRecord addWin = new InventoryRecord();
                //addWin.winCount = 1;

                //staticData.Add(addWin);

                SetStaticData(type, staticData);
            }

            Rectangle crop = new Rectangle(0, 0, width, height);
            DrawableTexture2D textureDrawable = new DrawableTexture2D(content.Load<Texture2D>(@"pics\background"), (int)result.width, (int)result.height);
            textureDrawable.SetCrop(crop);

            RenderComponent render = (RenderComponent)AllocateComponent(typeof(RenderComponent));
            render.Priority = PyroSortConstants.BACKGROUND;
            render.setDrawable(textureDrawable);


            result.Add(render);

            //AddStaticData(type, result, null);

            return result;
        }

        public GameObject SpawnTileEmpty(float positionX, float positionY)
        {
            int type = (int)PyroGameObjectTypes.Tile_Blank;

            GameObject result = mGameObjectPool.Allocate();
            result.SetPosition(positionX, positionY);
            result.ActivationRadius = mActivationRadiusTight;
            result.width = 32;
            result.height = 32;
            result.PositionLocked = true;
            result.DestroyOnDeactivation = false;


            FixedSizeArray<BaseObject> staticData = GetStaticData(type);
            if (staticData == null)
            {
                ContentManager content = sSystemRegistry.Game.Content;
                GraphicsDevice device = sSystemRegistry.Game.GraphicsDevice;

                int staticObjectCount = 1;
                staticData = new FixedSizeArray<BaseObject>(staticObjectCount);

                const int fileImageSize = 64;
                Rectangle crop = new Rectangle(0, 0, fileImageSize, fileImageSize);
                Texture2D texture = content.Load<Texture2D>(@"pics\tile_empty");
                
                
                DrawableTexture2D textureDrawable = new DrawableTexture2D(texture, (int)result.width, (int)result.height);
                textureDrawable.SetCrop(crop);

                RenderComponent render = (RenderComponent)AllocateComponent(typeof(RenderComponent));
                render.Priority = PyroSortConstants.TILES;
                render.setDrawable(textureDrawable);

                staticData.Add(render);
                SetStaticData(type, staticData);
            }

            AddStaticData(type, result, null);

            return result;
        }

        public GameObject SpawnPlayer(float positionX, float positionY)
        {
            int thisGameObjectType = (int)PyroGameObjectTypes.Player;
            GameObject result = mGameObjectPool.Allocate();
            result.SetPosition(positionX, positionY);
            result.ActivationRadius = mActivationRadius_AlwaysActive;
            result.width = 32;
            result.height = 32;

            result.life = 1;
            result.team = GameObject.Team.PLAYER;


            FixedSizeArray<BaseObject> staticData = GetStaticData(thisGameObjectType);

            if (staticData == null)
            {
                ContentManager content = sSystemRegistry.Game.Content;
                int staticObjectCount = 1;
                staticData = new FixedSizeArray<BaseObject>(staticObjectCount);

                // Animation Data
                float animationDelay = 0.16f;
                //Idle
                SpriteAnimation idle = new SpriteAnimation((int)Animations.Idle, 3);
                idle.Loop = true;
                idle.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\player\001_attackNN_01"), animationDelay));
                idle.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\player\001_attackNN_02"), animationDelay, new Rectangle(0,0,32,32)));
                idle.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\player\001_attackNN_03"), animationDelay));

                //animations
                staticData.Add(idle);

                SetStaticData(thisGameObjectType, staticData);
            }

            RenderComponent render = (RenderComponent)AllocateComponent(typeof(RenderComponent));
            render.Priority = PyroSortConstants.PLAYER;
            render.CameraRelative = true;

            SpriteComponent sprite = (SpriteComponent)AllocateComponent(typeof(SpriteComponent));
            sprite.SetSize((int)result.width, (int)result.height);
            sprite.SetRenderComponent(render);
            sprite.SetRenderMode(SpriteComponent.RenderMode.RotateToFacingDirection);

            LifetimeComponent lifetime = AllocateComponent<LifetimeComponent>();
            lifetime.SetDeathSound(playerDeathSound);
            lifetime.SetObjectToSpawnOnDeath((int)PyroGameObjectTypes.PlayerDead, false);

            result.Add(render);
            result.Add(lifetime);
            result.Add(sprite);

            AddStaticData(thisGameObjectType, result, sprite);

            sprite.PlayAnimation((int)Animations.Idle);

            return result;
        }

        public GameObject SpawnPlayerDead(float positionX, float positionY, Vector2 facingDir)
        {
            int thisGameObjectType = (int)PyroGameObjectTypes.PlayerDead;
            GameObject result = mGameObjectPool.Allocate();
            result.SetPosition(positionX, positionY);
            result.ActivationRadius = mActivationRadius_AlwaysActive;
            result.width = 32;
            result.height = 32;
            result.facingDirection = facingDir;

            result.life = 1;
            result.team = GameObject.Team.NONE;


            FixedSizeArray<BaseObject> staticData = GetStaticData(thisGameObjectType);

            if (staticData == null)
            {
                ContentManager content = sSystemRegistry.Game.Content;
                int staticObjectCount = 1;
                staticData = new FixedSizeArray<BaseObject>(staticObjectCount);

                // Animation Data
                float animationDelay = 0.16f;
                Rectangle crop32 = new Rectangle(0, 0, 32, 32);
                //Idle
                SpriteAnimation idle = new SpriteAnimation((int)Animations.Idle, 2);
                idle.Loop = true;
                idle.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\player\001_Death"), animationDelay, crop32));
                idle.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\player\001_Death"), animationDelay, crop32, SpriteEffects.FlipHorizontally));

                //animations
                staticData.Add(idle);

                SetStaticData(thisGameObjectType, staticData);
            }

            RenderComponent render = (RenderComponent)AllocateComponent(typeof(RenderComponent));
            render.Priority = PyroSortConstants.PLAYER;
            render.CameraRelative = true;

            SpriteComponent sprite = (SpriteComponent)AllocateComponent(typeof(SpriteComponent));
            sprite.SetSize((int)result.width, (int)result.height);
            sprite.SetRenderComponent(render);
            sprite.SetRenderMode(SpriteComponent.RenderMode.RotateToFacingDirection);

            LifetimeComponent lifetime = AllocateComponent<LifetimeComponent>();
            
            result.Add(render);
            result.Add(lifetime);
            result.Add(sprite);

            AddStaticData(thisGameObjectType, result, sprite);

            sprite.PlayAnimation((int)Animations.Idle);

            return result;
        }

        public GameObject SpawnFuel(float positionX, float positionY)
        {
            int type = (int)PyroGameObjectTypes.Fuel;

            GameObject result = mGameObjectPool.Allocate();
            result.SetPosition(positionX, positionY);
            result.ActivationRadius = mActivationRadiusTight;
            result.width = 32;
            result.height = 32;
            result.PositionLocked = true;
            result.DestroyOnDeactivation = false;

            result.life = 1;
            result.team = GameObject.Team.NONE;

            FixedSizeArray<BaseObject> staticData = GetStaticData(type);
            if (staticData == null)
            {
                ContentManager content = sSystemRegistry.Game.Content;
                GraphicsDevice device = sSystemRegistry.Game.GraphicsDevice;

                int staticObjectCount = 1;
                staticData = new FixedSizeArray<BaseObject>(staticObjectCount);

                const int fileImageSize = 64;
                Rectangle crop = new Rectangle(0, 0, fileImageSize, fileImageSize);
                Texture2D texture = content.Load<Texture2D>(@"pics\fuel");


                DrawableTexture2D textureDrawable = new DrawableTexture2D(texture, (int)result.width, (int)result.height);
                textureDrawable.SetCrop(crop);

                RenderComponent render = (RenderComponent)AllocateComponent(typeof(RenderComponent));
                render.Priority = PyroSortConstants.FUEL;
                render.setDrawable(textureDrawable);

                staticData.Add(render);
                SetStaticData(type, staticData);
            }

            LifetimeComponent lifetime = AllocateComponent<LifetimeComponent>();
            lifetime.SetDeathSound(fuelSound);

            result.Add(lifetime);

            AddStaticData(type, result, null);

            return result;
        }

        public GameObject SpawnFire(float positionX, float positionY, int life)
        {
            int thisGameObjectType = (int)PyroGameObjectTypes.Fire;
            GameObject result = mGameObjectPool.Allocate();
            result.SetPosition(positionX, positionY);
            result.ActivationRadius = mActivationRadius_AlwaysActive;
            result.width = 32;
            result.height = 32;

            result.life = life;
            result.team = GameObject.Team.NONE;


            FixedSizeArray<BaseObject> staticData = GetStaticData(thisGameObjectType);

            if (staticData == null)
            {
                ContentManager content = sSystemRegistry.Game.Content;
                int staticObjectCount = 10;
                staticData = new FixedSizeArray<BaseObject>(staticObjectCount);

                // Animation Data
                float animationDelay = 0.16f;
                const int fileImageSize = 64;
                Rectangle crop = new Rectangle(0, 0, fileImageSize, fileImageSize);

                SpriteAnimation fire100 = new SpriteAnimation((int)FireAnimation.Fire100, 2);
                fire100.Loop = true;
                fire100.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire1"), animationDelay, crop));
                fire100.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire1"), animationDelay, crop, SpriteEffects.FlipHorizontally));

                SpriteAnimation fire80 = new SpriteAnimation((int)FireAnimation.Fire80, 2);
                fire80.Loop = true;
                fire80.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire2"), animationDelay, crop));
                fire80.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire2"), animationDelay, crop, SpriteEffects.FlipHorizontally));

                SpriteAnimation fire60 = new SpriteAnimation((int)FireAnimation.Fire60, 2);
                fire60.Loop = true;
                fire60.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire3"), animationDelay, crop));
                fire60.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire3"), animationDelay, crop, SpriteEffects.FlipHorizontally));

                SpriteAnimation fire40 = new SpriteAnimation((int)FireAnimation.Fire40, 2);
                fire40.Loop = true;
                fire40.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire4"), animationDelay, crop));
                fire40.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire4"), animationDelay, crop, SpriteEffects.FlipHorizontally));

                SpriteAnimation fire20 = new SpriteAnimation((int)FireAnimation.Fire20, 2);
                fire20.Loop = true;
                fire20.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire5"), animationDelay, crop));
                fire20.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire5"), animationDelay, crop, SpriteEffects.FlipHorizontally));

                SpriteAnimation fire0 = new SpriteAnimation((int)FireAnimation.Fire0, 2);
                fire0.Loop = true;
                fire0.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire6"), animationDelay, crop));
                fire0.AddFrame(new AnimationFrame(content.Load<Texture2D>(@"pics\fire6"), animationDelay, crop, SpriteEffects.FlipHorizontally));

                //animations
                staticData.Add(fire100);
                staticData.Add(fire80);
                staticData.Add(fire60);
                staticData.Add(fire40);
                staticData.Add(fire20);
                staticData.Add(fire0);

                SetStaticData(thisGameObjectType, staticData);
            }

            RenderComponent render = (RenderComponent)AllocateComponent(typeof(RenderComponent));
            render.Priority = PyroSortConstants.FIRE;
            render.CameraRelative = true;

            SpriteComponent sprite = (SpriteComponent)AllocateComponent(typeof(SpriteComponent));
            sprite.SetSize((int)result.width, (int)result.height);
            sprite.SetRenderComponent(render);
            sprite.SetRenderMode(SpriteComponent.RenderMode.RotateToFacingDirection);

            LifetimeComponent lifetime = AllocateComponent<LifetimeComponent>();

            result.Add(render);
            result.Add(lifetime);
            result.Add(sprite);

            AddStaticData(thisGameObjectType, result, sprite);

            sprite.PlayAnimation((int)FireAnimation.Fire100);

            return result;
        }

        public SolidSurfaceComponent GenerateRectangleSolidSurfaceComponent(float width, float height, bool container)
        {
            SolidSurfaceComponent solidSurface = AllocateComponent<SolidSurfaceComponent>();
            solidSurface.Inititalize(4);

            int normalFlip;
            if (container) normalFlip = 1;
            else normalFlip = -1;

            // box shape:
            // ___       ___1
            // | |      2| |3
            // ---       ---4
            Vector2 surface1Start = new Vector2(0, height);
            Vector2 surface1End = new Vector2(width, height);
            Vector2 surface1Normal = new Vector2(0.0f, normalFlip * -1.0f);

            Vector2 surface2Start = new Vector2(0, height);
            Vector2 surface2End = new Vector2(0, 0);
            Vector2 surface2Normal = new Vector2(normalFlip * 1.0f, 0.0f);

            Vector2 surface3Start = new Vector2(width, height);
            Vector2 surface3End = new Vector2(width, 0);
            Vector2 surface3Normal = new Vector2(normalFlip * -1.0f, 0);

            Vector2 surface4Start = new Vector2(0, 0);
            Vector2 surface4End = new Vector2(width, 0);
            Vector2 surface4Normal = new Vector2(0, normalFlip * 1.0f);

            solidSurface.AddSurface(surface1Start, surface1End, surface1Normal);
            solidSurface.AddSurface(surface2Start, surface2End, surface2Normal);
            solidSurface.AddSurface(surface3Start, surface3End, surface3Normal);
            solidSurface.AddSurface(surface4Start, surface4End, surface4Normal);

            return solidSurface;
        }
    }
    public class PyroSortConstants
    {
        public const int BACKGROUND = -100;
        public const int TILES = -5;
        public const int PLAYER = 20;
        public const int FIRE = 30;
        public const int FUEL = 31;
        public const int HUD = 100;
    }
    public enum PyroGameObjectTypes
    {
        //these first values must match those in the generic GameObjectTypes
        Invalid = -1,
        Tile_Blank = 0,
        Tile_Blocked = 1,
        Player = 2,

        PlayerDead,
        Background_Plate,
        Fire,
        Fuel,
    }
    public enum Animations
    {
        Idle,
        Move,
        Death,
    }
    public enum FireAnimation
    {
        Fire100,
        Fire80,
        Fire60,
        Fire40,
        Fire20,
        Fire0,
    }
}