using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;
using ProjectRimFactory.Common.HarmonyPatches;

namespace ProjectRimFactory.AutoMachineTool
{
    public enum DirectionPriority
    {
        VeryHigh = 4,
        High = 3,
        Normal = 2,
        Low = 1
    }

    public static class DirectionPriorityExtension
    {
        public static string ToText(this DirectionPriority pri)
        {
            return ("PRF.AutoMachineTool.Conveyor.DirectionPriority." + pri.ToString()).Translate();
        }
    }

    class Building_BeltConveyor : Building_BaseMachine<Thing>, IBeltConbeyorLinkable, IHideItem, IHideRightClickMenu, IForbidPawnOutputItem
    {
        public Building_BeltConveyor()
        {
            base.setInitialMinPower = false;
        }

        private Rot4 dest = default(Rot4);
        private Dictionary<Rot4, ThingFilter> filters = new Dictionary<Rot4, ThingFilter>();
        private Dictionary<Rot4, DirectionPriority> priorities = new Rot4[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }.ToDictionary(d => d, _ => DirectionPriority.Normal);
        public static float supplyPower = 10f;

        [Unsaved]
        private int round = 0;
        [Unsaved]
        private List<Rot4> outputRot = new List<Rot4>();

        [Unsaved]
        private bool stuck = false;

        public IEnumerable<Rot4> OutputRots => this.outputRot;

        private ModExtension_Conveyor Extension => this.def.GetModExtension<ModExtension_Conveyor>();

        public override float SupplyPowerForSpeed
        {
            get
            {
                return supplyPower;
            }

            set
            {
                supplyPower = value;
                this.SetPower();
            }
        }

        public Dictionary<Rot4, ThingFilter> Filters { get => this.filters; }
        public Dictionary<Rot4, DirectionPriority> Priorities { get => this.priorities; }

        public bool IsStuck => this.stuck;

        public bool IsUnderground { get => Option(this.Extension).Fold(false)(x => x.underground); }

        public bool HideItems => !this.IsUnderground && this.State != WorkingState.Ready;

        public bool HideRightClickMenus => !this.IsUnderground && this.State != WorkingState.Ready;

        public bool ForbidPawnOutput => !this.IsUnderground && this.State != WorkingState.Ready;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref supplyPower, "supplyPower", 10f);
            Scribe_Values.Look(ref this.dest, "dest");
            Scribe_Collections.Look(ref this.filters, "filters", LookMode.Value, LookMode.Deep);
            if (this.filters == null)
            {
                this.filters = new Dictionary<Rot4, ThingFilter>();
            }
            Scribe_Collections.Look(ref this.priorities, "priorities", LookMode.Value, LookMode.Value);
            if (this.priorities == null)
            {
                this.priorities = new Rot4[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }.ToDictionary(d => d, _ => DirectionPriority.Normal);
            }
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            
            this.FilterSetting();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.showProgressBar = false;

            if (!respawningAfterLoad)
            {
                var conveyors = LinkTargetConveyor();
                if (conveyors.Count == 0)
                {
                    this.FilterSetting();
                }
                else
                {
                    conveyors.ForEach(x =>
                    {
                        x.Link(this);
                        this.Link(x);
                    });
                }
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var targets = LinkTargetConveyor();
            base.DeSpawn();

            targets.ForEach(x => x.Unlink(this));
        }

        protected override void Reset()
        {
            if (this.State != WorkingState.Ready)
            {
                this.FilterSetting();
                if (this.working != null)
                {
                    this.products.Add(this.working);
                }
            }
            base.Reset();
        }
        
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();

            if (this.IsUnderground && !OverlayDrawHandler_UGConveyor.ShouldDraw)
            {
                // 地下コンベアの場合には表示しない.
                return;
            }

