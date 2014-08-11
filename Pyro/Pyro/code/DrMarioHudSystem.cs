using Archives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snake
{
    class DrMarioHudSystem : HudSystem
    {
        private StringRenderObject levelTitle;
        private StringRenderObject level;
        private StringRenderObject speedTitle;
        private StringRenderObject speed;
        private StringRenderObject virusTitle;
        private StringRenderObject virus;
        private StringRenderObject scoreTitle;
        private StringRenderObject score;
        private StringRenderObject highScoreTitle;
        private StringRenderObject highScore;

        private float scale = 2f;

        public override void Setup()
        {
            Reset();
            
            int rightXOffset = 950;
            int rightXSubOffset = rightXOffset + 32 * 9;
            int rightYOffset = 400;
            int verticalSubSeperation = 32;
            int verticalSeperation = 32*3;

            levelTitle = new StringRenderObject(new KromskyFontSpriteSheet(), "Level");
            levelTitle.Priority = SortConstants.HUD;
            levelTitle.SetPosition(rightXOffset, rightYOffset);
            levelTitle.SetScale(scale, scale);

            speedTitle = new StringRenderObject(new KromskyFontSpriteSheet(), "Speed");
            speedTitle.Priority = SortConstants.HUD;
            speedTitle.SetPosition(rightXOffset, rightYOffset + verticalSeperation);
            speedTitle.SetScale(scale, scale);

            virusTitle = new StringRenderObject(new KromskyFontSpriteSheet(), "Virus");
            virusTitle.Priority = SortConstants.HUD;
            virusTitle.SetPosition(rightXOffset, rightYOffset + verticalSeperation * 2);
            virusTitle.SetScale(scale, scale);

            level = new StringRenderObject(new KromskyFontSpriteSheet(), "0000");
            level.RightAligned = true;
            level.Priority = SortConstants.HUD;
            level.SetPosition(rightXSubOffset, rightYOffset + verticalSubSeperation);
            level.SetScale(scale, scale);

            speed = new StringRenderObject(new KromskyFontSpriteSheet(), "Only");
            speed.RightAligned = true;
            speed.Priority = SortConstants.HUD;
            speed.SetPosition(rightXSubOffset, rightYOffset + verticalSeperation + verticalSubSeperation);
            speed.SetScale(scale, scale);

            virus = new StringRenderObject(new KromskyFontSpriteSheet(), "00");
            virus.RightAligned = true;
            virus.Priority = SortConstants.HUD;
            virus.SetPosition(rightXSubOffset, rightYOffset + verticalSeperation * 2 + verticalSubSeperation);
            virus.SetScale(scale, scale);



            int leftXOffset = 20;
            int leftXSubOffset = leftXOffset + 32 * 9;
            int leftYOffset = 75;

            highScoreTitle = new StringRenderObject(new KromskyFontSpriteSheet(), "Top");
            highScoreTitle.Priority = SortConstants.HUD;
            highScoreTitle.SetPosition(leftXOffset, leftYOffset);
            highScoreTitle.SetScale(scale, scale);
            
            highScore = new StringRenderObject(new KromskyFontSpriteSheet(), "0000000");
            highScore.RightAligned = true;
            highScore.Priority = SortConstants.HUD;
            highScore.SetPosition(leftXSubOffset, leftYOffset + verticalSubSeperation);
            highScore.SetScale(scale, scale);

            scoreTitle = new StringRenderObject(new KromskyFontSpriteSheet(), "Score");
            scoreTitle.Priority = SortConstants.HUD;
            scoreTitle.SetPosition(leftXOffset, leftYOffset + verticalSeperation);
            scoreTitle.SetScale(scale, scale);

            score = new StringRenderObject(new KromskyFontSpriteSheet(), "0000000");
            score.RightAligned = true;
            score.Priority = SortConstants.HUD;
            score.SetPosition(leftXSubOffset, leftYOffset + verticalSeperation + verticalSubSeperation);
            score.SetScale(scale, scale);
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
                    levelTitle.Update(secondsDelta, this);
                    virusTitle.Update(secondsDelta, this);
                    speedTitle.Update(secondsDelta, this);
                    scoreTitle.Update(secondsDelta, this);
                    highScoreTitle.Update(secondsDelta, this);

                    score.SetText(DrMarioGameManager.Score.ToString());
                    score.Update(secondsDelta, this);

                    highScore.SetText(DrMarioGameManager.HighScore.ToString());
                    highScore.Update(secondsDelta, this);

                    level.SetText(DrMarioGameManager.LevelNo.ToString());
                    level.Update(secondsDelta, this);

                    speed.SetText(DrMarioGameManager.GetSpeedName(DrMarioGameManager.Speed));
                    speed.Update(secondsDelta, this);

                    virus.SetText(DrMarioGameManager.RemainingViruses.ToString());
                    virus.Update(secondsDelta, this);
                }
            }
        }

        public override void UpdateInventory(InventoryComponent.UpdateRecord inv)
        {
            //TODO write this stub HUD inventory stub
        }
    }
}
