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
            int squareAreaRadius = def.GetModExtension<CultivatorDefModExtension>().squareAreaRadius;
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
