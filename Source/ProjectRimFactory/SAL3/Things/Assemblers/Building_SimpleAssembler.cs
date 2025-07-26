using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public class Building_SimpleAssembler : Building_ProgrammableAssembler
    {
        public override IEnumerable<RecipeDef> GetAllRecipes()
        {
            var recipes = new HashSet<RecipeDef>();
            // Imports recipes from mod extension and recipes tag
            if ((AssemblerDefModExtension?.importRecipesFrom?.Count ?? 0) > 0)
            {
                foreach (var r in AssemblerDefModExtension.importRecipesFrom.SelectMany(t => t.AllRecipes))
                {
                    if (recipes.Contains(r) || !SatisfiesSkillRequirements(r)) continue;
                    recipes.Add(r);
                    yield return r;
                }
            }
            if (def.recipes == null) yield break;
            {
                foreach (var r in def.recipes)
                {
                    yield return r;
                }
            }
        }
    }
}
