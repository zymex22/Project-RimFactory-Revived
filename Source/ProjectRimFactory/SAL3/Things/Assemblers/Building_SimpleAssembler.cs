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
            // Imports recipes from modextension and recipes tag
            AssemblerDefModExtension extension = def.GetModExtension<AssemblerDefModExtension>();
            if (extension?.importRecipesFrom != null)
            {
                foreach (RecipeDef r in extension.importRecipesFrom.AllRecipes)
                {
                    yield return r;
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
