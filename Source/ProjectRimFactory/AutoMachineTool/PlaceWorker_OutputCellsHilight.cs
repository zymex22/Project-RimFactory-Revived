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

namespace ProjectRimFactory.AutoMachineTool
{
    class PlaceWorker_OutputCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;
            var ext = def.GetModExtension<ModExtension_WorkIORange>();
            if (ext == null || ext.OutputCellResolver == null)
            {
                UnityEngine.Debug.LogWarning("outputCellResolver not found.");
                return;
            }

            ext.OutputCellResolver.OutputCell(def, center, def.Size, map, rot).ForEach(c =>
                GenDraw.DrawFieldEdges(new List<IntVec3>().Append(c), ext.OutputCellResolver.GetColor(c, map, rot, CellPattern.OutputCell)));
            ext.OutputCellResolver.OutputZoneCells(def, center, def.Size, map, rot)
                .Select(c => new { Cell = c, Color = ext.OutputCellResolver.GetColor(c, map, rot, CellPattern.OutputZone) })
                .GroupBy(a => a.Color)
                .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
        }
    }
}
