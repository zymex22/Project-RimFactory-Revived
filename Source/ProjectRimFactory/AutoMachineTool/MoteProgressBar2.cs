using System;
using RimWorld;
using UnityEngine;

namespace ProjectRimFactory.AutoMachineTool
{
    public class MoteProgressBar2 : MoteProgressBar
    {
        public Func<float> progressGetter;

        public override void Draw()
        {
            if (progressGetter != null) progress = Mathf.Clamp01(progressGetter());
            base.Draw();
        }
    }
}