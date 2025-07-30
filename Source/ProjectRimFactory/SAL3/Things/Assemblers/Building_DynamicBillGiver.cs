using ProjectRimFactory.Common;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.SAL3.Tools;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public abstract class Building_DynamicBillGiver : PRF_BuildingBill
    {
        
        // TODO: This could be much more efficient if that base is even used
        public new virtual IEnumerable<IntVec3> IngredientStackCells => GetComp<CompPowerWorkSetting>()?.GetRangeCells().
            Where(c => c.InBounds(Map)) ?? GenAdj.CellsAdjacent8Way(this).Where(c => c.InBounds(Map));

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            def.recipes ??= [];
            def.recipes.AddRange(GetImportedRecipes());
            ReflectionUtility.AllRecipesCached.SetValue(def, null);
        }

        public new bool CurrentlyUsableForBills() => false;
        
        protected virtual IEnumerable<RecipeDef> GetImportedRecipes()
        {
            return [];
        }
        
        public new bool UsableForBillsAfterFueling()
        {
            return false;
        }
    }
}
