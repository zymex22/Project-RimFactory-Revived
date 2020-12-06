using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class CompGlowerPulse : CompGlower, ITicker
    {
        private bool glows = true;

        [Unsaved] private bool needUpdate = true;

        public new CompProperties_GlowerPulse Props => (CompProperties_GlowerPulse) props;

        public new bool Glows
        {
            get => glows;

            set
            {
                glows = value;
                Props.Glows = value;
                needUpdate = true;
            }
        }

        public void Tick()
        {
            if (needUpdate || Props.pulse)
            {
                Props.Update();
                parent.Map.glowGrid.MarkGlowGridDirty(parent.Position);
                needUpdate = false;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref glows, "glows", true);
            Props.Glows = glows;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Props.Glows = glows;

            parent.Map.GetComponent<PRFMapComponent>()?.AddTicker(this);
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            map.GetComponent<PRFMapComponent>()?.RemoveTicker(this);
        }
    }

    public class CompProperties_GlowerPulse : CompProperties_Glower
    {
        private IEasing easing;

        public Type easingType;

        public float intervalTicks = 60;

        private readonly int lag = Rand.Int % 60;
        public int lastTick;
        public ColorInt maxGlowColor = new ColorInt(255, 255, 255, 0) * 1.45f;
        public float maxGlowRadius = 15f;

        public ColorInt minGlowColor = new ColorInt(255, 255, 255, 0) * 0.45f;

        public float minGlowRadius = 10f;

        public bool pulse = true;

        public CompProperties_GlowerPulse()
        {
            compClass = typeof(CompGlowerPulse);
        }

        public bool Glows { get; set; }

        public void Update()
        {
            if (lastTick == Find.TickManager.TicksGame) return;
            lastTick = Find.TickManager.TicksGame;
            if (Glows)
            {
                if (!pulse)
                {
                    glowColor = Color32.Lerp(minGlowColor.ToColor32, maxGlowColor.ToColor32, 0.5f).AsColorInt();
                    glowRadius = Mathf.Lerp(minGlowRadius, maxGlowRadius, 0.5f);
                    return;
                }

                if (easing == null)
                {
                    if (easingType != null)
                        easing = (IEasing) Activator.CreateInstance(easingType);
                    else
                        easing = new EasingLinear();
                }

                var time = (Find.TickManager.TicksGame + lag) % (intervalTicks * 2) / intervalTicks;
                if (time > 1.0f) time = 2.0f - time;
                var factor = easing.GetValue(time);

                glowColor = Color32.Lerp(minGlowColor.ToColor32, maxGlowColor.ToColor32, factor).AsColorInt();
                glowRadius = Mathf.Lerp(minGlowRadius, maxGlowRadius, factor);
            }
            else
            {
                glowColor = ((Color32) Color.clear).AsColorInt();
                glowRadius = 0;
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
        private const float c5 = (float) (2 * Math.PI / 4.5f);

        public float GetValue(float t)
        {
            return t == 0
                ? 0
                : t == 1
                    ? 1
                    : t < 0.5f
                        ? -(Mathf.Pow(2, 20 * t - 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2
                        : Mathf.Pow(2, -20 * t + 10) * Mathf.Sin((20 * t - 11.125f) * c5) / 2 + 1;
        }
    }

    public class EasingBounceInOut : IEasing
    {
        private const float n1 = 7.5625f;
        private const float d1 = 2.75f;

        public float GetValue(float t)
        {
            return t < 0.5
                ? (1 - EaseBounceOut(1 - 2 * t)) / 2
                : (1 + EaseBounceOut(2 * t - 1)) / 2;
        }

        private float EaseBounceOut(float t)
        {
            if (t < 1 / d1)
                return n1 * t * t;
            if (t < 2 / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            if (t < 2.5 / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }

    #endregion
}