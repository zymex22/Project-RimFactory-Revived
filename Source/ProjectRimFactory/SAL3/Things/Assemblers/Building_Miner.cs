using System.Collections.Generic;
using ProjectRimFactory.Common;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers;

public class Building_Miner : Building_ProgrammableAssembler
{
    public override IEnumerable<RecipeDef> GetAllRecipes()
    {
        if (def.recipes == null) yield break;
        {
            foreach (var r in def.recipes)
            {
                yield return r;
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        billStack.Bills.RemoveAll(b => def.GetModExtension<ModExtension_Miner>()?.IsExcluded(b.recipe.ProducedThingDef) ?? false);
    }
}