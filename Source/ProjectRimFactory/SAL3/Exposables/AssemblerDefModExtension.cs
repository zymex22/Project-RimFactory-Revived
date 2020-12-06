using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ProjectRimFactory.SAL3.Exposables
{
    public class AssemblerDefModExtension : DefModExtension
    {
        public EffecterDef defaultEffecter;
        public SoundDef defaultSound;
        public bool doEffect = false;
        public bool drawStatus = false;
        public List<ThingDef> importRecipesFrom;
        public List<RecipeEffecter> overrideRecipeEffecter = new List<RecipeEffecter>();
        public GraphicData powerOffGraphicData;

        public GraphicData workingGraphicData;
        public float workSpeedBaseFactor = 1f;

        public Graphic WorkingGrahic
        {
            get
            {
                if (workingGraphicData != null) return workingGraphicData.Graphic;
                return null;
            }
        }

        public Graphic PowerOffGrahic
        {
            get
            {
                if (powerOffGraphicData != null) return powerOffGraphicData.Graphic;
                return null;
            }
        }

        private bool IsNothingEffectAndSound(RecipeDef recipe)
        {
            return recipe.effectWorking == null && recipe.soundWorking == null;
        }

        public EffecterDef GetEffecter(RecipeDef recipe)
        {
            if (!doEffect) return null;
            var overridden = overrideRecipeEffecter.Where(e => e.recipe == recipe).FirstOrDefault();
            return overridden == null
                ? IsNothingEffectAndSound(recipe) ? defaultEffecter : recipe.effectWorking
                : overridden.effecter;
        }

        public SoundDef GetSound(RecipeDef recipe)
        {
            if (!doEffect) return null;
            var overridden = overrideRecipeEffecter.Where(e => e.recipe == recipe).FirstOrDefault();
            return overridden == null
                ? IsNothingEffectAndSound(recipe) ? defaultSound : recipe.soundWorking
                : overridden.sound;
        }
    }

    public class RecipeEffecter
    {
        public EffecterDef effecter;
        public RecipeDef recipe;
        public SoundDef sound;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "recipe", xmlRoot.Name);
            if (xmlRoot.Attributes["Effecter"] != null)
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "effecter",
                    xmlRoot.Attributes["Effecter"].Value);
            if (xmlRoot.Attributes["Sound"] != null)
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "sound", xmlRoot.Attributes["Sound"].Value);
        }
    }
}