            if (this.State != WorkingState.Ready && Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                var p = CarryPosition();
                Vector2 result = Find.Camera.WorldToScreenPoint(p + new Vector3(0, 0, -0.4f)) / Prefs.UIScale;
                result.y = (float)UI.screenHeight - result.y;
                GenMapUI.DrawThingLabel(result, this.CarryingThing().stackCount.ToStringCached(), GenMapUI.DefaultThingLabelColor);
            }
        }

        public override void Draw()
        {
            if (this.IsUnderground && !OverlayDrawHandler_UGConveyor.ShouldDraw)
            {
                // 地下コンベアの場合には表示しない.
                return;
            }
            base.Draw();
            if (this.State != WorkingState.Ready)
            {
                var p = CarryPosition();
                this.CarryingThing().DrawAt(p);
            }
        }

        private Thing CarryingThing()
        {
            if (this.State == WorkingState.Working)
            {
                return this.Working;
            }
            else if (this.State == WorkingState.Placing)
            {
                return this.products[0];
            }
            return null;
        }

        private Vector3 CarryPosition()
        {
            var workLeft = this.stuck ? Mathf.Clamp(Mathf.Abs(this.WorkLeft), 0f, 0.5f) : Mathf.Clamp01(this.WorkLeft);
            return (this.dest.FacingCell.ToVector3() * (1f - workLeft)) + this.Position.ToVector3() + new Vector3(0.5f, 10f, 0.5f);
        }
        
        public override bool CanStackWith(Thing other)
        {
            return base.CanStackWith(other) && this.State == WorkingState.Ready;
        }

        public bool ReceiveThing(bool underground, Thing t)
        {
            return ReceiveThing(underground, t, Destination(t, true));
        }

        private bool ReceiveThing(bool underground, Thing t, Rot4 rot)
        {
            if (!this.ReceivableNow(underground, t))
                return false;
            if (this.State == WorkingState.Ready)
            {
                if (t.Spawned && this.IsUnderground) t.DeSpawn();
                t.Position = this.Position;
                this.dest = rot;
                this.ForceStartWork(t, 1f);
                return true;
            }
            else
            {
                var target = this.State == WorkingState.Working ? this.Working : this.products[0];
                return target.TryAbsorbStack(t, true);
            }
        }

        private Rot4 Destination(Thing t, bool doRotate)
        {
            var conveyors = this.OutputBeltConveyor();
            var allowed = this.filters
                .Where(f => f.Value.Allows(t.def)).Select(f => f.Key)
                .ToList();
            var placeable = allowed.Where(r => conveyors.Where(l => l.Position == this.Position + r.FacingCell).FirstOption().Select(b => b.ReceivableNow(this.IsUnderground, t) || !b.IsStuck).GetOrDefault(true))
                .ToList();

            if (placeable.Count == 0)
            {
                if(allowed.Count == 0)
                {
                    placeable = this.OutputRots.Where(r =>
                        conveyors
                            .Where(l => l.Position == this.Position + r.FacingCell)
                            .FirstOption()
                            .Select(b => b.ReceivableNow(this.IsUnderground, t) || !b.IsStuck)
                            .GetOrDefault(true))
                        .ToList();
                }
                else
                {
                    placeable = allowed;
                }
            }

            var maxPri = placeable.Select(r => this.priorities[r]).Max();
            var dests = placeable.Where(r => this.priorities[r] == maxPri).ToList();

            if (dests.Count <= this.round) this.round = 0;
            var index = this.round;
            if (doRotate) this.round++;
            return dests.ElementAt(index);
        }

        private bool SendableConveyor(Thing t, out Rot4 dir)
        {
            dir = default(Rot4);
            var result = this.filters
                .Where(f => f.Value.Allows(t.def))
                .Select(f => f.Key)
                .SelectMany(r => this.OutputBeltConveyor().Where(l => l.Position == this.Position + r.FacingCell).Select(b => new { Dir = r, Conveyor = b }))
                .Where(b => b.Conveyor.ReceivableNow(this.IsUnderground, t))
                .FirstOption();
            if (result.HasValue)
            {
                dir = result.Value.Dir;
            }
            return result.HasValue;
        }

        protected override bool PlaceProduct(ref List<Thing> products)
        {
            var thing = products[0];
            if (this.WorkInterruption(thing))
            {
                return true;
            }
            var next = this.LinkTargetConveyor().Where(o => o.Position == this.dest.FacingCell + this.Position).FirstOption();
            if (next.HasValue)
            {
                // コンベアある場合、そっちに流す.
                if (next.Value.ReceiveThing(this.IsUnderground, thing))
                {
                    NotifyAroundSender();
                    this.stuck = false;
                    return true;
                }
            }
            else
            {
                if (!this.IsUnderground && PlaceItem(thing, this.dest.FacingCell + this.Position, false, this.Map))
                {
                    NotifyAroundSender();
                    this.stuck = false;
                    return true;
                }
            }

            if (this.SendableConveyor(thing, out Rot4 dir))
            {
                // 他に流す方向があれば、やり直し.
                this.Reset();
                this.ReceiveThing(this.IsUnderground, thing, dir);
                return false;
            }
            // 配置失敗.
            this.stuck = true;
            return false;
        }

        public void Link(IBeltConbeyorLinkable link)
        {
            this.FilterSetting();
        }

        public void Unlink(IBeltConbeyorLinkable unlink)
        {
            this.FilterSetting();
            Option(this.Working).ForEach(t => this.dest = Destination(t, true));
        }

        private void FilterSetting()
        {
            Func<ThingFilter> createNew = () =>
            {
                var f = new ThingFilter();
                f.SetAllowAll(null);
                return f;
            };
            var output = this.OutputBeltConveyor();
            this.filters = Enumerable.Range(0, 4).Select(x => new Rot4(x))
                .Select(x => new { Rot = x, Pos = this.Position + x.FacingCell })
                .Where(x => output.Any(l => l.Position == x.Pos) || this.Rotation == x.Rot)
                .ToDictionary(r => r.Rot, r => this.filters.ContainsKey(r.Rot) ? this.filters[r.Rot] : createNew());
            if(this.filters.Count <= 1)
            {
                this.filters.ForEach(x => x.Value.SetAllowAll(null));
            }
            this.outputRot = this.filters.Select(x => x.Key).ToList();
        }

        private List<IBeltConbeyorLinkable> LinkTargetConveyor()
        {
            return Enumerable.Range(0, 4).Select(i => this.Position + new Rot4(i).FacingCell)
                .SelectMany(t => t.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => CanLink(this, t, this.def, t.def))
                .SelectMany(t => Option(t as IBeltConbeyorLinkable))
                .ToList();
        }

        private List<IBeltConbeyorLinkable> OutputBeltConveyor()
        {
            var links = this.LinkTargetConveyor();
            return links.Where(x =>
                    (x.Rotation.Opposite.FacingCell + x.Position == this.Position && x.Position != this.Position + this.Rotation.Opposite.FacingCell) ||
                    (x.Rotation.Opposite.FacingCell + x.Position == this.Position && links.Any(l => l.Position + l.Rotation.FacingCell == this.Position))
                )
                .ToList();
        }

        public bool Acceptable(Rot4 rot, bool underground)
        {
            return rot != this.Rotation && this.IsUnderground == underground;
        }

        public bool ReceivableNow(bool underground, Thing thing)
        {
            if(!this.IsActive() || this.IsUnderground != underground)
            {
                return false;
            }
            Func<Thing, bool> check = (t) => t.CanStackWith(thing) && t.stackCount < t.def.stackLimit;
            switch (this.State) {
                case WorkingState.Ready:
                    return true;
                case WorkingState.Working:
                    return check(this.Working);
                case WorkingState.Placing:
                    return check(this.products[0]);
                default:
                    return false;
            }
        }

        private void NotifyAroundSender()
        {
            new Rot4[] { this.Rotation.Opposite, this.Rotation.Opposite.RotateAsNew(RotationDirection.Clockwise), this.Rotation.Opposite.RotateAsNew(RotationDirection.Counterclockwise) }
                .Select(r => this.Position + r.FacingCell)
                .SelectMany(p => p.GetThingList(this.Map).ToList())
                .Where(t => t.def.category == ThingCategory.Building)
                .SelectMany(t => Option(t as IBeltConbeyorSender))
                .ForEach(s => s.NortifyReceivable());
        }

        protected override bool WorkInterruption(Thing working)
        {
            return this.IsUnderground ? false : !working.Spawned || working.Position != this.Position;
        }

        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            workAmount = 1f;
            if (this.IsUnderground)
            {
                target = null;
                return false;
            }
            target = this.Position.GetThingList(this.Map).Where(t => t.def.category == ThingCategory.Item)
                .Where(t => t.def != ThingDefOf.ActiveDropPod)
                .FirstOption().GetOrDefault(null);
            if (target != null)
            {
                this.dest = Destination(target, true);
                if (target.Spawned && this.IsUnderground) target.DeSpawn();
                target.Position = this.Position;
            }
            return target != null;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            products = new List<Thing>().Append(working);
            return true;
        }

        protected override bool WorkingIsDespawned()
        {
            return true;
        }

        public static bool IsBeltConveyorDef(ThingDef def)
        {
            return typeof(Building_BeltConveyor).IsAssignableFrom(def.thingClass);
        }

        public static bool IsUndergroundDef(ThingDef def)
        {
            return Option(def.GetModExtension<ModExtension_Conveyor>()).Fold(false)(x => x.underground);
        }


        public static bool CanLink(Thing @this, Thing other, ThingDef thisDef, ThingDef otherDef)
        {
            var t = @this;
            if (Building_BeltConveyor.IsBeltConveyorDef(thisDef))
            {
                var ug = Building_BeltConveyor.IsUndergroundDef(thisDef);
                if (Building_BeltConveyor.IsBeltConveyorDef(otherDef))
                {
                    return ug == Building_BeltConveyor.IsUndergroundDef(otherDef) && (
                        t.Position + t.Rotation.FacingCell == other.Position ||
                        t.Position + t.Rotation.Opposite.FacingCell == other.Position ||
                        other.Position + other.Rotation.FacingCell == t.Position ||
                        other.Position + other.Rotation.Opposite.FacingCell == t.Position);
                }
                else if (Building_BeltConveyorUGConnector.IsConveyorUGConnecterDef(otherDef))
                {
                    return t.Position + t.Rotation.FacingCell == other.Position ||
                        (other.Position + other.Rotation.FacingCell == t.Position && ug == Building_BeltConveyorUGConnector.ToUndergroundDef(otherDef)) ||
                        (other.Position + other.Rotation.Opposite.FacingCell == t.Position && ug != Building_BeltConveyorUGConnector.ToUndergroundDef(otherDef));
                }
            }
            else if (Building_BeltConveyorUGConnector.IsConveyorUGConnecterDef(thisDef))
            {
                var toUg = Building_BeltConveyorUGConnector.ToUndergroundDef(thisDef);
                if (Building_BeltConveyor.IsBeltConveyorDef(otherDef))
                {
                    return (t.Position + t.Rotation.FacingCell == other.Position && toUg == Building_BeltConveyor.IsUndergroundDef(otherDef)) ||
                        (t.Position + t.Rotation.Opposite.FacingCell == other.Position && toUg != Building_BeltConveyor.IsUndergroundDef(otherDef));
                }
                else if (Building_BeltConveyorUGConnector.IsConveyorUGConnecterDef(otherDef))
                {
                    return (t.Position + t.Rotation.FacingCell == other.Position && toUg != Building_BeltConveyorUGConnector.ToUndergroundDef(otherDef)) ||
                        (t.Position + t.Rotation.Opposite.FacingCell == other.Position && toUg != Building_BeltConveyorUGConnector.ToUndergroundDef(otherDef));
                }
            }
            return false;
        }

        public Thing Carrying()
        {
            if (this.State == WorkingState.Working)
            {
                return this.Working;
            }
            else if (this.State == WorkingState.Placing)
            {
                return this.products.FirstOption().GetOrDefault(null);
            }
            return null;
        }

        public Thing Pickup()
        {
            var pickup = this.Carrying();
            if (pickup != null)
            {
                this.products.Clear();
                this.working = null;
                this.ForceReady();
            }
            return pickup;
        }
    }
}
