using System;
using System.Collections.Generic;
using ProjectRimFactory.Common;
using RimWorld;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    [StaticConstructorOnStartup]
    public abstract class Building_DynamicBillGiver : PRF_Building, IBillGiver
    {
        public abstract BillStack BillStack { get; }

        public virtual IEnumerable<IntVec3> IngredientStackCells =>
            GetComp<CompPowerWorkSetting>()?.GetRangeCells() ?? GenAdj.CellsAdjacent8Way(this);

        public bool CurrentlyUsableForBills()
        {
            return false;
        }

        public bool UsableForBillsAfterFueling()
        {
            throw new NotImplementedException();
        }

        public abstract IEnumerable<RecipeDef> GetAllRecipes();
    }
}