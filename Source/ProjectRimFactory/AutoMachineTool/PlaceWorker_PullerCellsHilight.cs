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

            //Shall draw that input & Output cells for Pullers

            //Input  --> CommonColors.GetCellPatternColor(CommonColors.CellPattern.InputZone)
            //Output --> CommonColors.GetCellPatternColor(CommonColors.CellPattern.OutputZone)





        }
    }
}
