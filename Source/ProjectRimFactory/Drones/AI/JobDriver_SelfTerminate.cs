using System.Collections.Generic;
using Verse.AI;

namespace ProjectRimFactory.Drones.AI
{
    public class JobDriver_SelfTerminate : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return new Toil()
            {
                initAction = () =>
                {
                    pawn.Destroy();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
