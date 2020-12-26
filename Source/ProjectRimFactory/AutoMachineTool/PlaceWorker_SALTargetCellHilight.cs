using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.AutoMachineTool
{

    //TODO Update to Hilight the Workbech cell for S.A.L
    class PlaceWorker_SALTargetCellHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {

            IntVec3 tragetCell = center;
            switch (rot.AsInt)
            {
                case 0:
                    //North
                    tragetCell.z++;
                    break;
                case 1:
                    //East
                    tragetCell.x++;
                    break;
                case 2:
                    //South
                    tragetCell.z--;
                    break;
                case 3:
                    //West
                    tragetCell.x--;
                    break;
                default:
                    //Default North
                    tragetCell.z++;
                    break;
            }
            GenDraw.DrawFieldEdges(new List<IntVec3> { tragetCell }, Common.CommonColors.outputZone);

        }
    }
}
