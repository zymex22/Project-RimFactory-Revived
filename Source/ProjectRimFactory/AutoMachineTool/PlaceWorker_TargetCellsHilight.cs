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
    class PlaceWorker_SALTargetWorkCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;

            if (true)
            {
                //Log.Message("--------- targetCellResolver not found. - Sniper");
                return;
            }

            //var machine = center.GetThingList(map).Where(t => t.def == def).SelectMany(t => Option(t as IRange)).FirstOption();
            //if (machine.HasValue)
            //{
            //    machine.Value.GetAllTargetCells().Select(c => new { Cell = c, Color = CommonColors.GetCellPatternColor(CommonColors.CellPattern.Instance) })
            //        .GroupBy(a => a.Color)
            //        .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
            //}
            //else
            //{
            //    var min = ext.TargetCellResolver.GetRangeCells(def, center, def.size, map, rot, ext.TargetCellResolver.MinRange());
            //    var max = ext.TargetCellResolver.GetRangeCells(def, center, def.size, map, rot, ext.TargetCellResolver.MaxRange());
            //    min.Select(c => new { Cell = c, Color = CommonColors.GetCellPatternColor(CommonColors.CellPattern.BlurprintMin) })
            //        .Concat(max.Select(c => new { Cell = c, Color = CommonColors.GetCellPatternColor(CommonColors.CellPattern.BlurprintMax) }))
            //        .GroupBy(a => a.Color)
            //        .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
            //}

            //map.listerThings.ThingsOfDef(def).SelectMany(t => Option(t as IRange)).Where(r => r.Position != center).ForEach(r =>
            //        r.GetAllTargetCells().Select(c => new { Cell = c, Color = CommonColors.GetCellPatternColor(CommonColors.CellPattern.OtherInstance) })
            //            .GroupBy(a => a.Color)
            //            .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key)));
        }
    }
}
