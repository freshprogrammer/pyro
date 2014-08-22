using Archives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pyro
{
    class PyroHudSystem : HudSystem
    {
        private StringRenderObject fuelTitle;
        private StringRenderObject fuel;
        private StringRenderObject scoreTitle;
        private StringRenderObject score;
        private StringRenderObject highScoreTitle;
        private StringRenderObject highScore;
        private StringRenderObject lastScoreTitle;
        private StringRenderObject lastScore;

        private float scale = 2f;
        
        public override void Setup()
        {
            Reset();

            int verticalSubSeperation = 32;
            int verticalSeperation = 32 * 3;
            int leftXOffset = 20;
            int leftXSubOffset = leftXOffset + 32 * 9;
            int leftYOffset = 75;

            highScoreTitle = new StringRenderObject(new KromskyFontSpriteSheet(), "High Score");
            highScoreTitle.Priority = SortConstants.HUD;
            highScoreTitle.SetPosition(leftXOffset, leftYOffset);
            highScoreTitle.SetScale(scale, scale);
            
            highScore = new StringRenderObject(new KromskyFontSpriteSheet(), "0000000");
            highScore.RightAligned = true;
            highScore.Priority = SortConstants.HUD;
            highScore.SetPosition(leftXSubOffset, leftYOffset + verticalSubSeperation);
            highScore.SetScale(scale, scale);

            lastScoreTitle = new StringRenderObject(new KromskyFontSpriteSheet(), "Last Score");
            lastScoreTitle.Priority = SortConstants.HUD;
            lastScoreTitle.SetPosition(leftXOffset, leftYOffset + verticalSeperation);
            lastScoreTitle.SetScale(scale, scale);
                
            lastScore = new StringRenderObject(new KromskyFontSpriteSheet(), "0000000");
            lastScore.RightAligned = true;
            lastScore.Priority = SortConstants.HUD;
            lastScore.SetPosition(leftXSubOffset, leftYOffset + verticalSeperation + verticalSubSeperation);
            lastScore.SetScale(scale, scale);

            scoreTitle = new StringRenderObject(new KromskyFontSpriteSheet(), "Score");
            scoreTitle.Priority = SortConstants.HUD;
            scoreTitle.SetPosition(leftXOffset, leftYOffset + 2 * verticalSeperation);
            scoreTitle.SetScale(scale, scale);

            score = new StringRenderObject(new KromskyFontSpriteSheet(), "0000000");
            score.RightAligned = true;
            score.Priority = SortConstants.HUD;
            score.SetPosition(leftXSubOffset, leftYOffset + 2 * verticalSeperation + verticalSubSeperation);
            score.SetScale(scale, scale);

            fuelTitle = new StringRenderObject(new KromskyFontSpriteSheet(), "Fuel");
            fuelTitle.Priority = SortConstants.HUD;
            fuelTitle.SetPosition(leftXOffset, leftYOffset + 3 * verticalSeperation);
            fuelTitle.SetScale(scale, scale);
            
            fuel = new StringRenderObject(new KromskyFontSpriteSheet(), "0000000");
            fuel.RightAligned = true;
            fuel.Priority = SortConstants.HUD;
            fuel.SetPosition(leftXSubOffset, leftYOffset + 3 * verticalSeperation + verticalSubSeperation);
            fuel.SetScale(scale, scale);
        }

        public override void Reset()
        {
            Enabled = true;
        }

        public override void Update(float secondsDelta, BaseObject parent)
        {
            if (Enabled)
            {
                GameObjectManager manager = sSystemRegistry.GameObjectManager;
                if (manager != null)
                {
                    //update to render
                    highScoreTitle.Update(secondsDelta, this);
                    lastScoreTitle.Update(secondsDelta, this);
                    scoreTitle.Update(secondsDelta, this);
                    fuelTitle.Update(secondsDelta, this);

                    if (PyroGameManager.AIEnabled)
                        highScore.SetText(PyroGameManager.AIHighScore.ToString());
                    else
                        highScore.SetText(PyroGameManager.HighScore.ToString());
                    highScore.Update(secondsDelta, this);

                    lastScore.SetText(PyroGameManager.LastScore.ToString());
                    lastScore.Update(secondsDelta, this);

                    score.SetText(PyroGameManager.Score.ToString());
                    score.Update(secondsDelta, this);

                    fuel.SetText(PyroGameManager.FuelCollected.ToString());
                    fuel.Update(secondsDelta, this);
                }
            }
        }

        public override void UpdateInventory(InventoryComponent.UpdateRecord inv)
        {
            //stub HUD inventory stub
        }
    }
}
