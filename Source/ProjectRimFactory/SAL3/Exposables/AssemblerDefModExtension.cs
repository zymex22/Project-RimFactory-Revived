using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ProjectRimFactory.SAL3.Exposables
{
    public class AssemblerDefModExtension : DefModExtension
    {
        public float workSpeedBaseFactor = 1f;
        public List<ThingDef> importRecipesFrom;
        public bool drawStatus = false;
        public bool doEffect = false;
        public List<RecipeEffecter> overrideRecipeEffecter = [];
        public EffecterDef defaultEffecter;
        public SoundDef defaultSound;

        public GraphicData workingGraphicData;
        public GraphicData powerOffGraphicData;

        public Graphic WorkingGrahic => workingGraphicData?.Graphic;

        public Graphic PowerOffGrahic => powerOffGraphicData?.Graphic;

        private bool IsNothingEffectAndSound(RecipeDef recipe)
        {
            return recipe.effectWorking == null && recipe.soundWorking == null;
        }

        public EffecterDef GetEffecter(RecipeDef recipe)
        {
            if (!doEffect)
            {
                return null;
            }
            var overridden = overrideRecipeEffecter.FirstOrDefault(e => e.recipe == recipe);
            return overridden == null ? (IsNothingEffectAndSound(recipe) ? defaultEffecter : recipe.effectWorking) : overridden.effecter;
        }

        public SoundDef GetSound(RecipeDef recipe)
        {
            if (!doEffect)
            {
                return null;
            }
            var overridden = overrideRecipeEffecter.FirstOrDefault(e => e.recipe == recipe);
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
