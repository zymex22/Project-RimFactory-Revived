using System.Collections.Generic;
using UnityEngine;
using Verse;

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

        //reverses the action performed by Verse.GenAdj.AdjustForRotation(ref IntVec3 center, ref IntVec2 size, Rot4 rot)
        //This is needed as the game calls it in CellsOccupiedBy and its purpose is the rotation around the mouse pointer.
        public static void CounterAdjustForRotation(ref IntVec3 center, ref IntVec2 size, Rot4 rot)
        {
            if (size.x == 1 && size.z == 1)
            {
                return;
            }
            if (rot.IsHorizontal)
            {
                int x = size.x;
                size.x = size.z;
                size.z = x;
            }
            switch (rot.AsInt)
            {
                case 0:
                    break;
                case 1:
                    if (size.z % 2 == 0)
                    {
                        center.z++;
                    }
                    break;
                case 2:
                    if (size.x % 2 == 0)
                    {
                        center.x++;
                    }
                    if (size.z % 2 == 0)
                    {
                        center.z++;
                    }
                    break;
                case 3:
                    if (size.x % 2 == 0)
                    {
                        center.x++;
                    }
                    break;
            }
        }



    }
}
