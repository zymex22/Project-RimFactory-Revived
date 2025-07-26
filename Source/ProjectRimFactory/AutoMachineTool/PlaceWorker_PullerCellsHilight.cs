using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    class PlaceWorker_PullerCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            //base.DrawGhost(def, center, rot, ghostCol, thing);
            IntVec3 outputCell;

            //outputCell = center + rot.FacingCell;
            var inputCell = center + rot.Opposite.FacingCell;

            //Not shure how i should sopport the angeled one
            var isRight = false;

            if (thing != null && !thing.def.IsBlueprint)
            {
                isRight = (thing as Building_ItemPuller)?.GetRight ?? false;
            }
            if (def.IsBlueprint || def.IsFrame)
            {
                outputCell = def.entityDefToBuild.GetModExtension<ModExtension_Puller>().GetOutputCell(center, rot, isRight);
            }
            else
            {
                outputCell = def.GetModExtension<ModExtension_Puller>().GetOutputCell(center, rot, isRight);
            }

            GenDraw.DrawFieldEdges([inputCell], Common.CommonColors.inputCell);
            GenDraw.DrawFieldEdges([outputCell], Common.CommonColors.outputCell);
        }
    }
}
