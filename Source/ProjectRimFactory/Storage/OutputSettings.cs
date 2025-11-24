using System;
using Verse;

namespace ProjectRimFactory.Storage
{
    public class OutputSettings(string minTooltip, string maxTooltip) : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref MinTooltip, "minTooltip");
            Scribe_Values.Look(ref MaxTooltip, "maxTooltip");
            Scribe_Values.Look(ref UseMin, "useMin");
            Scribe_Values.Look(ref UseMax, "useMax");
            Scribe_Values.Look(ref min, "min");
            Scribe_Values.Look(ref max, "max");
            
            // Just in case for "broken" saves
            // May be removed in the future
            if (min > max) max = min;
        }
        public bool SatisfiesMax(int stackCount, int stackLimit)
        {
            return CountNeededToReachMax(stackCount, stackLimit) > 0;
        }
        public bool SatisfiesMin(int stackCount)
        {
            return !UseMin || stackCount >= min;
        }
        public int CountNeededToReachMax(int currentCount, int limit)
        {
            if (UseMax)
            {
                return Math.Min(limit, max) - currentCount;
            }
            return limit - currentCount;
        }

        public void Copy(OutputSettings other)
        {
            other.MinTooltip = MinTooltip;
            other.MaxTooltip = MaxTooltip;
            other.UseMin = UseMin;
            other.UseMax = UseMax;
            other.min = min;
            other.max = max;
        }
        
        public int Min
        {
            get => min;
            set
            {
                min = value;
                if (min > max)
                {
                    max = value;
                }
            }
        }
        
        public int Max
        {
            get => max;
            set
            {
                max = value;
                if (min > max)
                {
                    min = value;
                }
            }
        }

        public string MinTooltip = minTooltip;
        public string MaxTooltip = maxTooltip;
        public bool UseMin;
        public bool UseMax;
        private int min;
        private int max = 75;
    }
}
