using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Linq;


namespace ProjectRimFactory.Drones
{
    public class PlaceWorker_DroneStation : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            base.DrawGhost(def, center, rot, ghostCol);

            if (def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius > 0)
            {
                int squareAreaRadius = def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius;
                List<IntVec3> list = new List<IntVec3>((squareAreaRadius * 2 + 1) * (squareAreaRadius * 2 + 1));
                for (int i = -squareAreaRadius; i <= squareAreaRadius; i++)
                {
                    for (int j = -squareAreaRadius; j <= squareAreaRadius; j++)
                    {
                        list.Add(new IntVec3(i, 0, j) + center);
                    }
                }
                GenDraw.DrawFieldEdges(list);
            }

        }
    }
}
