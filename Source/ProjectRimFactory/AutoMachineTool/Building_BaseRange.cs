using ProjectRimFactory.Common;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public interface IRange
    {
        int GetRange();
        IntVec3 Position { get; }
        IntVec2 Size { get; }
        Rot4 Rotation { get; }
        IEnumerable<IntVec3> GetAllTargetCells();
    }

    public abstract class Building_BaseRange<T> : Building_BaseLimitation<T>, IRange, IPowerSupplyMachineHolder where T : Thing
    {
        // public override int MinPowerForRange => this.RangeExtension.minPower;
        // public override int MaxPowerForRange => this.RangeExtension.maxPower;

        public override bool Glowable => false;

        private bool glow = false;
        public override bool Glow
        {
            get => glow;
            set
            {
                if (glow == value) return;
                glow = value;
                ChangeGlow();
            }
        }

        public IntVec2 Size => def.Size;

        private float supplyPowerForRange;

        [Unsaved]
        protected int targetEnumrationCount = 100;

        [Unsaved]
        private bool nextTargetCells = false;

        [Unsaved]
        private HashSet<IntVec3> allTargetCellsCache;

        [Unsaved]
        private List<List<IntVec3>> splittedTargetCells;

        [Unsaved]
        private int splittedTargetCellsIndex = 0;

        public IEnumerable<IntVec3> GetAllTargetCells()
        {
            CacheTargetCells();
            return PowerWorkSetting.GetRangeCells()?.ToHashSet() ?? [];
        }

        private void CacheTargetCells()
        {
            if (allTargetCellsCache != null) return;
            allTargetCellsCache = PowerWorkSetting.GetRangeCells()?.ToHashSet() ?? [];
            if (targetEnumrationCount > 0)
            {
                splittedTargetCells = allTargetCellsCache.ToList().Grouped(targetEnumrationCount);
            }
        }

        private List<IntVec3> GetCurrentSplittedTargetCells()
        {
            CacheTargetCells();
            if (splittedTargetCellsIndex >= splittedTargetCells.Count)
            {
                splittedTargetCellsIndex = 0;
            }
            return splittedTargetCells[splittedTargetCellsIndex];
        }

        private void NextSplittedTargetCells()
        {
            splittedTargetCellsIndex++;
            if (splittedTargetCellsIndex >= splittedTargetCells.Count)
            {
                splittedTargetCellsIndex = 0;
            }
        }

        private void ClearAllTargetCellCache()
        {
            if (IsActive())
            {
                allTargetCellsCache = null;
            }
            if (Spawned)
            {
            }
        }

        protected override void ClearActions()
        {
            base.ClearActions();
            MapManager.RemoveAfterAction(ClearAllTargetCellCache);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref supplyPowerForRange, "supplyPowerForRange", 0);
            Scribe_Values.Look<bool>(ref glow, "glow", false);
        }

        private void ChangeGlow()
        {
            Option(this.TryGetComp<CompGlower>()).ForEach(glower =>
            {
                var tmp = this.TryGetComp<CompFlickable>().SwitchIsOn;
                glower.Props.glowRadius = Glow ? (GetRange() + 2f) * 2f : 0;
                glower.Props.overlightRadius = Glow ? (GetRange() + 2.1f) : 0;
                this.TryGetComp<CompFlickable>().SwitchIsOn = !tmp;
                glower.UpdateLit(Map);
                this.TryGetComp<CompFlickable>().SwitchIsOn = tmp;
                glower.UpdateLit(Map);
            });
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Option(this.TryGetComp<CompGlower>()).ForEach(g =>
            {
                var newProp = new CompProperties_Glower
                {
                    compClass = g.Props.compClass,
                    glowColor = g.Props.glowColor,
                    glowRadius = g.Props.glowRadius,
                    overlightRadius = g.Props.overlightRadius
                };
                g.props = newProp;
            });
            allTargetCellsCache = null;
            ChangeGlow();
        }

        public int GetRange()
        {
            Log.ErrorOnce("ERROR Sniper needs to fix C# Building_BaseRange GetRange()", 684546864);
            return 0;
        }

        protected virtual IEnumerable<IntVec3> GetTargetCells()
        {
            if (!SplitTargetCells) return GetAllTargetCells();
            nextTargetCells = true;
            return GetCurrentSplittedTargetCells();

        }

#if DEBUG
        public override void Draw()
        {
            base.Draw();

            if (Find.Selector.FirstSelectedObject == this && this.SplitTargetCells)
            {
                GenDraw.DrawFieldEdges(this.GetCurrentSplittedTargetCells(), Color.red);
            }
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

        private bool SplitTargetCells => targetEnumrationCount > 0 && GetAllTargetCells().Count() > targetEnumrationCount;
    }
}
