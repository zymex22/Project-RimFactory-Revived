using System.Collections.Generic;
using ProjectRimFactory.Common;
using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    public abstract class Building_SquareCellIterator : Building_CellIterator
    {
        public SquareCellIterator iter;

        private Cache<List<IntVec3>> selectedCellsCache;

        public override IntVec3 Current => iter.cellPattern[currentPosition] + Position;

        public List<IntVec3> CellsInRange => selectedCellsCache.Get();

        protected override int cellCount => iter.cellPattern.Length;

        private List<IntVec3> UpdateCellsCache()
        {
            var squareAreaRadius = def.GetModExtension<CultivatorDefModExtension>().squareAreaRadius;
            var list = new List<IntVec3>((squareAreaRadius * 2 + 1) * (squareAreaRadius * 2 + 1));
            for (var i = -squareAreaRadius; i <= squareAreaRadius; i++)
            for (var j = -squareAreaRadius; j <= squareAreaRadius; j++)
                list.Add(new IntVec3(i, 0, j) + Position);
            return list;
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(CellsInRange);
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            iter = new SquareCellIterator(def.GetModExtension<CultivatorDefModExtension>().squareAreaRadius);
            selectedCellsCache = new Cache<List<IntVec3>>(UpdateCellsCache);
        }
    }
}