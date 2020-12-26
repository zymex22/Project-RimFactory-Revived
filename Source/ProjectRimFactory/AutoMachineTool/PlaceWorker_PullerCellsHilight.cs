using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    class PlaceWorker_PullerCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            //base.DrawGhost(def, center, rot, ghostCol, thing);
            IntVec3 inputCell = center;
            IntVec3 outputCell = center;

            //outputCell = center + rot.FacingCell;
            inputCell = center + rot.Opposite.FacingCell;

            //Not shure how i should sopport the angeled one
            bool isRight = false;

            if (thing != null) {
                isRight = (thing as Building_ItemPuller).Getright;
            }
            outputCell = def.GetModExtension<ModExtension_Puller>().GetOutputCell(center, rot, isRight);

            GenDraw.DrawFieldEdges(new List<IntVec3> { inputCell },Common.CommonColors.inputCell);
            GenDraw.DrawFieldEdges(new List<IntVec3> { outputCell }, Common.CommonColors.outputCell);
        }
    }
}
