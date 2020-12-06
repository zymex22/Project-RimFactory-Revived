using System.Collections.Generic;
using ProjectRimFactory.AutoMachineTool;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public static class Util
    {
        public static Color A(this Color color, float a)
        {
            return Ops.A(color, a);
        }

        public static IntVec3 FacingCell(IntVec3 center, IntVec2 size, Rot4 rot)
        {
            return Ops.FacingCell(center, size, rot);
        }

        public static IEnumerable<IntVec3> FacingRect(IntVec3 pos, Rot4 dir, int range)
        {
            return Ops.FacingRect(pos, dir, range);
        }

        public static IEnumerable<IntVec3> FacingRect(Thing thing, Rot4 dir, int range)
        {
            return Ops.FacingRect(thing, dir, range);
        }

        public static IEnumerable<IntVec3> FacingRect(IntVec3 center, IntVec2 size, Rot4 dir, int range)
        {
            return Ops.FacingRect(center, size, dir, range);
        }
    }
}