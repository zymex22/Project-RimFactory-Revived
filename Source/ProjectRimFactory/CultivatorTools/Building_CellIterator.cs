using RimWorld;
using UnityEngine;
using Verse;


namespace ProjectRimFactory.CultivatorTools
{
    public abstract class Building_CellIterator : Building
    {
        protected int CurrentPosition;

        private CompRefuelable compRefuelable;
        private CompPowerTrader compPowerTrader;
        protected CultivatorDefModExtension CultivatorDefModExtension;

        private bool Fueled => compRefuelable?.HasFuel ?? true;
        private bool Powered => compPowerTrader?.PowerOn ?? true;

        protected virtual int TickRate => 250;

        protected virtual bool CellValidator(IntVec3 cell)
        {
            return cell.InBounds(Map);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref CurrentPosition, "currentNumber", 1);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            CultivatorDefModExtension = def.GetModExtension<CultivatorDefModExtension>();
            compRefuelable = GetComp<CompRefuelable>();
            compPowerTrader = GetComp<CompPowerTrader>();
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges([Current], Color.yellow);
        }

        private void DoTickerWork()
        {
            base.TickRare();
            var cell = Current;
            if (CellValidator(cell))
            {
                if (!DoIterationWork(cell)) return;
            }
            MoveNextInternal();
        }

        protected abstract bool DoIterationWork(IntVec3 cell);

        protected abstract IntVec3 Current { get; }

        protected abstract int CellCount { get; }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (Find.TickManager.TicksGame % TickRate == 0 && Powered && Fueled) DoTickerWork();
        }

        private void MoveNextInternal()
        {
            for (var i = 0; i < 10; i++)
            {
                CurrentPosition++;
                if (CurrentPosition >= CellCount) CurrentPosition = 0;
                var cell = Current;
                if (CellValidator(cell))
                {
                    break;
                }
            }
        }


    }
}
