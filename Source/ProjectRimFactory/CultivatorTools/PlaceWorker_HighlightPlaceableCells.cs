using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    public class PlaceWorker_HighlightPlaceableCells : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            base.DrawGhost(def, center, rot, ghostCol);
            var squareAreaRadius = def.GetModExtension<CultivatorDefModExtension>().squareAreaRadius;
            var list = new List<IntVec3>((squareAreaRadius * 2 + 1) * (squareAreaRadius * 2 + 1));
            for (var i = -squareAreaRadius; i <= squareAreaRadius; i++)
            for (var j = -squareAreaRadius; j <= squareAreaRadius; j++)
                list.Add(new IntVec3(i, 0, j) + center);
            GenDraw.DrawFieldEdges(list);
        }
    }
}