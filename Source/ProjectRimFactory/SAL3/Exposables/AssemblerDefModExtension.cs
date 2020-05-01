using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Verse;

namespace ProjectRimFactory.SAL3.Exposables
{
    public class AssemblerDefModExtension : DefModExtension
    {
        public float workSpeedBaseFactor = 1f;
        public List<ThingDef> importRecipesFrom;
        public int skillLevel = 20;
        public bool drawStatus = false;
        public bool doEffect = false;
        public List<RecipeEffecter> overrideRecipeEffecter = new List<RecipeEffecter>();
        public EffecterDef defaultEffecter;
        public SoundDef defaultSound;

        public GraphicData workingGraphicData;

        public Graphic WorkingGrahic
        {
            get
            {
                if (workingGraphicData != null)
                {
                    return workingGraphicData.Graphic;
                }
                return null;
            }
        }

        private bool IsNothingEffectAndSound(RecipeDef recipe)
        {
            return recipe.effectWorking == null && recipe.soundWorking == null;
        }

        public EffecterDef GetEffecter(RecipeDef recipe)
        {
            if (!this.doEffect)
            {
                return null;
            }
            var overridden = this.overrideRecipeEffecter.Where(e => e.recipe == recipe).FirstOrDefault();
            return overridden == null ? (IsNothingEffectAndSound(recipe) ? defaultEffecter : recipe.effectWorking) : overridden.effecter;
        }

        public SoundDef GetSound(RecipeDef recipe)
        {
            if (!this.doEffect)
            {
                return null;
            }
            var overridden = this.overrideRecipeEffecter.Where(e => e.recipe == recipe).FirstOrDefault();
            return overridden == null ? (IsNothingEffectAndSound(recipe) ? defaultSound : recipe.soundWorking) : overridden.sound;
        }
    }

    public class RecipeEffecter
    {
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "recipe", xmlRoot.Name, null, null);
            if (xmlRoot.Attributes["Effecter"] != null)
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "effecter", xmlRoot.Attributes["Effecter"].Value, null, null);
            }
            if (xmlRoot.Attributes["Sound"] != null)
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "sound", xmlRoot.Attributes["Sound"].Value, null, null);
            }
        }
        public RecipeDef recipe;
        public EffecterDef effecter;
        public SoundDef sound;
    }
}
