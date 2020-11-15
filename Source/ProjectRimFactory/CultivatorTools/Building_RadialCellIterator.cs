using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using RimWorld;

namespace ProjectRimFactory.CultivatorTools
{
    public abstract class Building_RadialCellIterator : Building
    {
        /// <summary>
        /// This index is offset by one as seen in <see cref="Current"/>.
        /// </summary>
        public int currentRadialNumber;
        public int RadialCellCount { get; private set; }
        public virtual bool Fueled
        {
            get
            {
                return GetComp<CompRefuelable>()?.HasFuel ?? true;
            }
        }
        public virtual bool Powered
        {
            get
            {
                return GetComp<CompPowerTrader>()?.PowerOn ?? true;
            }
        }
        public virtual int TickRate
        {
            get
            {
                return 250;
            }
        }
        public IntVec3 Current
        {
            get
            {
                return GenRadial.RadialPattern[currentRadialNumber + 1] + Position;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentRadialNumber, "currentNumber", 1);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            RadialCellCount = GenRadial.NumCellsInRadius(def.specialDisplayRadius);
        }
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(new List<IntVec3> { Current }, Color.yellow);
        }
        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % TickRate == 0 && Powered && Fueled)
                DoTickerWork();
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

        protected virtual void MoveNextInternal()
        {
            for (int i = 0; i < 10; i++)
            {
                currentRadialNumber++;
                var num = RadialCellCount;
                if (currentRadialNumber + 1 >= num)
                    currentRadialNumber = 0;
                //Log.Message("Iterating. Current number: " + currentRadialNumber + " Limit: " + num);
                var cell = Current;
                if (CellValidator(cell))
                {
                    break;
                }
            }
        }
        public virtual bool CellValidator(IntVec3 c)
        {
            return c.InBounds(Map);
        }

        public abstract bool DoIterationWork(IntVec3 c);
    }
}
