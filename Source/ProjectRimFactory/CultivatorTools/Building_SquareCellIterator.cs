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
    public abstract class Building_SquareCellIterator : Building
    {
        public SquareCellIterator iter;
        protected int currentPosition;

        public IntVec3 Current => iter.cellPattern[currentPosition] + Position;
        public bool Fueled => GetComp<CompRefuelable>()?.HasFuel ?? true;
        public bool Powered => GetComp<CompPowerTrader>()?.PowerOn ?? true;
        public virtual int TickRate => 250;

        public virtual bool CellValidator(IntVec3 c)
        {
            return c.InBounds(Map);
        }
        public abstract bool DoIterationWork(IntVec3 c);
        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % TickRate == 0 && Powered && Fueled)
                DoTickerWork();
        }
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
            GenDraw.DrawFieldEdges(new List<IntVec3> { Current }, Color.yellow);
            GenDraw.DrawFieldEdges(CellsInRange);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentPosition, "currentNumber", 1);
        }
        public void DoTickerWork()
        {
            base.TickRare();
            var cell = Current;
            if (CellValidator(cell))
            {
                if (!DoIterationWork(cell)) return;
            }
            MoveNextInternal();
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            iter = new SquareCellIterator(def.GetModExtension<CultivatorDefModExtension>().squareAreaRadius);
            selectedCellsCache = new Cache<List<IntVec3>>(UpdateCellsCache);
        }
        protected virtual void MoveNextInternal()
        {
            for (int i = 0; i < 10; i++)
            {
                currentPosition++;
                var num = iter.cellPattern.Length;
                if (currentPosition >= num)
                    currentPosition = 0;
                var cell = Current;
                if (CellValidator(cell))
                {
                    break;
                }
            }
        }
    }
}
