using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class MoteProgressBar2 : MoteProgressBar
    {
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            if (progressGetter != null)
            {
                this.progress = Mathf.Clamp01(this.progressGetter());
            }
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }

        public Func<float> progressGetter;
    }
}
