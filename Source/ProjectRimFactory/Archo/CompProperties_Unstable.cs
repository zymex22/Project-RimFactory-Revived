using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Archo
{
    public class CompProperties_Unstable : CompProperties
    {
        public int ticksToDisintegrate;
        public CompProperties_Unstable()
        {
            compClass = typeof(CompUnstable);
        }
    }
}
