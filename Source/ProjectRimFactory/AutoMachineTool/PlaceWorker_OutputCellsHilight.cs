using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using ProjectRimFactory.Common;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    //Used in Pullers
    class PlaceWorker_OutputCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;
            if (true)
            {
                //   Log.Message("outputCellResolver not found. sniper");
                return;
            }

            //ext.OutputCellResolver.OutputCell(def, center, def.Size, map, rot).ForEach(c =>
            //    GenDraw.DrawFieldEdges(new List<IntVec3>().Append(c), CommonColors.GetCellPatternColor(CommonColors.CellPattern.OutputCell)));
            //ext.OutputCellResolver.OutputZoneCells(def, center, def.Size, map, rot)
            //    .Select(c => new { Cell = c, Color = CommonColors.GetCellPatternColor(CommonColors.CellPattern.OutputZone) })
            //    .GroupBy(a => a.Color)
            //    .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
        }
    }
}
