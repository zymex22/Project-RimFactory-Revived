using RimWorld;
using System;
using UnityEngine;

namespace ProjectRimFactory.AutoMachineTool
{
    public class MoteProgressBar2 : MoteProgressBar
    {
        public override void Draw()
        {
            if (progressGetter != null)
            {
                this.progress = Mathf.Clamp01(this.progressGetter());
            }
            base.Draw();
        }

        public Func<float> progressGetter;
    }
}
