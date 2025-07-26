using ProjectRimFactory.Common;
using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    public abstract class Building_SquareCellIterator : Building_CellIterator
    {
        protected SquareCellIterator Iter;

        protected override IntVec3 Current => Iter.CellPattern[CurrentPosition] + Position;

        private Cache<List<IntVec3>> selectedCellsCache;

        private List<IntVec3> UpdateCellsCache()
        {
            var squareAreaRadius = CultivatorDefModExtension.squareAreaRadius;
            var list = new List<IntVec3>((squareAreaRadius * 2 + 1) * (squareAreaRadius * 2 + 1));
            for (var i = -squareAreaRadius; i <= squareAreaRadius; i++)
            {
                for (var j = -squareAreaRadius; j <= squareAreaRadius; j++)
                {
                    list.Add(new IntVec3(i, 0, j) + Position);
                }
            }
            return list;
        }

        private List<IntVec3> CellsInRange => selectedCellsCache.Get();

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(CellsInRange);
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Iter = new SquareCellIterator(CultivatorDefModExtension.squareAreaRadius);
            selectedCellsCache = new Cache<List<IntVec3>>(UpdateCellsCache);
        }

        protected override int CellCount => Iter.CellPattern.Length;

    }
}
