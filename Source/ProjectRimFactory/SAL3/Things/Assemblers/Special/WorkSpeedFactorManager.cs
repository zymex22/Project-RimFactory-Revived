using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers.Special
{
    public sealed class WorkSpeedFactorEntry : IExposable, IComparable<WorkSpeedFactorEntry>
    {
        private float factorCached;
        private int lastTick;
        private float learningRateCached = WorkSpeedFactorManager.LearningRateCachedDefault;

        public float LearningRate
        {
            get => learningRateCached;
            set
            {
                UpdateFactorCache();
                learningRateCached = value;
            }
        }

        public int DeltaTicks => Find.TickManager.TicksAbs - lastTick;

        public float FactorFinal
        {
            get => factorCached * Mathf.Pow(2, -(DeltaTicks * LearningRate));
            set
            {
                factorCached = value;
                lastTick = Find.TickManager.TicksAbs;
            }
        }

        public int CompareTo(WorkSpeedFactorEntry other)
        {
            return FactorFinal.CompareTo(other.FactorFinal);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref lastTick, "lastTick");
            Scribe_Values.Look(ref factorCached, "factorCached");
            Scribe_Values.Look(ref learningRateCached, "learningRateCached", forceSave: true);
        }

        private void UpdateFactorCache()
        {
            if (factorCached != 0f) factorCached *= Mathf.Pow(2, -(DeltaTicks * LearningRate));
            lastTick = Find.TickManager.TicksAbs;
        }
    }

    public class WorkSpeedFactorManager : IExposable
    {
        public const float LearningRateCachedDefault = 1f / GenDate.TicksPerTwelfth;
        public Dictionary<RecipeDef, WorkSpeedFactorEntry> factors = new Dictionary<RecipeDef, WorkSpeedFactorEntry>();
        private float learningRateCached = LearningRateCachedDefault;

        public virtual float LearningRate
        {
            get => learningRateCached;
            set
            {
                foreach (var recipe in factors.Keys) factors[recipe].LearningRate = value;
                learningRateCached = value;
            }
        }

        public virtual void ExposeData()
        {
            Scribe_Collections.Look(ref factors, "factors", LookMode.Def, LookMode.Deep);
            Scribe_Values.Look(ref learningRateCached, "learningRateCached", LearningRateCachedDefault);
        }

        public virtual void IncreaseWeight(RecipeDef recipe, float factor)
        {
            if (factors.TryGetValue(recipe, out var entry))
                entry.FactorFinal += factor;
            else
                factors.Add(recipe, new WorkSpeedFactorEntry {FactorFinal = factor});
        }

        public virtual float GetFactorFor(RecipeDef recipe)
        {
            if (factors.TryGetValue(recipe, out var val)) return val.FactorFinal;
            return 0f;
        }

        public void TrimUnnecessaryFactors()
        {
            var keysList = factors.Keys.ToList(); // ToList evaluates the KeyCollection.Enumerator
            for (var i = 0; i < keysList.Count; i++)
            {
                var key = keysList[i];
                if (factors[key].FactorFinal < 0.005f) // Pruned if under 0.5%
                    factors.Remove(key);
            }
        }
    }
}