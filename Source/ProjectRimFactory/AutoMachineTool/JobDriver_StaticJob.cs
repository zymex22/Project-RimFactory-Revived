using System;
using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace ProjectRimFactory.AutoMachineTool
{
    public class JobDriver_StaticJob : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield break;
        }
    }
}
