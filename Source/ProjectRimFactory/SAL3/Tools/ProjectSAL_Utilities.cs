using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.SAL3.Tools
{
    static class ProjectSAL_Utilities
    {
        public static Thing CalculateDominantIngredient(RecipeDef currentRecipe, IEnumerable<Thing> thingRecord)
        {
            var stuffs = thingRecord.Where(t => t.def.IsStuff);
            if (!thingRecord.Any())
            {
                if (currentRecipe.ingredients.Count > 0) Log.Warning("S.A.L.: Had no thingRecord of items being accepted, but crafting recipe has ingredients. Did you reload a save?");
                return ThingMaker.MakeThing(ThingDefOf.Steel);
            }
            if (stuffs.Any())
            {
                if (currentRecipe.productHasIngredientStuff)
                {
                    return stuffs.OrderByDescending(x => x.stackCount).First();
                }
                if (currentRecipe.products.Any(x => x.thingDef.MadeFromStuff))
                {
                    return stuffs.RandomElementByWeight(x => x.stackCount);
                }
            }
            return ThingMaker.MakeThing(ThingDefOf.Steel);
        }
    }
}
