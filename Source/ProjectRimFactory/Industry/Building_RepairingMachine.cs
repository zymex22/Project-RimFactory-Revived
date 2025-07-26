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
            public override void ConsumeIngredient(Thing ingredient, RecipeDef _, Map map)
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

        private const int TicksPerHitPoint = 60;

        protected override void Notify_BillStarted()
        {
            base.Notify_BillStarted();
            var thingToRepair = CurrentBillReport.Selected.Find(t => t.def != PRFDefOf.Paperclip);
            CurrentBillReport.WorkLeft = (thingToRepair.MaxHitPoints - thingToRepair.HitPoints) * TicksPerHitPoint;
        }
    }
}
