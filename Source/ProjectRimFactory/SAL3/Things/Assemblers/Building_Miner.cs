using System.Collections.Generic;
using ProjectRimFactory.Common;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers;

public class Building_Miner : Building_ProgrammableAssembler
{
    public override void ExposeData()
    {
        base.ExposeData();
        billStack.Bills.RemoveAll(b => def.GetModExtension<ModExtension_Miner>()?.IsExcluded(b.recipe.ProducedThingDef) ?? false);
    }
}