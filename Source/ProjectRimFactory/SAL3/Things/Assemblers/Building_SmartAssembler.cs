﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public class Building_SmartAssembler : Building_ProgrammableAssembler
    {
        public override IEnumerable<RecipeDef> GetAllRecipes()
        {
            return from IntVec3 c in GetComp<CompRecipeImportRange>()?.RangeCells() ?? GenAdj.CellsAdjacent8Way(this)
                from Thing t in c.GetThingList(Map)
                let h = t as Building_RecipeHolder
                where h != null
                from RecipeDef recipe in h.recipes
                where base.SatisfiesSkillRequirements(recipe)
                select recipe;
        }

        public virtual void Notify_RecipeHolderRemoved()
        {
            var count = BillStack.Bills.Count;
            var removed = false;
            var set = new HashSet<RecipeDef>(GetAllRecipes());
            BillStack.Bills.RemoveAll(b => !set.Contains(b.recipe));
            removed = BillStack.Bills.Count < count;
            if (currentBillReport != null && !set.Contains(currentBillReport.bill.recipe))
            {
                for (var i = 0; i < currentBillReport.selected.Count; i++)
                    GenPlace.TryPlaceThing(currentBillReport.selected[i], Position, Map, ThingPlaceMode.Near);
                currentBillReport = null;
                removed = true;
            }

            if (removed)
                Messages.Message("SAL3Alert_SomeBillsRemoved".Translate(), this, MessageTypeDefOf.NegativeEvent);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Notify_RecipeHolderRemoved();
        }
    }
}