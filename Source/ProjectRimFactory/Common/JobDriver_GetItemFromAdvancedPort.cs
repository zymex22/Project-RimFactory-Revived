using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using ProjectRimFactory.Storage;

namespace ProjectRimFactory.Common
{
    public class JobDriver_GetItemFromAdvancedPort : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            LocalTargetInfo lookAtTarget = job.GetTarget(TargetIndex.B);
            Toil toil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
            toil.FailOn(() => job.GetTarget(TargetIndex.A).Thing?.Destroyed ?? false);
            if (lookAtTarget.IsValid)
            {
                toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
                {
                    pawn.rotationTracker.FaceCell(lookAtTarget.Cell);
                });
                toil.handlingFacing = true;
            }
            toil.AddFinishAction(delegate
            {
                //Give the Items to the Pawn
                // pawn.inventory
                Log.Message("Made it to the port");
                var port = (Building_AdvancedStorageUnitIOPort)job.GetTarget(TargetIndex.A).Thing;
                port.AddItemToQueue(lookAtTarget.Thing);
                port.updateQueue();


            });
            yield return toil;
            //yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: false);
        }
    }
}
