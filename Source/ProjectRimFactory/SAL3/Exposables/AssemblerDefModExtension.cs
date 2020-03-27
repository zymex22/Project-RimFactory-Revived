using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.SAL3.Exposables
{
    public class AssemblerDefModExtension : DefModExtension
    {
        public float workSpeedBaseFactor = 1f;
        public ThingDef importRecipesFrom;
    }
}
