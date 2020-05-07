using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRimFactory.Common
{
    public static class Util
    {
        public static Color A(this Color color, float a)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.A(color, a);
        }

        public static IntVec3 FacingCell(IntVec3 center, IntVec2 size, Rot4 rot)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.FacingCell(center, size, rot);
        }

        public static IEnumerable<IntVec3> FacingRect(IntVec3 pos, Rot4 dir, int range)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.FacingRect(pos, dir, range);
        }

        public static IEnumerable<IntVec3> FacingRect(Thing thing, Rot4 dir, int range)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.FacingRect(thing, dir, range);
        }

        public static IEnumerable<IntVec3> FacingRect(IntVec3 center, IntVec2 size, Rot4 dir, int range)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.FacingRect(center, size, dir, range);
        }
    }
}
