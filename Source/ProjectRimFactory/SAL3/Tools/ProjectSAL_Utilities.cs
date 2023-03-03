using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.SAL3.Tools
{
    static class ProjectSAL_Utilities
    {
        /// <summary>
        /// Modified Version of Verse.AI.Toils_Recipe:CalculateDominantIngredient to work without a Job
        /// </summary>
        /// <param name="RecipeDef"></param>
        /// <param name="ingredients"></param>
        /// <returns></returns>
        public static Thing CalculateDominantIngredient(RecipeDef RecipeDef, List<Thing> ingredients)
        {
            //Get Things that are Stuff
            var stuffs = ingredients.Where(t => t.def.IsStuff).ToList();

            if (!ingredients.NullOrEmpty() && stuffs.Any())
            {
                if (RecipeDef.productHasIngredientStuff)
                {
                    return stuffs[0];
                }
                if (RecipeDef.products.Any((ThingDefCountClass x) => x.thingDef.MadeFromStuff))
                {
                    return stuffs.Where((Thing x) => x.def.IsStuff).RandomElementByWeight((Thing x) => x.stackCount);
                }
                return stuffs.RandomElementByWeight((Thing x) => x.stackCount); ;
            }
            //Return steel instead of Null to prevent null ref in some cases
            return ThingMaker.MakeThing(ThingDefOf.Steel);
        }
    }
}
