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
            //Log.Message("def.defName " + def.defName);
            IntVec3 inputCell = center;
            IntVec3 outputCell = center;


            switch (rot.AsInt)
            {
                case 0:
                    //North
                    inputCell.z--;
                    outputCell.z++;
                    break;
                case 1:
                    //East
                    outputCell.x++;
                    inputCell.x--;
                    break;
                case 2:
                    //South
                    inputCell.z++;
                    outputCell.z--;
                    break;
                case 3:
                    //West
                    outputCell.x--;
                    inputCell.x++;
                    break;
                default:
                    //Default North
                    inputCell.z--;
                    outputCell.z++;
                    break;
            }

            //Not shure how i should sopport the angeled one



            GenDraw.DrawFieldEdges(new List<IntVec3> { inputCell },Common.CommonColors.inputCell);
            GenDraw.DrawFieldEdges(new List<IntVec3> { outputCell }, Common.CommonColors.outputCell);





        }
    }
}
