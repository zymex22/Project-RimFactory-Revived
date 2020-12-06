using System;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public interface IRange
    {
        IntVec3 Position { get; }
        IntVec2 Size { get; }
        Rot4 Rotation { get; }
        int GetRange();
        IEnumerable<IntVec3> GetAllTargetCells();
    }

    public abstract class Building_BaseRange<T> : Building_BaseLimitation<T>, IRange, IPowerSupplyMachineHolder,
        IPowerSupplyMachine where T : Thing
    {
        private const int CACHE_CLEAR_INTERVAL_TICKS = 180;

        [Unsaved] private HashSet<IntVec3> allTargetCellsCache;

        private bool glow;

        [Unsaved] private bool nextTargetCells;

        [Unsaved] private List<List<IntVec3>> splittedTargetCells;

        [Unsaved] private int splittedTargetCellsIndex;

        private float supplyPowerForRange;

        [Unsaved] protected int targetEnumrationCount = 100;

        protected ModExtension_WorkIORange RangeExtension => def.GetModExtension<ModExtension_WorkIORange>();

        private bool SplitTargetCells =>
            targetEnumrationCount > 0 && GetAllTargetCells().Count() > targetEnumrationCount;

        public override int MinPowerForRange => RangeExtension.minPower;
        public override int MaxPowerForRange => RangeExtension.maxPower;

        public override bool Glowable => false;

        public override bool Glow
        {
            get => glow;
            set
            {
                if (glow != value)
                {
                    glow = value;
                    ChangeGlow();
                }
            }
        }

        public override bool SpeedSetting => true;

        public override bool RangeSetting => true;

        public override float RangeInterval => 500;

        public override float SupplyPowerForRange
        {
            get => supplyPowerForRange;
            set
            {
                if (supplyPowerForRange != value)
                {
                    supplyPowerForRange = value;
                    ChangeGlow();
                    allTargetCellsCache = null;
                }

                RefreshPowerStatus();
            }
        }

        public override void RefreshPowerStatus()
        {
            if (-SupplyPowerForRange - SupplyPowerForSpeed - (Glowable && Glow ? 2000 : 0) !=
                this.TryGetComp<CompPowerTrader>().PowerOutput)
                powerComp.PowerOutput = -SupplyPowerForRange - SupplyPowerForSpeed - (Glowable && Glow ? 2000 : 0);
        }

        public IntVec2 Size => def.Size;

        public IEnumerable<IntVec3> GetAllTargetCells()
        {
            CacheTargetCells();
            return allTargetCellsCache;
        }

        public int GetRange()
        {
            return RangeExtension.TargetCellResolver.GetRange(SupplyPowerForRange);
        }

        private void CacheTargetCells()
        {
            if (allTargetCellsCache == null)
            {
                allTargetCellsCache = RangeExtension.TargetCellResolver
                    .GetRangeCells(def, Position, RotatedSize, Map, Rotation, GetRange()).ToHashSet();
                if (targetEnumrationCount > 0)
                    splittedTargetCells = allTargetCellsCache.ToList().Grouped(targetEnumrationCount);
            }
        }

        private List<IntVec3> GetCurrentSplittedTargetCells()
        {
            CacheTargetCells();
            if (splittedTargetCellsIndex >= splittedTargetCells.Count) splittedTargetCellsIndex = 0;
            return splittedTargetCells[splittedTargetCellsIndex];
        }

        private void NextSplittedTargetCells()
        {
            splittedTargetCellsIndex++;
            if (splittedTargetCellsIndex >= splittedTargetCells.Count) splittedTargetCellsIndex = 0;
        }

        private void ClearAllTargetCellCache()
        {
            if (IsActive()) allTargetCellsCache = null;
            if (Spawned)
                if (RangeExtension.TargetCellResolver.NeedClearingCache)
                    MapManager.AfterAction(CACHE_CLEAR_INTERVAL_TICKS, ClearAllTargetCellCache);
        }

        protected override void ClearActions()
        {
            base.ClearActions();
            MapManager.RemoveAfterAction(ClearAllTargetCellCache);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref supplyPowerForRange, "supplyPowerForRange", MinPowerForRange);
            Scribe_Values.Look(ref glow, "glow");
        }

        protected override void ReloadSettings(object sender, EventArgs e)
        {
            if (SupplyPowerForRange < MinPowerForRange) SupplyPowerForRange = MinPowerForRange;
            if (SupplyPowerForRange > MaxPowerForRange) SupplyPowerForRange = MaxPowerForRange;
        }

        private void ChangeGlow()
        {
            Option(this.TryGetComp<CompGlower>()).ForEach(glower =>
            {
                var tmp = this.TryGetComp<CompFlickable>().SwitchIsOn;
                glower.Props.glowRadius = Glow ? (GetRange() + 2f) * 2f : 0;
                glower.Props.overlightRadius = Glow ? GetRange() + 2.1f : 0;
                this.TryGetComp<CompFlickable>().SwitchIsOn = !tmp;
                // this.TryGetComp<CompPowerTrader>().PowerOn = !tmp;
                glower.UpdateLit(Map);
                this.TryGetComp<CompFlickable>().SwitchIsOn = tmp;
                // this.TryGetComp<CompPowerTrader>().PowerOn = tmp;
                glower.UpdateLit(Map);
            });
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad) SupplyPowerForRange = MinPowerForRange;
            Option(this.TryGetComp<CompGlower>()).ForEach(g =>
            {
                var newProp = new CompProperties_Glower();
                newProp.compClass = g.Props.compClass;
                newProp.glowColor = g.Props.glowColor;
                newProp.glowRadius = g.Props.glowRadius;
                newProp.overlightRadius = g.Props.overlightRadius;
                g.props = newProp;
            });
            allTargetCellsCache = null;
            ChangeGlow();
            if (RangeExtension.TargetCellResolver.NeedClearingCache)
                MapManager.AfterAction(CACHE_CLEAR_INTERVAL_TICKS, ClearAllTargetCellCache);
        }

        protected virtual IEnumerable<IntVec3> GetTargetCells()
        {
            if (SplitTargetCells)
            {
                nextTargetCells = true;
                return GetCurrentSplittedTargetCells();
            }

            return GetAllTargetCells();
        }

#if DEBUG
        public override void Draw()
        {
            base.Draw();

            if (Find.Selector.FirstSelectedObject == this && SplitTargetCells)
                GenDraw.DrawFieldEdges(GetCurrentSplittedTargetCells(), Color.red);
        }
#endif

        protected override void Ready()
        {
            base.Ready();
            if (State == WorkingState.Ready && SplitTargetCells && nextTargetCells)
            {
                NextSplittedTargetCells();
                nextTargetCells = false;
            }
        }
    }
}