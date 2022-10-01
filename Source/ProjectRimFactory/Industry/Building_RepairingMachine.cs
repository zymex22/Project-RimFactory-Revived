using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3.Things.Assemblers;
using Verse;

namespace ProjectRimFactory.Industry
{
    // Unfinished.
    public class Building_RepairingMachine : Building_SimpleAssembler
    {
        public class Recipe_Repair : RecipeWorker
        {
            public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
            {
                if (ingredient.def == PRFDefOf.Paperclip)
                {
                    ingredient.Destroy();
                }
                else
                {
                    ingredient.HitPoints = ingredient.MaxHitPoints;
                }
            }
        }

        public const int TicksPerHitPoint = 60;

        protected override void Notify_BillStarted()
        {
            base.Notify_BillStarted();
            Thing thingToRepair = currentBillReport.selected.Find(t => t.def != PRFDefOf.Paperclip);
            currentBillReport.workLeft = (thingToRepair.MaxHitPoints - thingToRepair.HitPoints) * TicksPerHitPoint;
        }
    }
}
