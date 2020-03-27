using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Storage
{
    public class OutputSettings : IExposable
    {
        public OutputSettings(string minTooltip, string maxTooltip)
        {
            this.minTooltip = minTooltip;
            this.maxTooltip = maxTooltip;
            useMin = false;
            useMax = false;
            min = 0;
            max = 75;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref minTooltip, "minTooltip");
            Scribe_Values.Look(ref maxTooltip, "maxTooltip");
            Scribe_Values.Look(ref useMin, "useMin");
            Scribe_Values.Look(ref useMax, "useMax");
            Scribe_Values.Look(ref min, "min");
            Scribe_Values.Look(ref max, "max");
        }
        public bool SatisfiesMax(int stackCount, int stackLimit)
        {
            return CountNeededToReachMax(stackCount, stackLimit) > 0;
        }
        public bool SatisfiesMin(int stackCount)
        {
            return !useMin || stackCount >= min;
        }
        public int CountNeededToReachMax(int currentCount, int limit)
        {
            if (useMax)
                return Math.Min(limit, max) - currentCount;
            return limit - currentCount;
        }
        public string minTooltip;
        public string maxTooltip;
        public bool useMin;
        public bool useMax;
        public int min;
        public int max;
    }
}
