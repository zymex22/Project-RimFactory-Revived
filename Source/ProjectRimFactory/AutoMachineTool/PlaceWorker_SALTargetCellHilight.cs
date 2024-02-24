using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{

    //TODO Update to Hilight the Workbech cell for S.A.L
    class PlaceWorker_SALTargetCellHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            IntVec3 tragetCell = center + rot.FacingCell;

            GenDraw.DrawFieldEdges(new List<IntVec3> { tragetCell }, Common.CommonColors.WorkbenchAlpha);

        }
    }
}
