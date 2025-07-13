﻿using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;


namespace ProjectRimFactory.CultivatorTools
{
    public abstract class Building_CellIterator : Building
    {

        public int currentPosition;

        public bool Fueled => GetComp<CompRefuelable>()?.HasFuel ?? true;
        public bool Powered => GetComp<CompPowerTrader>()?.PowerOn ?? true;

        public virtual int TickRate => 250;

        public virtual bool CellValidator(IntVec3 c)
        {
            return c.InBounds(Map);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentPosition, "currentNumber", 1);
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(new List<IntVec3> { Current }, Color.yellow);
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

        public abstract bool DoIterationWork(IntVec3 c);

        abstract public IntVec3 Current { get; }

        abstract protected int cellCount { get; }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (Find.TickManager.TicksGame % TickRate == 0 && Powered && Fueled)
                DoTickerWork();
        }

        protected void MoveNextInternal()
        {
            for (int i = 0; i < 10; i++)
            {
                currentPosition++;
                if (currentPosition >= cellCount)
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
