using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    [StaticConstructorOnStartup]
    public abstract class Building_DynamicBillGiver : PRF_Building, IBillGiver, IBillTab
    {
        public abstract BillStack BillStack { get; }
        
        // TODO: This could be much more efficient if that base is even used
        public virtual IEnumerable<IntVec3> IngredientStackCells => GetComp<CompPowerWorkSetting>()?.GetRangeCells().
            Where(c => c.InBounds(Map)) ?? GenAdj.CellsAdjacent8Way(this).Where(c => c.InBounds(Map));
        public bool CurrentlyUsableForBills() => false;

        public abstract IEnumerable<RecipeDef> GetAllRecipes();

        public void Notify_BillDeleted(Bill bill)
        {
        }

        public bool UsableForBillsAfterFueling()
        {
            throw new NotImplementedException();
        }
    }
}
