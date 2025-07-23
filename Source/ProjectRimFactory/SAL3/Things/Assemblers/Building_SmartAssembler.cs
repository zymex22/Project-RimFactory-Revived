using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public class Building_SmartAssembler : Building_ProgrammableAssembler
    {
        public override IEnumerable<RecipeDef> GetAllRecipes()
        {
            return from IntVec3 c in this.GetComp<CompRecipeImportRange>()?.RangeCells() ?? GenAdj.CellsAdjacent8Way(this)
                   where c.InBounds(this.Map)
                   from Thing t in c.GetThingList(Map)
                   let h = t as Building_RecipeHolder
                   where h != null
                   from RecipeDef recipe in h.Recipes
                   where base.SatisfiesSkillRequirements(recipe)
                   select recipe;
        }

        public virtual void Notify_RecipeHolderRemoved()
        {
            if (GravshipPlacementUtility.placingGravship || GravshipUtility.generatingGravship) return; // Don't remove stuff on GravShip move
            var count = BillStack.Bills.Count;
            HashSet<RecipeDef> set = [..GetAllRecipes()];
            BillStack.Bills.RemoveAll(b => !set.Contains(b.recipe));
            var removed = BillStack.Bills.Count < count;
            if (CurrentBillReport != null && !set.Contains(CurrentBillReport.bill.recipe))
            {
                for (var i = 0; i < CurrentBillReport.selected.Count; i++)
                {
                    GenPlace.TryPlaceThing(CurrentBillReport.selected[i], Position, Map, ThingPlaceMode.Near);
                }
                CurrentBillReport = null;
                removed = true;
            }
            if (removed)
            {
                Messages.Message("SAL3Alert_SomeBillsRemoved".Translate(), this, MessageTypeDefOf.NegativeEvent);
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Notify_RecipeHolderRemoved();
        }
    }
}
