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
            if (phase != DrawPhase.Draw) return; //Crashes when drawing 2 things at the same time in some of the other phases
            if (progressGetter != null)
            {
                progress = Mathf.Clamp01(progressGetter());
            }
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }

        public Func<float> progressGetter;
    }
}
