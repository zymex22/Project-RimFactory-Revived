using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.SAL3.Tools
{
    internal static class ProjectSal_Utilities
    {
        /// <summary>
        /// Modified Version of Verse.AI.Toils_Recipe:CalculateDominantIngredient to work without a Job
        /// </summary>
        /// <param name="recipeDef"></param>
        /// <param name="ingredients"></param>
        /// <returns></returns>
        public static Thing CalculateDominantIngredient(RecipeDef recipeDef, List<Thing> ingredients)
        {
            //Get Things that are Stuff
            var stuffs = ingredients.Where(thing => thing.def.IsStuff).ToList();

            if (ingredients.NullOrEmpty() || !stuffs.Any()) return ThingMaker.MakeThing(ThingDefOf.Steel);
            
            if (recipeDef.productHasIngredientStuff)
            {
                return stuffs[0];
            }
            if (recipeDef.products.Any(thingDefCountClass => thingDefCountClass.thingDef.MadeFromStuff))
            {
                return stuffs.Where(thing => thing.def.IsStuff).RandomElementByWeight(thing => thing.stackCount);
            }
            return stuffs.RandomElementByWeight(thing => thing.stackCount);
            //Return steel instead of Null to prevent null ref in some cases
        }
    }
}
