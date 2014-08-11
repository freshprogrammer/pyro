using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archives;

namespace Pyro
{
    class PyroLevel : Level
    {
        //would be stored as a list of object types (int type ID) at a location - maybe be addition info like resource count or team ID
        //List<Ro

        public override void LoadFromFile()
        {
            //scrap load from file
            //load fixed level - normaly these would be loaded from a XML like list file

            //create blank level
            //add player at 0,128
            //add base recharge station at 0,0
            //    size 128,128
            //add res at 200,200
            //    size 64,64
            //    resCount 100
            //Add Rock at 500,0
            //    size 64,128

            width = 360;
            height = 675;
            
        }

        public override void BuildEmpty()
        {
            width = 360;
            height = 675;
        }

        public override void SetupCollision()
        {
            //dynamic collision - pass blank colision map
            const int tileSize = 0;
            sSystemRegistry.CollisionSystem.Initialize(new TiledCollisionWorld(8,15), tileSize, tileSize);
        }

        public override void SpawnObjects()
        {
            PyroGameObjectFactory factory = (PyroGameObjectFactory)sSystemRegistry.GameObjectFactory;
            GameObjectManager manager = sSystemRegistry.GameObjectManager;

            manager.Add(factory.SpawnBackgroundPlate(0, 0));
        }
    }
}
