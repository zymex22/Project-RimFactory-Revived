using ProjectRimFactory.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
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

    public abstract class Building_Base<T> : PRF_Building, IProductOutput where T : Thing
    {
        private WorkingState state;
        protected T working;
        protected List<Thing> Products = [];
        private float totalWorkAmount;
        private int workStartTick;
        [Unsaved]
        private Effecter progressBar;
        [Unsaved]
        protected bool ShowProgressBar = true;
        [Unsaved]
        protected bool ReadyOnStart = false;
        [Unsaved] 
        private const int StartCheckIntervalTicks = 30;

        protected CompOutputAdjustable CompOutputAdjustable;


        public override void PostMake()
        {
            base.PostMake();
            CompOutputAdjustable = GetComp<CompOutputAdjustable>();
        }

        protected MapTickManager MapManager { get; private set; }

        public override Thing GetThingBy(Func<Thing, bool> optionalValidator = null)
        {
            foreach (var thing in Products)
            {
                if (optionalValidator is null || optionalValidator(thing))
                {
                    Products.Remove(thing);
                    return thing;
                }
            }
            return null;
        }

        protected WorkingState State
        {
            get => state;
            set => state = value;
        }

        protected T Working => working;
        
        protected virtual void ClearActions()
        {
            MapManager.RemoveAfterAction(Ready);
            MapManager.RemoveAfterAction(Placing);
            MapManager.RemoveAfterAction(CheckWork);
            MapManager.RemoveAfterAction(StartWork);
            MapManager.RemoveAfterAction(FinishWork);
        }
        /// <summary>
        /// The mode to use for saving/loading `working` for this class.
        ///   Use LookMode.Deep if no one else saves the object (e.g. if
        ///   the object is not spawned). Use LookMode.Reference if some
        ///   other source also saves the item - a spawned item is saved
        ///   by the map.
        /// </summary>
        protected virtual LookMode WorkingLookMode => LookMode.Reference;

        protected virtual LookMode ProductsLookMode => LookMode.Deep;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref state, "workingState", WorkingState.Ready);
            Scribe_Values.Look(ref totalWorkAmount, "totalWorkAmount", 0f);
            Scribe_Values.Look(ref workStartTick, "workStartTick", 0);
            Scribe_Collections.Look<Thing>(ref Products, "products", ProductsLookMode);
            if (WorkingLookMode == LookMode.Deep)
            {
                Scribe_Deep.Look<T>(ref working, "working");
            }
            else if (WorkingLookMode == LookMode.Reference)
            {
                Scribe_References.Look<T>(ref working, "working");
            }
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            Products ??= [];
            if (working == null && State == WorkingState.Working)
                ForceReady();
            if (Products.Count == 0 && State == WorkingState.Placing)
                ForceReady();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            MapManager = map.GetComponent<MapTickManager>();
            CompOutputAdjustable = GetComp<CompOutputAdjustable>();
            if (ReadyOnStart) // No check for respawning after load?
            {
                State = WorkingState.Ready;
                Reset();
                MapManager.AfterAction(Rand.Range(0, StartCheckIntervalTicks), Ready);
            }
            else
            {
                switch (State)
                {
                    case WorkingState.Ready:
                        MapManager.AfterAction(Rand.Range(0, StartCheckIntervalTicks), Ready);
                        break;
                    case WorkingState.Working:
                        MapManager.NextAction(StartWork);
                        break;
                    case WorkingState.Placing:
                        MapManager.NextAction(Placing);
                        break;
                }
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (!GravshipUtility.generatingGravship)
            {
                Reset();
                ClearActions();
            }
            
            base.DeSpawn(mode);
        }

        protected virtual bool IsActive()
        {
            if (Destroyed || !Spawned)
            {
                return false;
            }

            //Check if Output is in Bounds
            return OutputCell().InBounds(Map);
        }

        protected virtual void Reset()
        {
            if (State != WorkingState.Ready)
            {
                Products.ForEach(t =>
                {
                    if (t is null || t.Destroyed) return;
                    if (t.Spawned)
                    {
                        t.DeSpawn();
                    }
                    GenPlace.TryPlaceThing(t, Position, Map, ThingPlaceMode.Near);
                });
            }
            CleanupWorkingEffect();
            State = WorkingState.Ready;
            totalWorkAmount = 0;
            workStartTick = 0;
            working = null;
            Products.Clear();
        }

        protected void ForceReady()
        {
            Reset();
            ClearActions();
            MapManager.NextAction(Ready);
        }

        protected virtual void CleanupWorkingEffect()
        {
            Option(progressBar).ForEach(e => e.Cleanup());
            progressBar = null;
        }

        protected virtual void CreateWorkingEffect()
        {
            CleanupWorkingEffect();
            if (ShowProgressBar)
            {
                Option(ProgressBarTarget()).ForEach(t =>
                {
                    progressBar = DefDatabase<EffecterDef>.GetNamed("AutoMachineTool_Effect_ProgressBar").Spawn();
                    progressBar.EffectTick(ProgressBarTarget(), TargetInfo.Invalid);
                    ((MoteProgressBar2)((SubEffecter_ProgressBar)progressBar.children[0]).mote).progressGetter = () => (CurrentWorkAmount / totalWorkAmount);
                });
            }
        }

        protected virtual TargetInfo ProgressBarTarget()
        {
            if (working.Spawned)
            {
                return working;
            }
            return this;
        }

        protected virtual void Ready()
        {
            if (State != WorkingState.Ready || !Spawned)
            {
                return;
            }
            if (!IsActive())
            {
                Reset();
                MapManager.AfterAction(30, Ready);
                return;
            }

            if (TryStartWorking(out working, out totalWorkAmount))
            {
                State = WorkingState.Working;
                workStartTick = Find.TickManager.TicksAbs;
                MapManager.NextAction(StartWork);
            }
            else
            {
                MapManager.AfterAction(StartCheckIntervalTicks, Ready);
            }
        }

        private int CalcRemainTick()
        {
            if (float.IsInfinity(totalWorkAmount))
            {
                return int.MaxValue;
            }
            return Mathf.Max(1, Mathf.CeilToInt((totalWorkAmount - CurrentWorkAmount) / WorkAmountPerTick));
        }

        private float CurrentWorkAmount => (Find.TickManager.TicksAbs - workStartTick) * WorkAmountPerTick;

        protected float WorkLeft => totalWorkAmount - CurrentWorkAmount;

        protected virtual void StartWork()
        {
            if (State != WorkingState.Working || !Spawned)
            {
                return;
            }
            if (!IsActive())
            {
                ForceReady();
                return;
            }
            CreateWorkingEffect();
            MapManager.AfterAction(30, CheckWork);
            if (!float.IsInfinity(totalWorkAmount))
            {
                MapManager.AfterAction(CalcRemainTick(), FinishWork);
            }
        }

        protected virtual void ForceStartWork(T working, float workAmount)
        {
            Reset();
            ClearActions();

            State = WorkingState.Working;
            this.working = working;
            totalWorkAmount = workAmount;
            workStartTick = Find.TickManager.TicksAbs;
            MapManager.NextAction(StartWork);
        }

        protected virtual void CheckWork()
        {
            if (State != WorkingState.Working || !Spawned)
            {
                return;
            }
            if (!IsActive())
            {
                ForceReady();
                return;
            }
            if (WorkInterruption(working))
            {
                ForceReady();
                return;
            }
            if (CurrentWorkAmount >= totalWorkAmount)
            {
                // 作業中に電力が変更されて終わってしまった場合、次TickでFinish呼び出し.
                // If the power is changed during work and it ends, 
                //     call Finish with the next tick.
                MapManager.NextAction(FinishWork);
            }
            else
            {
                MapManager.AfterAction(30, CheckWork);
            }
        }

        protected virtual void FinishWork()
        {
            if (State != WorkingState.Working || !Spawned)
            {
                return;
            }
            MapManager.RemoveAfterAction(CheckWork);
            MapManager.RemoveAfterAction(FinishWork);
            if (!IsActive())
            {
                ForceReady();
                return;
            }
            if (WorkInterruption(working))
            {
                ForceReady();
                return;
            }
            if (FinishWorking(working, out Products))
            {
                State = WorkingState.Placing;
                CleanupWorkingEffect();
                working = null;
                if (Products == null || Products.Count == 0)
                {
                    Reset();
                    MapManager.NextAction(Ready);
                }
                else
                {
                    MapManager.NextAction(Placing);
                }
            }
            else
            {
                Reset();
                MapManager.NextAction(Ready);
            }
        }

        protected virtual void Placing()
        {
            if (State != WorkingState.Placing || !Spawned)
            {
                return;
            }
            if (!IsActive())
            {
                ForceReady();
                return;
            }

            if (PlaceProduct(ref Products))
            {
                State = WorkingState.Ready;
                Reset();
                MapManager.NextAction(Ready);
            }
            else
            {
                // If we are still Placing, try again in 30
                if (State == WorkingState.Placing) MapManager.AfterAction(30, Placing);
            }
        }

        protected abstract float WorkAmountPerTick { get; }

        protected abstract bool WorkInterruption(T working);

        protected abstract bool TryStartWorking(out T target, out float workAmount);

        protected abstract bool FinishWorking(T working, out List<Thing> outputProducts);

        protected bool ForcePlace = true;

        protected virtual bool PlaceProduct(ref List<Thing> things)
        {
            // Use Aggregate() to attempt to place each item in products
            //   Any unplaced products accumulate in the new List<Thing>,
            //   and stay in products.
            // Is there any reason this uses `ref List<Thing> products`
            //   instead of `this.products`?
            things = things.Aggregate(new List<Thing>(), (total, target) =>
            {
                if (target.Spawned) target.DeSpawn();
                if (this.PRFTryPlaceThing(target, OutputCell(), Map, ForcePlace))
                {
                    return total;
                }
                return total.Append(target);
            });
            // if there are any left in products, we didn't place them all:
            return !this.Products.Any();
        }

        public override IntVec3 OutputCell()
        {
            return FacingCell(Position, def.Size, Rotation.Opposite);
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            switch (State)
            {
                case WorkingState.Working:
                    if (float.IsInfinity(totalWorkAmount))
                    {
                        msg += "PRF.AutoMachineTool.StatWorkingNotParam".Translate();
                    }
                    else
                    {
                        msg += "PRF.AutoMachineTool.StatWorking".Translate(
                            Mathf.RoundToInt(Math.Min(CurrentWorkAmount, totalWorkAmount)),
                            Mathf.RoundToInt(totalWorkAmount),
                            Mathf.RoundToInt(Mathf.Clamp01(CurrentWorkAmount / totalWorkAmount) * 100));
                    }
                    break;
                case WorkingState.Ready:
                    msg += "PRF.AutoMachineTool.StatReady".Translate();
                    break;
                case WorkingState.Placing:
                    if (Products.Count != 1)
                        msg += "PRF.AutoMachineTool.StatPlacing".Translate(Products.Count);
                    else
                        msg += "PRF.AutoMachineTool.PlacingSingle".Translate(Products[0].Label);
                    break;
                default:
                    msg += State.ToString();
                    break;
            }
            return msg;
        }

        public void NortifyReceivable()
        {
            if (State != WorkingState.Placing || !Spawned) return;
            if (MapManager.IsExecutingThisTick(Placing)) return;
            MapManager.RemoveAfterAction(Placing);
            MapManager.NextAction(Placing);
        }
    }
}
