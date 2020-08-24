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
using ProjectRimFactory;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.AutoMachineTool
{
    class Building_BeltConveyorUGConnector : Building_BaseMachine<Thing>, IBeltConveyorLinkable
    {
        private ModExtension_Conveyor Extension { get { return this.def.GetModExtension<ModExtension_Conveyor>(); } }
        public override float SupplyPowerForSpeed { get => Building_BeltConveyor.supplyPower; set => Building_BeltConveyor.supplyPower = (int)value; }
        public bool IsStuck => this.stuck;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                LinkTargetConveyor().ForEach(x =>
                {
                    x.Link(this);
                    this.Link(x);
                });
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var targets = LinkTargetConveyor();

            base.DeSpawn();

            targets.ForEach(x => x.Unlink(this));
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();

            if (this.State != WorkingState.Ready && Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                var p = CarryPosition();
                if (!this.ToUnderground || this.WorkLeft > 0.7f)
                {
                    Vector2 result = Find.Camera.WorldToScreenPoint(p + new Vector3(0, 0, -0.4f)) / Prefs.UIScale;
                    result.y = (float)UI.screenHeight - result.y;
                    GenMapUI.DrawThingLabel(result, this.CarryingThing().stackCount.ToStringCached(), GenMapUI.DefaultThingLabelColor);
                }
            }
        }

        public override void Draw()
        {
            base.Draw();

            if (this.State != WorkingState.Ready)
            {
                var p = CarryPosition();
                if (!this.ToUnderground || this.WorkLeft > 0.7f)
                {
                    this.CarryingThing().DrawAt(p);
                }
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
            var workLeft = this.stuck ? Mathf.Clamp(Mathf.Abs(this.WorkLeft), 0f, 0.8f) : Mathf.Clamp01(this.WorkLeft);
            return (this.Rotation.FacingCell.ToVector3() * (1f - workLeft)) + this.Position.ToVector3() + new Vector3(0.5f, 10f, 0.5f);
        }

        public override bool CanStackWith(Thing other)
        {
            return base.CanStackWith(other) && this.State == WorkingState.Ready;
        }

        public bool ReceiveThing(bool underground, Thing t)
        {
            if (!this.ReceivableNow(underground, t))
                return false;
            if (this.State == WorkingState.Ready)
            {
                if (t.Spawned) t.DeSpawn();
                this.ForceStartWork(t, 1f);
                return true;
            }
            else
            {
                var target = this.State == WorkingState.Working ? this.Working : this.products[0];
                return target.TryAbsorbStack(t, true);
            }
        }

        public override bool AcceptsThing(Thing newThing, IPRF_Building giver = null)
        {
            Log.Warning("" + this + " was asked if it can accept " + newThing);
            if (giver is AutoMachineTool.IBeltConveyorLinkable conveyor) {
                if (this.ToUnderground) {
                    if (!conveyor.CanSendToLevel(ConveyorLevel.Ground)) return false;
                } else
                    if (!conveyor.CanSendToLevel(ConveyorLevel.Underground)) return false;
            }
            if (!this.ReceivableNow(true /*TODO: XXX */, newThing))
                return false;
            if (this.State == WorkingState.Ready) {
                if (newThing.Spawned && this.IsUnderground) newThing.DeSpawn();
                newThing.Position = this.Position;
                this.ForceStartWork(newThing, 1f);
                return true;
            } else {
                var target = this.State == WorkingState.Working ? this.Working : this.products[0];
                return target.TryAbsorbStack(newThing, true);
            }
        }

        protected override bool PlaceProduct(ref List<Thing> products)
        {
            // These can only place things in one direction,
            //   so the placing is different from base conveyors:
            var thing = products[0];
            // TODO: check each if multilpe levels possible
            var next = this.OutputConveyor();
            if (next != null)
            {
                // コンベアある場合、そっちに流す.
                // If there is a conveyor, let it flow to it
                if (next.AcceptsThing(thing, this))
                {
                    this.stuck = false;
                    return true;
                }
            }
            else
            {
                if (!this.ToUnderground && this.PRFTryPlaceThing(thing, 
                    this.Position + this.Rotation.FacingCell,
                    this.Map)) 
                {
                    this.stuck = false;
                    return true;
                }
            }
            // 配置失敗.
            this.stuck = true;
            return false;
        }

        [Unsaved]
        private bool stuck = false;

        public void Link(IBeltConveyorLinkable link)
        {
        }

        public void Unlink(IBeltConveyorLinkable unlink)
        {
        }

        public IEnumerable<Rot4> OutputRots
        {
            get
            {
                yield return this.Rotation;
            }
        }

        private IEnumerable<IBeltConveyorLinkable> LinkTargetConveyor()
        {
            return new List<Rot4> { this.Rotation, this.Rotation.Opposite }
                .Select(r => this.Position + r.FacingCell)
                .SelectMany(t => t.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => Building_BeltConveyor.CanLink(this, t, this.def, t.def))
                .Select(t => (t as IBeltConveyorLinkable));
        }

        // TODO: Faster to directly access, or to cache
        private IBeltConveyorLinkable OutputConveyor()
        {
            return this.LinkTargetConveyor()
                .Where(x => x.Position == this.Position + this.Rotation.FacingCell)
                .FirstOrDefault();
        }

        public bool ReceivableNow(bool underground, Thing thing)
        {
            if (!this.IsActive())// TODO: XXX || this.ToUnderground == underground)
            {
                return false;
            }
            Func<Thing, bool> check = (t) => t.CanStackWith(thing) && t.stackCount < t.def.stackLimit;
            switch (this.State)
            {
                case WorkingState.Ready:
                    return true;
                case WorkingState.Working:
                    // return check(this.working);
                    return false;
                case WorkingState.Placing:
                    return check(this.products[0]);
                default:
                    return false;
            }
        }

        protected override bool WorkInterruption(Thing working)
        {
            return false;
        }

        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            target = null;
            workAmount = 1f;
            return false;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            products = new List<Thing>().Append(working);
            return true;
        }

        public bool IsUnderground { get => false; }
        // 
        public bool ToUnderground { get => this.Extension.toUnderground; }

        public bool CanSendToLevel(ConveyorLevel level)
        {
            if (this.ToUnderground) {
                if (level == ConveyorLevel.Underground) return true;
            } else // this is exit to surface:
                if (level == ConveyorLevel.Ground) return true;
            return false;
        }
        public bool CanReceiveFromLevel(ConveyorLevel level)
        {
            if (this.ToUnderground) {
                if (level == ConveyorLevel.Ground) return true;
            } else // this is exit to surface:
                if (level == ConveyorLevel.Underground) return true;
            return false;
        }

        protected override bool WorkingIsDespawned()
        {
            return true;
        }

        public static bool IsConveyorUGConnecterDef(ThingDef def)
        {
            return typeof(Building_BeltConveyorUGConnector).IsAssignableFrom(def.thingClass);
        }

        public static bool ToUndergroundDef(ThingDef def)
        {
            return Option(def.GetModExtension<ModExtension_Conveyor>()).Fold(false)(x => x.toUnderground);
        }
    }
}
