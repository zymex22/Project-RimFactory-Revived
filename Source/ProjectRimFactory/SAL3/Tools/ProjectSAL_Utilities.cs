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
            if (!ingredients.NullOrEmpty())
            {
                if (RecipeDef.productHasIngredientStuff)
                {
                    return ingredients[0];
                }
                if (RecipeDef.products.Any((ThingDefCountClass x) => x.thingDef.MadeFromStuff))
                {
                    return ingredients.Where((Thing x) => x.def.IsStuff).RandomElementByWeight((Thing x) => x.stackCount);
                }
                return ingredients.RandomElementByWeight((Thing x) => x.stackCount);
            }
            return null;
        }
    }
}
