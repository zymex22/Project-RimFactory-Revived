using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using System.Diagnostics.Eventing.Reader;
using HarmonyLib;
using System.IO;

namespace ProjectRimFactory.Common
{
    public class CompGlowerPulse : CompGlower
    {
        public new CompProperties_GlowerPulse Props => (CompProperties_GlowerPulse)this.props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.glows, "glows", true);
            this.Props.Glows = this.glows;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.Props.Glows = this.glows;

            this.tickerThing = NormalTickerThing.Spawn(this.parent);
            this.tickerThing.tickAction = this.TickerTick;
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            tickerThing.Destroy();
        }

        private NormalTickerThing tickerThing;

        private void TickerTick()
        {
            if (this.needUpdate || this.Props.pulse)
            {
                this.Props.Update();
                this.parent.Map.glowGrid.MarkGlowGridDirty(this.parent.Position);
                this.needUpdate = false;
            }
        }

        private bool glows = true;

        public new bool Glows
        {
            get
            {
                return this.glows;
            }

            set
            {
                this.glows = value;
                this.Props.Glows = value;
                this.needUpdate = true;
            }
        }

        [Unsaved]
        private bool needUpdate = true;
    }

    public class CompProperties_GlowerPulse : CompProperties_Glower
    {
        public CompProperties_GlowerPulse()
        {
            this.compClass = typeof(CompGlowerPulse);
        }
        public int lastTick = 0;

        public float minGlowRadius = 10f;
        public float maxGlowRadius = 15f;

        private int lag = Rand.Int % 60;

        public ColorInt minGlowColor = new ColorInt(255, 255, 255, 0) * 0.45f;
        public ColorInt maxGlowColor = new ColorInt(255, 255, 255, 0) * 1.45f;

        public float intervalTicks = 60;

        public Type easingType;

        private IEasing easing = null;

        public bool Glows { get; set; }

        public bool pulse = true;

        public void Update()
        {
            if (this.lastTick == Find.TickManager.TicksGame)
            {
                return;
            }
            this.lastTick = Find.TickManager.TicksGame;
            if (this.Glows)
            {
                if (!pulse)
                {
                    this.glowColor = Color32.Lerp(minGlowColor.ToColor32, maxGlowColor.ToColor32, 0.5f).AsColorInt();
                    this.glowRadius = Mathf.Lerp(minGlowRadius, maxGlowRadius, 0.5f);
                    return;
                }
                if (this.easing == null)
                {
                    if (this.easingType != null)
                    {
                        this.easing = (IEasing)Activator.CreateInstance(this.easingType);
                    }
                    else
                    {
                        this.easing = new EasingLinear();
                    }
                }
                var time = ((Find.TickManager.TicksGame + lag) % (this.intervalTicks * 2)) / this.intervalTicks;
                if (time > 1.0f)
                {
                    time = 2.0f - time;
                }
                var factor = this.easing.GetValue(time);

                this.glowColor = Color32.Lerp(minGlowColor.ToColor32, maxGlowColor.ToColor32, factor).AsColorInt();
                this.glowRadius = Mathf.Lerp(minGlowRadius, maxGlowRadius, factor);
            }
            else
            {
                this.glowColor = ((Color32)Color.clear).AsColorInt();
                this.glowRadius = 0;
            }
        }
    }

    #region Easing

    public interface IEasing
    {
        float GetValue(float t);
    }

    public class EasingLinear : IEasing
    {
        public float GetValue(float t)
        {
            return t;
        }
    }

    public class EasingSineInOut : IEasing
    {
        public float GetValue(float t)
        {
            return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
        }
    }

    public class EasingQuadInOut : IEasing
    {
        public float GetValue(float t)
        {
            return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        }
    }

    public class EasingQuadIn : IEasing
    {
        public float GetValue(float t)
        {
            return t * t;
        }
    }

    public class EasingQuadOut : IEasing
    {
        public float GetValue(float t)
        {
            return 1 - (1 - t) * (1 - t);
        }
    }

    public class EasingCubicInOut : IEasing
    {
        public float GetValue(float t)
        {
            return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }
    }

    public class EasingCubicIn : IEasing
    {
        public float GetValue(float t)
        {
            return t * t * t;
        }
    }

    public class EasingCubicOut : IEasing
    {
        public float GetValue(float t)
        {
            return 1 - Mathf.Pow(1 - t, 3);
        }
    }

    public class EasingExpInOut : IEasing
    {
        public float GetValue(float t)
        {
            return t == 0 
                ? 0 
                : t == 1 
                ? 1 
                : t < 0.5f 
                ? Mathf.Pow(2, 20 * t - 10) / 2 
                : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;
        }
    }

    public class EasingElasticInOut : IEasing
    {
        const float c5 = (float)((2 * Math.PI) / 4.5f);

        public float GetValue(float t)
        {

            return t == 0
              ? 0
              : t == 1
              ? 1
              : t < 0.5f
              ? -(Mathf.Pow(2, 20 * t - 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2
              : (Mathf.Pow(2, -20 * t + 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2 + 1;
        }
    }

    public class EasingBounceInOut : IEasing
    {
        public float GetValue(float t)
        {
            return t < 0.5
                ? (1 - EaseBounceOut(1 - 2 * t)) / 2
                : (1 + EaseBounceOut(2 * t - 1)) / 2;
        }

        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        private float EaseBounceOut(float t)
        {
            if (t < 1 / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2 / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5 / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }
    }
    #endregion
}
