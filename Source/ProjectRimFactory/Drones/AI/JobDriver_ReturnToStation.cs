using System.Collections.Generic;
using Verse.AI;

namespace ProjectRimFactory.Drones.AI
{
    public class JobDriver_ReturnToStation : JobDriver_Goto
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            foreach (Toil t in base.MakeNewToils())
                yield return t;
            yield return new Toil()
            {
                initAction = () =>
                {
                    pawn.inventory.DropAllNearPawn(pawn.Position);
                    pawn.Destroy();
                    ((Pawn_Drone)pawn).station.Notify_DroneGained();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
