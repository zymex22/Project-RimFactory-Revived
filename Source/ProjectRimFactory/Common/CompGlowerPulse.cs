using RimWorld;
using System;
using UnityEngine;
using Verse;
// ReSharper disable MemberCanBePrivate.Global

namespace ProjectRimFactory.Common
{
    public class CompGlowerPulse : CompGlower, ITicker
    {
        private new CompProperties_GlowerPulse Props => (CompProperties_GlowerPulse)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref glows, "glows", true);
            Props.Glows = glows;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Props.Glows = glows;

            parent.Map.GetComponent<PRFMapComponent>()?.AddTicker(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);

            map.GetComponent<PRFMapComponent>()?.RemoveTicker(this);
        }

        public void Tick()
        {
            if (!needUpdate && !Props.pulse) return;
            Props.Update();
            // MarkGlowGridDirty
            //DirtyCache
            parent.Map.glowGrid.DirtyCell(parent.Position);
            needUpdate = false;
        }

        private bool glows = true;

        public new bool Glows
        {
            set
            {
                glows = value;
                Props.Glows = value;
                needUpdate = true;
            }
        }

        [Unsaved]
        private bool needUpdate = true;
    }

    public class CompProperties_GlowerPulse : CompProperties_Glower
    {
        public CompProperties_GlowerPulse()
        {
            compClass = typeof(CompGlowerPulse);
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
            if (lastTick == Find.TickManager.TicksGame)
            {
                return;
            }
            lastTick = Find.TickManager.TicksGame;
            if (Glows)
            {
                if (!pulse)
                {
                    glowColor = Color32.Lerp(minGlowColor.ProjectToColor32(), maxGlowColor.ProjectToColor32(), 0.5f).AsColorInt();
                    glowRadius = Mathf.Lerp(minGlowRadius, maxGlowRadius, 0.5f);
                    return;
                }
                if (easing == null)
                {
                    if (easingType != null)
                    {
                        easing = (IEasing)Activator.CreateInstance(easingType);
                    }
                    else
                    {
                        easing = new EasingLinear();
                    }
                }
                var time = ((Find.TickManager.TicksGame + lag) % (intervalTicks * 2)) / intervalTicks;
                if (time > 1.0f)
                {
                    time = 2.0f - time;
                }
                var factor = easing.GetValue(time);

                glowColor = Color32.Lerp(minGlowColor.ProjectToColor32(), maxGlowColor.ProjectToColor32(), factor).AsColorInt();
                glowRadius = Mathf.Lerp(minGlowRadius, maxGlowRadius, factor);
            }
            else
            {
                glowColor = ((Color32)Color.clear).AsColorInt();
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
