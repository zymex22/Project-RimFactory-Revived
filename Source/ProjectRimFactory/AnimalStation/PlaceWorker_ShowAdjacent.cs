using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AnimalStation
{
    // ReSharper disable once UnusedType.Global
    class PlaceWorker_ShowAdjacent : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            base.DrawGhost(def, center, rot, ghostCol, thing);
            GenDraw.DrawFieldEdges(GenAdj.OccupiedRect(center, rot, def.size).ExpandedBy(1).Cells.ToList()
                .FindAll(cell => cell.Standable(Find.CurrentMap)));
        }
    }
}
