using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones
{
    public class PlaceWorker_DroneStation : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            base.DrawGhost(def, center, rot, ghostCol);

            var radius = def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius;
            if (radius <= 0) return;
            var list = new List<IntVec3>((radius * 2 + 1) * (radius * 2 + 1));
            for (var i = -radius; i <= radius; i++)
            {
                for (var j = -radius; j <= radius; j++)
                {
                    list.Add(new IntVec3(i, 0, j) + center);
                }
            }
            GenDraw.DrawFieldEdges(list);

        }
    }
}
