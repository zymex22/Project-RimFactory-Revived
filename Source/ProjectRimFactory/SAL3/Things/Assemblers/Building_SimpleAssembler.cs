using ProjectRimFactory.SAL3.Exposables;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public class Building_SimpleAssembler : Building_ProgrammableAssembler
    {
        public override IEnumerable<RecipeDef> GetAllRecipes()
        {
            HashSet<RecipeDef> recipes = new HashSet<RecipeDef>();
            // Imports recipes from modextension and recipes tag
            AssemblerDefModExtension extension = def.GetModExtension<AssemblerDefModExtension>();
            if ((extension?.importRecipesFrom?.Count ?? 0) > 0)
            {
                foreach (RecipeDef r in extension.importRecipesFrom.SelectMany(t => t.AllRecipes))
                {
                    if (!recipes.Contains(r) && (r.skillRequirements == null || r.skillRequirements.All(s => s.minLevel <= this.SkillLevel)))
                    {
                        recipes.Add(r);
                        yield return r;
                    }
                }
            }
            if (def.recipes != null)
            {
                foreach (RecipeDef r in def.recipes)
                {
                    yield return r;
                }
            }
        }
    }
}
