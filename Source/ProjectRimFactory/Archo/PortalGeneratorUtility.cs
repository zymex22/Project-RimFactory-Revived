using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Archo
{
    public class PortalGeneratorUtility
    {
        public static void DrawBlueprintFieldEdges(IntVec3 center)
        {
            List<List<IntVec3>> cells = FieldEdgeCells(center);
            // Floor Computer
            GenDraw.DrawFieldEdges(cells[0], new Color(144f / 255f, 149f / 255f, 92f / 255f));
            // Z-Composite
            GenDraw.DrawFieldEdges(cells[1], new Color(112f / 255f, 48f / 255f, 160f / 255f));
            // Y-Composite
            GenDraw.DrawFieldEdges(cells[2], new Color(46f / 255f, 117f / 255f, 182f / 255f));
        }
        public static List<List<IntVec3>> FieldEdgeCells(IntVec3 center)
        {
            List<List<IntVec3>> results = new List<List<IntVec3>>
            {
                new List<IntVec3>(),
                new List<IntVec3>(),
                new List<IntVec3>()
            };
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    bool check1 = Math.Abs(x) <= 1;
                    bool check2 = Math.Abs(z) <= 1;
                    bool check3 = Math.Abs(x) <= 2 && Math.Abs(z) <= 2;
                    if (check1 && check2)
                    {
                        results[0].Add(new IntVec3(center.x + x, center.y, center.z + z));
                    }
                    else if (check3)
                    {
                        results[1].Add(new IntVec3(center.x + x, center.y, center.z + z));
                    }
                    else if (check1 || check2)
                    {
                        results[2].Add(new IntVec3(center.x + x, center.y, center.z + z));
                    }
                }
            }
            return results;
        }
    }
}
