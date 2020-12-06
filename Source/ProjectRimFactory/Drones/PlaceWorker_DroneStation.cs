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

            if (def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius > 0)
            {
                var squareAreaRadius = def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius;
                var list = new List<IntVec3>((squareAreaRadius * 2 + 1) * (squareAreaRadius * 2 + 1));
                for (var i = -squareAreaRadius; i <= squareAreaRadius; i++)
                for (var j = -squareAreaRadius; j <= squareAreaRadius; j++)
                    list.Add(new IntVec3(i, 0, j) + center);
                GenDraw.DrawFieldEdges(list);
            }
        }
    }
}