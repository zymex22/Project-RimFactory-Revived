using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    internal class PlaceWorker_InputCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var map = Find.CurrentMap;
            var ext = def.GetModExtension<ModExtension_WorkIORange>();
            if (ext == null || ext.InputCellResolver == null)
            {
                UnityEngine.Debug.LogWarning("inputCellResolver not found.");
                return;
            }

            ext.InputCellResolver.InputCell(def, center, def.Size, map, rot).ForEach(c =>
                GenDraw.DrawFieldEdges(new List<IntVec3>().Append(c),
                    ext.InputCellResolver.GetColor(c, map, rot, CellPattern.InputCell)));
            ext.InputCellResolver.InputZoneCells(def, center, def.Size, map, rot)
                .Select(c => new {Cell = c, Color = ext.InputCellResolver.GetColor(c, map, rot, CellPattern.InputZone)})
                .GroupBy(a => a.Color)
                .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
        }
    }
}