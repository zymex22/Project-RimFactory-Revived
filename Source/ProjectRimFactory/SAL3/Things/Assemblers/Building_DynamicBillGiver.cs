using ProjectRimFactory.Common;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    [StaticConstructorOnStartup]
    public abstract class Building_DynamicBillGiver : PRF_BuildingBill
    {
        
        // TODO: This could be much more efficient if that base is even used
        public new virtual IEnumerable<IntVec3> IngredientStackCells => GetComp<CompPowerWorkSetting>()?.GetRangeCells().
            Where(c => c.InBounds(Map)) ?? GenAdj.CellsAdjacent8Way(this).Where(c => c.InBounds(Map));
        public new bool CurrentlyUsableForBills() => false;

        public abstract IEnumerable<RecipeDef> GetAllRecipes();
        
        public new bool UsableForBillsAfterFueling()
        {
            return false;
        }
    }
}
