using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Industry
{
    public class PlaceWorker_FuelingMachine : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            base.DrawGhost(def, center, rot, ghostCol);
            GenDraw.DrawFieldEdges(new List<IntVec3>() { rot.FacingCell + center }, Color.yellow);
        }
    }
}
