using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public enum WorkingState
    {
        Ready,
        Working,
        Placing
    }

    public interface IProductOutput
    {
        IntVec3 OutputCell();
    }

    public abstract class Building_Base<T> : Building, IProductOutput where T : Thing
    {
        private WorkingState state;
        private static HashSet<T> workingSet = new HashSet<T>();
        protected T working;
        protected List<Thing> products = new List<Thing>();
        private float totalWorkAmount;
        private int workStartTick;
        [Unsaved]
        private Effecter progressBar;
        [Unsaved]
        protected bool showProgressBar = true;
        [Unsaved]
        protected bool placeFirstAbsorb = false;
        [Unsaved]
        protected bool readyOnStart = false;
        [Unsaved]
        protected int startCheckIntervalTicks = 30;

        private MapTickManager mapManager;

        protected WorkingState State
        {
            get { return this.state; }
            private set
            {
                if (this.state != value)
                {
                    OnChangeState(this.state, value);
                    this.state = value;
                }
            }
        }

        protected T Working => this.working;

        protected MapTickManager MapManager => this.mapManager;

        protected virtual void OnChangeState(WorkingState before, WorkingState after)
        {
        }

        protected virtual void ClearActions()
        {
            this.MapManager.RemoveAfterAction(this.Ready);
            this.MapManager.RemoveAfterAction(this.Placing);
            this.MapManager.RemoveAfterAction(this.CheckWork);
            this.MapManager.RemoveAfterAction(this.StartWork);
            this.MapManager.RemoveAfterAction(this.FinishWork);
        }

        protected virtual bool WorkingIsDespawned()
        {
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref this.state, "workingState", WorkingState.Ready);
            Scribe_Values.Look(ref this.totalWorkAmount, "totalWorkAmount", 0f);
            Scribe_Values.Look(ref this.workStartTick, "workStartTick", 0);
            Scribe_Collections.Look<Thing>(ref this.products, "products", LookMode.Reference);

            if (WorkingIsDespawned())
                Scribe_Deep.Look<T>(ref this.working, "working");
            else
                Scribe_References.Look<T>(ref this.working, "working");
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            if (this.products == null)
                this.products = new List<Thing>();
            if (this.working == null && this.State == WorkingState.Working)
                this.ForceReady();
            if (this.products.Count == 0 && this.State == WorkingState.Placing)
                this.ForceReady();
        }

        protected static bool InWorking(T thing)
        {
            return workingSet.Contains(thing);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.mapManager = map.GetComponent<MapTickManager>();

            if (readyOnStart)
            {
                this.State = WorkingState.Ready;
                this.Reset();
                MapManager.AfterAction(Rand.Range(0, this.startCheckIntervalTicks), this.Ready);
            }
            else
            {
                if (this.State == WorkingState.Ready)
                {
                    MapManager.AfterAction(Rand.Range(0, this.startCheckIntervalTicks), this.Ready);
                }
                else if (this.State == WorkingState.Working)
                {
                    MapManager.NextAction(this.StartWork);
                }
                else if (this.State == WorkingState.Placing)
                {
                    MapManager.NextAction(this.Placing);
                }
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.Reset();
            this.ClearActions();
            base.DeSpawn(mode);
        }

        protected virtual bool IsActive()
        {
            if (this.Destroyed || !this.Spawned)
            {
                return false;
            }

            return true;
        }

        protected virtual void Reset()
        {
            if (this.State != WorkingState.Ready)
            {
                this.products.ForEach(t =>
                {
                    if (!t.Spawned)
                    {
                        GenPlace.TryPlaceThing(t, this.Position, this.Map, ThingPlaceMode.Near);
                    }
                });
            }
            this.CleanupWorkingEffect();
            this.State = WorkingState.Ready;
            this.totalWorkAmount = 0;
            this.workStartTick = 0;
            Option(this.working).ForEach(h => workingSet.Remove(h));
            this.working = null;
            this.products.Clear();
        }

        protected void ForceReady()
        {
            this.Reset();
            this.ClearActions();
            MapManager.NextAction(this.Ready);
        }

        protected virtual void CleanupWorkingEffect()
        {
            Option(this.progressBar).ForEach(e => e.Cleanup());
            this.progressBar = null;
        }

        protected virtual void CreateWorkingEffect()
        {
            this.CleanupWorkingEffect();
            if (this.working.Spawned && this.showProgressBar)
            {
                Option(ProgressBarTarget()).ForEach(t =>
                {
                    this.progressBar = DefDatabase<EffecterDef>.GetNamed("AutoMachineTool_Effect_ProgressBar").Spawn();
                    this.progressBar.EffectTick(ProgressBarTarget(), TargetInfo.Invalid);
                    ((MoteProgressBar2)((SubEffecter_ProgressBar)progressBar.children[0]).mote).progressGetter = () => (this.CurrentWorkAmount / this.totalWorkAmount);
                });
            }
        }

        protected virtual TargetInfo ProgressBarTarget()
        {
            return this.working;
        }

        protected virtual void Ready()
        {
            if (this.State != WorkingState.Ready || !this.Spawned)
            {
                return;
            }
            if (!this.IsActive())
            {
                this.Reset();
                MapManager.AfterAction(30, Ready);
                return;
            }

            if (this.TryStartWorking(out this.working, out this.totalWorkAmount))
            {
                this.State = WorkingState.Working;
                this.workStartTick = Find.TickManager.TicksAbs;
                MapManager.NextAction(this.StartWork);
            }
            else
            {
                MapManager.AfterAction(this.startCheckIntervalTicks, Ready);
            }
        }

        private int CalcRemainTick()
        {
            if (float.IsInfinity(this.totalWorkAmount))
            {
                return int.MaxValue;
            }
            return Mathf.Max(1, Mathf.CeilToInt((this.totalWorkAmount - this.CurrentWorkAmount) / this.WorkAmountPerTick));
        }

        protected float CurrentWorkAmount => (Find.TickManager.TicksAbs - this.workStartTick) * WorkAmountPerTick;

        protected float WorkLeft => this.totalWorkAmount - this.CurrentWorkAmount;

        protected virtual void StartWork()
        {
            if (this.State != WorkingState.Working || !this.Spawned)
            {
                return;
            }
            if (!this.IsActive())
            {
                this.ForceReady();
                return;
            }
            CreateWorkingEffect();
            MapManager.AfterAction(30, this.CheckWork);
            if (!float.IsInfinity(this.totalWorkAmount))
            {
                MapManager.AfterAction(this.CalcRemainTick(), this.FinishWork);
            }
        }

        protected void ForceStartWork(T working, float workAmount)
        {
            this.Reset();
            this.ClearActions();

            this.State = WorkingState.Working;
            this.working = working;
            this.totalWorkAmount = workAmount;
            this.workStartTick = Find.TickManager.TicksAbs;
            MapManager.NextAction(StartWork);
        }

        protected virtual void CheckWork()
        {
            if (this.State != WorkingState.Working || !this.Spawned)
            {
                return;
            }
            if (!this.IsActive())
            {
                this.ForceReady();
                return;
            }
            if (this.WorkInterruption(this.working))
            {
                this.ForceReady();
                return;
            }
            if (this.CurrentWorkAmount >= this.totalWorkAmount)
            {
                // 作業中に電力が変更されて終わってしまった場合、次TickでFinish呼び出し.
                MapManager.NextAction(this.FinishWork);
            }
            else
            {
                MapManager.AfterAction(30, this.CheckWork);
            }
        }

        protected virtual void FinishWork()
        {
            if (this.State != WorkingState.Working || !this.Spawned)
            {
                return;
            }
            MapManager.RemoveAfterAction(this.CheckWork);
            MapManager.RemoveAfterAction(this.FinishWork);
            if (!this.IsActive())
            {
                this.ForceReady();
                return;
            }
            if (this.WorkInterruption(this.working))
            {
                this.ForceReady();
                return;
            }
            if (this.FinishWorking(this.working, out this.products))
            {
                this.State = WorkingState.Placing;
                this.CleanupWorkingEffect();
                this.working = null;
                if(this.products == null || this.products.Count == 0)
                {
                    this.Reset();
                    MapManager.NextAction(this.Ready);
                }
                else
                {
                    MapManager.NextAction(this.Placing);
                }
            }
            else
            {
                this.Reset();
                MapManager.NextAction(this.Ready);
            }
        }

        protected virtual void Placing()
        {
            if (this.State != WorkingState.Placing || !this.Spawned)
            {
                return;
            }
            if (!this.IsActive())
            {
                this.ForceReady();
                return;
            }

            if (this.PlaceProduct(ref this.products))
            {
                this.State = WorkingState.Ready;
                this.Reset();
                MapManager.NextAction(Ready);
            }
            else
            {
                MapManager.AfterAction(30, this.Placing);
            }
        }

        protected abstract float WorkAmountPerTick { get; }

        protected abstract bool WorkInterruption(T working);

        protected abstract bool TryStartWorking(out T target, out float workAmount);

        protected abstract bool FinishWorking(T working, out List<Thing> products);

        protected bool forcePlace = true;

        protected virtual bool PlaceProduct(ref List<Thing> products)
        {
            products = products.Aggregate(new List<Thing>(), (total, target) => {
                var conveyor = OutputCell().GetThingList(this.Map)
                    .OfType<IBeltConbeyorLinkable>() // return any IBeltConbeyorLinkable
                    .FirstOrDefault(b => !b.IsUnderground);
                if (conveyor != null)
                {
                    // コンベアがある場合、そっちに流す.
                    if (conveyor.ReceiveThing(false, target))
                    {
                        return total;
                    }
                }
                else
                {
                    // ない場合は適当に流す.
                    if (target.Spawned) target.DeSpawn();
                    if (PlaceItem(target, OutputCell(), false, this.Map, this.placeFirstAbsorb))
                    {
                        return total;
                    }
                    if (this.forcePlace)
                    {
                        GenPlace.TryPlaceThing(target, OutputCell(), this.Map, ThingPlaceMode.Near);
                        return total;
                    }
                }
                return total.Append(target);
            });
            return this.products.Count() == 0;
        }

        public virtual IntVec3 OutputCell()
        {
            return FacingCell(this.Position, this.def.Size, this.Rotation.Opposite);
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            switch (this.State)
            {
                case WorkingState.Working:
                    if (float.IsInfinity(this.totalWorkAmount))
                    {
                        msg += "PRF.AutoMachineTool.StatWorkingNotParam".Translate();
                    }
                    else
                    {
                        msg += "PRF.AutoMachineTool.StatWorking".Translate(
                            Mathf.RoundToInt(Math.Min(this.CurrentWorkAmount, this.totalWorkAmount)),
                            Mathf.RoundToInt(this.totalWorkAmount),
                            Mathf.RoundToInt(Mathf.Clamp01(this.CurrentWorkAmount / this.totalWorkAmount) * 100));
                    }
                    break;
                case WorkingState.Ready:
                    msg += "PRF.AutoMachineTool.StatReady".Translate();
                    break;
                case WorkingState.Placing:
                    msg += "PRF.AutoMachineTool.StatPlacing".Translate(this.products.Count);
                    break;
                default:
                    msg += this.State.ToString();
                    break;
            }
            return msg;
        }

        public void NortifyReceivable()
        {
            if (this.State == WorkingState.Placing && this.Spawned)
            {
                if (!this.MapManager.IsExecutingThisTick(this.Placing))
                {
                    this.MapManager.RemoveAfterAction(this.Placing);
                    this.MapManager.NextAction(this.Placing);
                }
            }
        }

        protected List<Thing> CreateThings(ThingDef def, int count)
        {
            var quot = count / def.stackLimit;
            var remain = count % def.stackLimit;
            return Enumerable.Range(0, quot + 1)
                .Select((c, i) => i == quot ? remain : def.stackLimit)
                .Select(c =>
                {
                    Thing p = ThingMaker.MakeThing(def, null);
                    p.stackCount = c;
                    return p;
                }).ToList();
        }
    }
}
