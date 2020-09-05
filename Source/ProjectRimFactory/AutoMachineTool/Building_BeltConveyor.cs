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
using ProjectRimFactory.Common;
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

    public class Building_BeltConveyor : Building_BaseMachine<Thing>, IBeltConveyorLinkable, IHideItem, IHideRightClickMenu, IForbidPawnOutputItem
    {
        public Building_BeltConveyor()
        {
            base.setInitialMinPower = false;
        }

        public static float supplyPower = 10f;

        [Unsaved]
        protected bool stuck = false;




        // Generally useful methods:
        protected ModExtension_Conveyor Extension => this.def.GetModExtension<ModExtension_Conveyor>();
        protected virtual Rot4 OutputDirection => this.Rotation;

        // AutoMachineTool: Building_Base
        public override IntVec3 OutputCell() => this.Position + this.OutputDirection.FacingCell;

        public override float SupplyPowerForSpeed
        {
            get
            {
                return supplyPower;
            }

            set
            {
                supplyPower = value;
                this.RefreshPowerStatus();
            }
        }
        /************* Conveyors IBeltConveyor ***********/
        // Conveyors are dumb. They just dump their stuff onto the ground when they end!
        //   TODO: mod setting?
        public override bool ObeysStorageFilters => false;
        public bool IsStuck => this.stuck;
        public bool IsUnderground { get => this.Extension?.underground ?? false; }
        public virtual bool CanSendToLevel(ConveyorLevel level)
        {
            if (this.IsUnderground) {
                if (level == ConveyorLevel.Underground) return true;
            } else // on surface
                if (level == ConveyorLevel.Ground) return true;
            return false;
        }
        public virtual bool CanReceiveFromLevel(ConveyorLevel level) => CanSendToLevel(level);

        /********** Display *********/
        public bool HideItems => !this.IsUnderground && this.State != WorkingState.Ready;

        /********** Interactions ********/
        public bool HideRightClickMenus => !this.IsUnderground && this.State != WorkingState.Ready;
        public bool ForbidPawnOutput => !this.IsUnderground && this.State != WorkingState.Ready;

        /********** RimWorld *********/
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref supplyPower, "supplyPower", 10f);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            base.SpawnSetup(map, respawningAfterLoad);
            this.showProgressBar = false;

            //TODO: was originally only !respawningAfterLoad - okay?
            foreach (var c in AllNearbyLinkables()) {
                c.Link(this);
                this.Link(c);
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var targets = AllNearbyLinkables().ToList();
            base.DeSpawn(mode);

            targets.ForEach(x => x.Unlink(this));
        }
        // What does this even mean for a building, anyway?
        public override bool CanStackWith(Thing other) {
            return base.CanStackWith(other) && this.State == WorkingState.Ready;
        }

        /********* Display **********/
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
            //TODO: what does this do?
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

        protected Vector3 CarryPosition() {
            var workLeft = this.stuck ? Mathf.Clamp(Mathf.Abs(this.WorkLeft), 0f, 0.5f) : Mathf.Clamp01(this.WorkLeft);
            return (this.OutputDirection.FacingCell.ToVector3() * (1f - workLeft)) + this.Position.ToVector3() + new Vector3(0.5f, 10f, 0.5f);
        }
        /******** AutoMachineTool logic *********/
        protected override void Reset() {
            if (this.State != WorkingState.Ready) {
                if (this.working != null) {
                    this.products.Add(this.working);
                }
            }
            //TODO: Underground belts should not spawn items above ground on reset...
            base.Reset();
        }

        protected Thing CarryingThing()
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

        public override bool AcceptsThing(Thing newThing, IPRF_Building giver = null) {
            Debug.Warning(Debug.Flag.Conveyors, "" + this + " was asked if it can accept " + newThing);
            if (!this.IsActive()) return false;
            // verify proper levels:
            if (giver is AutoMachineTool.IBeltConveyorLinkable) {
                if (this.IsUnderground) {
                    if (!((IBeltConveyorLinkable)giver).CanSendToLevel(ConveyorLevel.Underground))
                        return false;
                } else // not underground
                    if (!((IBeltConveyorLinkable)giver).CanSendToLevel(ConveyorLevel.Ground))
                        return false;
            }
            if (this.State == WorkingState.Ready)
            {
                if (newThing.Spawned && this.IsUnderground) newThing.DeSpawn();
                newThing.Position = this.Position;
                this.ForceStartWork(newThing, 1f);
                return true;
            }
            else
            {
                var target = this.State == WorkingState.Working ? this.Working : this.products[0];
                return target.TryAbsorbStack(newThing, true);
            }
        }

        public bool CanAcceptNow(Thing thing) {
            if (!this.IsActive()) return false;
            switch (this.State) {
                case WorkingState.Ready:
                    return true;
                case WorkingState.Working:
                    return ThisCanAcceptThat(this.Working, thing);
                case WorkingState.Placing:
                    return ThisCanAcceptThat(this.products[0], thing);
                default:
                    return false;
            }
        }
        protected bool ThisCanAcceptThat(Thing t1, Thing t2) =>
                       t1.CanStackWith(t2) && t1.stackCount < t1.def.stackLimit;

        protected override bool PlaceProduct(ref List<Thing> products)
        {
            var thing = products[0];
            Debug.Warning(Debug.Flag.Conveyors, "Conveyor " + this + " is about to try placing " + thing);
            if (this.WorkInterruption(thing))
            {
                return true;
            }
            // Try to send to another conveyor first:
            // コンベアある場合、そっちに流す.
            var outputBelt = this.OutputBeltAt(this.OutputCell());
            if (outputBelt != null)
            {
                if ((outputBelt as IPRF_Building).AcceptsThing(thing,this))
                {
                    NotifyAroundSender();
                    this.stuck = false;
                    Debug.Message(Debug.Flag.Conveyors, " and successfully passed it to " + outputBelt);
                    return true;
                }
                return false; // Don't try anything else if other belt is busy
            }
            else // if no conveyor, place if can
            {
                Debug.Message(Debug.Flag.Conveyors, "  trying to place at end of conveyor:");
                if (!this.IsUnderground && this.PRFTryPlaceThing(thing, 
                      this.OutputCell(), this.Map))
                {
                    NotifyAroundSender();
                    this.stuck = false;
                    Debug.Message(Debug.Flag.Conveyors, "Successfully placed!");
                    return true;
                }
            }
            // 配置失敗.
            // Placement failure
            this.stuck = true;
            return false;
        }

        public virtual void Link(IBeltConveyorLinkable link)
        {
        }

        public virtual void Unlink(IBeltConveyorLinkable unlink)
        {
        }

        /// <summary>
        /// Return the first belt at <paramref name="location"/> that this can send to
        /// </summary>
        /// <returns>The belt, or null if none found</returns>
        /// <param name="location">Valid IntVec3 this conveyor can send to</param>
        protected IBeltConveyorLinkable OutputBeltAt(IntVec3 location)
        {
            return location.GetThingList(this.Map)
                .OfType<IBeltConveyorLinkable>()
                .Where(b=>this.CanLinkTo(b, false))
                .Where(b=>b.CanLinkFrom(this))
                .FirstOrDefault();
        }

        protected IEnumerable<IBeltConveyorLinkable> AllNearbyLinkables() {
            return Enumerable.Range(0, 4).Select(i => this.Position + new Rot4(i).FacingCell)
                .SelectMany(c => c.GetThingList(this.Map))
                .OfType<IBeltConveyorLinkable>();
        }

        //TODO: fold this ability into IPRF_Building:
        //  NotifyOutputReady()
        //  OutputCell()?
        //  OutputsTo(IntVec3 location)?  <-- may be better,
        //                                    we don't know how all PRF buildings
        //                                    even do output...
        //  OutputsTo(IPRF_Building) <-- may be even better?
        //         but might also require AcceptsOutputFrom()...
        private void NotifyAroundSender()
        {
            new Rot4[] { this.Rotation.Opposite, this.Rotation.Opposite.RotateAsNew(RotationDirection.Clockwise), this.Rotation.Opposite.RotateAsNew(RotationDirection.Counterclockwise) }
                .Select(r => this.Position + r.FacingCell)
                .SelectMany(p => p.GetThingList(this.Map).ToList())
                .Where(t => t.def.category == ThingCategory.Building)
                .SelectMany(t => Option(t as IBeltConveyorSender))
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
            target = this.Position.GetThingList(this.Map).Where(t => t.def.EverHaulable)
                .FirstOrDefault();
            if (target != null)
            {
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

        public virtual bool CanLinkTo(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition=true) {
            // First test: level (e.g., Ground vs Underground):
            bool flag = false;
            // Loop through enum:
            //   (Seriously, C#, this is stupid syntax)
            foreach (var level in (ConveyorLevel [])Enum.GetValues(typeof(ConveyorLevel))) {
                if (this.CanSendToLevel(level) && otherBeltLinkable.CanReceiveFromLevel(level)) {
                    flag = true;
                    break;
                }
            }
            if (!flag) return false;
            if (!checkPosition) return true;
            // Conveyor Belts can link forward:
            return (this.OutputCell() == otherBeltLinkable.Position);
        }
        public virtual bool CanLinkFrom(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition=true) {
            // First test: level (e.g., Ground vs Underground):
            bool flag = false;
            // Loop through enum:
            //   (Seriously, C#, this is stupid syntax)
            foreach (var level in (ConveyorLevel[])Enum.GetValues(typeof(ConveyorLevel))) {
                if (this.CanReceiveFromLevel(level) && otherBeltLinkable.CanSendToLevel(level)) {
                    flag = true;
                    break;
                }
            }
            if (!flag) return false;
            if (!checkPosition) return true;
            // If you can dump it on a conveyor belt, it will take it.
            // But it does assume you are right next to the belt?
            // And it doesn't like it if something right in front of it tries
            //   to give it something - what is it going to do, give it back?
            if (this.OutputCell() == otherBeltLinkable.Position) return false;
            return this.Position.AdjacentToCardinal(otherBeltLinkable.Position);
        }
        public virtual bool HasLinkWith(IBeltConveyorLinkable otherBelt) {
            // TODO: should we cache these?
            return (CanLinkTo(otherBelt) && otherBelt.CanLinkFrom(this))
                || (otherBelt.CanLinkTo(this) && CanLinkFrom(otherBelt));
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
        public static bool CanDefSendToRot4AtLevel(ThingDef def, Rot4 defRotation,
                             Rot4 queryRotation, ConveyorLevel queryLevel) {
            // Not going to error check here: if there's a config error, there will be prominent
            //   red error messages in the log.
            if (queryLevel == ConveyorLevel.Underground) {
                if (!def.GetModExtension<ModExtension_Conveyor>().underground)
                    return false;
            } else { // Ground
                if (def.GetModExtension<ModExtension_Conveyor>().underground)
                    return false;
            }
            return defRotation == queryRotation;
        }
        public static IEnumerable<Rot4> AllRot4DefCanSendToAtLevel(ThingDef def, Rot4 defRotation,
            ConveyorLevel level) {
            if (level == ConveyorLevel.Underground &&
                 def.GetModExtension<ModExtension_Conveyor>().underground)
                yield return new Rot4(defRotation.AsInt);
        }
        public static bool CanDefReceiveFromRot4AtLevel(ThingDef def, Rot4 defRotation, 
                      Rot4 queryRotation, ConveyorLevel queryLevel) {
            if ((queryLevel == ConveyorLevel.Ground &&
                 !def.GetModExtension<ModExtension_Conveyor>().underground)
                || (queryLevel == ConveyorLevel.Underground &&
                    def.GetModExtension<ModExtension_Conveyor>().underground))
                return (defRotation != queryRotation.Opposite);
            return false;
        }
    }
}
