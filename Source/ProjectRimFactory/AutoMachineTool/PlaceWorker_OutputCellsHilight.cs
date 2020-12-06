using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    internal class PlaceWorker_OutputCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var map = Find.CurrentMap;
            var ext = def.GetModExtension<ModExtension_WorkIORange>();
            if (ext == null || ext.OutputCellResolver == null)
            {
                UnityEngine.Debug.LogWarning("outputCellResolver not found.");
                return;
            }

            ext.OutputCellResolver.OutputCell(def, center, def.Size, map, rot).ForEach(c =>
                GenDraw.DrawFieldEdges(new List<IntVec3>().Append(c),
                    ext.OutputCellResolver.GetColor(c, map, rot, CellPattern.OutputCell)));
            ext.OutputCellResolver.OutputZoneCells(def, center, def.Size, map, rot)
                .Select(c => new
                    {Cell = c, Color = ext.OutputCellResolver.GetColor(c, map, rot, CellPattern.OutputZone)})
                .GroupBy(a => a.Color)
                .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
        }
    }
}