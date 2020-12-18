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
    //Only used in pullers
    class PlaceWorker_InputCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;
            if (true)
            {
                //    Log.Message("inputCellResolver not found. Sniper");
                return;
            }
            
            //ext.InputCellResolver.InputCell(def, center, def.Size, map, rot).ForEach(c =>
            //    GenDraw.DrawFieldEdges(new List<IntVec3>().Append(c), CommonColors.GetCellPatternColor(CommonColors.CellPattern.InputCell)));
            //ext.InputCellResolver.InputZoneCells(def, center, def.Size, map, rot)
            //    .Select(c => new { Cell = c, Color = CommonColors.GetCellPatternColor(CommonColors.CellPattern.InputZone) })
            //    .GroupBy(a => a.Color)
            //    .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
        }
    }
}
