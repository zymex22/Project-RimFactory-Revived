using System.Linq;
using UnityEngine;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    internal class PlaceWorker_TargetCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var map = Find.CurrentMap;
            var ext = def.GetModExtension<ModExtension_WorkIORange>();

            if (ext == null || ext.TargetCellResolver == null)
            {
                UnityEngine.Debug.LogWarning("targetCellResolver not found.");
                return;
            }

            var machine = center.GetThingList(map).Where(t => t.def == def).SelectMany(t => Option(t as IRange))
                .FirstOption();
            if (machine.HasValue)
            {
                machine.Value.GetAllTargetCells().Select(c => new
                        {Cell = c, Color = ext.TargetCellResolver.GetColor(c, map, rot, CellPattern.Instance)})
                    .GroupBy(a => a.Color)
                    .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
            }
            else
            {
                var min = ext.TargetCellResolver.GetRangeCells(def, center, def.size, map, rot,
                    ext.TargetCellResolver.MinRange());
                var max = ext.TargetCellResolver.GetRangeCells(def, center, def.size, map, rot,
                    ext.TargetCellResolver.MaxRange());
                min.Select(c => new
                        {Cell = c, Color = ext.TargetCellResolver.GetColor(c, map, rot, CellPattern.BlurprintMin)})
                    .Concat(max.Select(c => new
                        {Cell = c, Color = ext.TargetCellResolver.GetColor(c, map, rot, CellPattern.BlurprintMax)}))
                    .GroupBy(a => a.Color)
                    .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
            }

            map.listerThings.ThingsOfDef(def).SelectMany(t => Option(t as IRange)).Where(r => r.Position != center)
                .ForEach(r =>
                    r.GetAllTargetCells().Select(c => new
                            {Cell = c, Color = ext.TargetCellResolver.GetColor(c, map, rot, CellPattern.OtherInstance)})
                        .GroupBy(a => a.Color)
                        .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key)));
        }
    }
}