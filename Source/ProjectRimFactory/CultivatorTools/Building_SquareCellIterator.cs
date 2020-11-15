using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    public abstract class Building_SquareCellIterator : Building_CellIterator
    {
        public SquareCellIterator iter;

        public override IntVec3 Current => iter.cellPattern[currentPosition] + Position;
        
        Cache<List<IntVec3>> selectedCellsCache;
        List<IntVec3> UpdateCellsCache()
        {
            int squareAreaRadius = def.GetModExtension<CultivatorDefModExtension>().squareAreaRadius;
            List<IntVec3> list = new List<IntVec3>((squareAreaRadius * 2 + 1) * (squareAreaRadius * 2 + 1));
            for (int i = -squareAreaRadius; i <= squareAreaRadius; i++)
            {
                for (int j = -squareAreaRadius; j <= squareAreaRadius; j++)
                {
                    list.Add(new IntVec3(i, 0, j) + Position);
                }
            }
            return list;
        }
        public List<IntVec3> CellsInRange
        {
            get
            {
                return selectedCellsCache.Get();
            }
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

        protected override int cellCount => iter.cellPattern.Length;
        
    }
}
