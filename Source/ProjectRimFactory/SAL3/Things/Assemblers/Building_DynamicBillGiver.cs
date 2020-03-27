using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    [StaticConstructorOnStartup]
    public abstract class Building_DynamicBillGiver : Building, IBillGiver
    {
        public abstract BillStack BillStack { get; }

        public virtual IEnumerable<IntVec3> IngredientStackCells => GenAdj.CellsAdjacent8Way(this);

        public bool CurrentlyUsableForBills() => false;

        public abstract IEnumerable<RecipeDef> GetAllRecipes();

        public bool UsableForBillsAfterFueling()
        {
            throw new NotImplementedException();
        }
    }
}
