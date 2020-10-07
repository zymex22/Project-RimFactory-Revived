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
/// <summary>
/// Conveyor Belts
/// </summary>
/// <designNotes>
/// Conveyor belts move an unspawned item from their position to
///   an adjacent one.  When their work is finished, the item is
///   placed.
/// Important note: If the item is ever spawned, it needs to be
///   either placed immediately, or despanwed before Placing()
///   happens - the products[] (from whence items are placed)
///   is saved Deep, not Reference, and if the item is spawned,
///   it will already be saved by the map.  This creates a small
///   window in which a problem can occur.
/// </designNotes>
    public class Building_BeltConveyor : Building_BaseMachine<Thing>, IBeltConveyorLinkable, IThingHolder, IHideItem, IHideRightClickMenu, IForbidPawnOutputItem
    {
        public Building_BeltConveyor()
        {
            base.setInitialMinPower = false;
            this.thingOwnerInt = new ThingOwner<Thing>(this);
            products = thingOwnerInt.InnerListForReading;
        }

        protected ThingOwner<Thing> thingOwnerInt;

        public static float supplyPower = 10f;
        protected bool stuck = false;

        // how far towards the next belt to stop:
        protected readonly float stuckDrawPercent = 0.3f;
        // A few display constants:
        // scale to draw items while on belts:
        protected const float carriedItemScale = 0.75f;
        // additional height over the belt's True Center to draw:
        protected const float carriedItemDrawHeight = 0.15f;

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
        /**************** IPRF_Building ***************/
        public override Thing GetThingBy(Func<Thing, bool> optionalValidator = null) {
            // TODO: underground?  Mod Setting?
            if (IsUnderground || this.State == WorkingState.Ready) return null;
            if (optionalValidator == null || optionalValidator(products[0])) {
                Thing t = thingOwnerInt.Take(products[0]);
                working = null;
                this.ForceReady();
                return t;
            }
            return null;
            /*
            if (working != null && (optionalValidator==null || optionalValidator(working))) {
                Thing t = working;
                working = null;
                this.ForceReady();
                return t;
            }
            // Nobo would have written this in an entirely different - and probably
            //   more elegant - way, but this works.
            if (!products.NullOrEmpty()) {
                // should only be one, but who knows.
                for (int i = products.Count - 1; i >= 0; i--) {
                    if (optionalValidator == null || optionalValidator(products[i])) {
                        Thing t = products[i];
                        products.Remove(t);
                        if (working == null)  // We were placing
                            ForceReady();
                        return t;
                    }
                }
            }
            return null;*/
        }
        // Conveyors are dumb. They just dump their stuff onto the ground when they end!
        //   TODO: mod setting?
        public override bool ObeysStorageFilters => false;
        /************* Conveyors IBeltConveyor ***********/
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

        /********** IThinHolder ***********/
        public void GetChildHolders(List<IThingHolder> outChildren) => Enumerable.Empty<IThingHolder>();
        public ThingOwner GetDirectlyHeldThings() => thingOwnerInt;

        /********** Pawn Interactions ********/
        public bool HideRightClickMenus => !this.IsUnderground && this.State != WorkingState.Ready;
        public bool ForbidPawnOutput => !this.IsUnderground && this.State != WorkingState.Ready;
        public override IEnumerable<Gizmo> GetGizmos() {
            foreach (Gizmo gizmo in base.GetGizmos()) {
                yield return gizmo;
            }
            if (!this.products.NullOrEmpty()) {
                Thing dropThing = products[0];
                yield return new Command_Action
                {
                    defaultLabel = "PRF_DropThing".Translate(dropThing.Label),
                    defaultDesc = "PRF_DropFromConveyorDesc".Translate(),
                    icon = (Texture2D)dropThing.Graphic.MatSingleFor(dropThing).mainTexture,
                    action=delegate() { DropThing(dropThing); },
                };
            }
        }
        public void DropThing(Thing t) {
            if (products == null || !products.Contains(t)) {
                //TODO: translate
                Messages.Message("Error: Conveyor " + this + " does not have " + t.Label,
                    this, MessageTypeDefOf.RejectInput);
                return; // should never happen?
            }
            thingOwnerInt.Take(t);
            Debug.Warning(Debug.Flag.Conveyors, "Conveyor " + this + " attempting to drop " + t);
            if (GenPlace.TryPlaceThing(t, Position, Map, ThingPlaceMode.Near, null,
                      // no above-ground conveyors, impassable cells:
                      delegate (IntVec3 c) {
                          if (c.Impassable(this.Map)) return false;
                          Debug.Message(Debug.Flag.Conveyors, "  validating " + c);
                          foreach (var jl in c.GetThingList(Map).OfType<IBeltConveyorLinkable>()) {
                              Debug.Message(Debug.Flag.Conveyors, "  but found " + jl);
                              if (!jl.IsUnderground) {
                                  Debug.Message(Debug.Flag.Conveyors, "  which is above ground, failed");
                                  return false;
                              }
                          }
                          return true;
                      })) {
                // successfully placed
                working = null;
                products.Remove(t);
                this.ForceReady();
            } else { // failed to place
                Messages.Message("PRF_CouldNotPlaceThing".Translate(t.Label), 
                      this, MessageTypeDefOf.NegativeEvent);
                thingOwnerInt.TryAdd(t);
            }
        }

        /********** RimWorld *********/
        /**********************
         * Saving and Loading
         * This should allow pulling in the data for 
         * old versions of belts seamlessly! I hope.
         * The new version don't save items directly
         * but through the ThingOwner object.
         * Old versions saved directly.
         */
        protected override LookMode WorkingLookMode {
            get {
                if (Scribe.mode == LoadSaveMode.Saving) return LookMode.Reference;
                if (version == 1) return LookMode.Deep;
                return LookMode.Reference;
            }
        }
        protected override LookMode ProductsLookMode {
            get {
                if (Scribe.mode == LoadSaveMode.Saving) return LookMode.Reference;
                if (version == 1) return LookMode.Deep;
                return LookMode.Reference;
            }
        }
        static int version = 2;
        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving) version = 2;
            Scribe_Values.Look(ref version, "v", 1);
            base.ExposeData();

            Scribe_Values.Look(ref supplyPower, "supplyPower", 10f);
            Scribe_Deep.Look(ref thingOwnerInt, "thingOwner", new object[] { this });
            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                if (thingOwnerInt == null) {
                    thingOwnerInt = new ThingOwner<Thing>(this);
                }
                if (version < 2) {
                    if (working != null) thingOwnerInt.TryAdd(working);
                    foreach (var t in products) thingOwnerInt.TryAdd(t);
                }
                this.products = this.thingOwnerInt.InnerListForReading;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            base.SpawnSetup(map, respawningAfterLoad);
            this.showProgressBar = false;
            foreach (var c in AllNearbyLinkables()) {
                c.Link(this);
                this.Link(c);
            }
            // already set, but just in case:
            this.products = thingOwnerInt.InnerListForReading;
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
        public bool HideItems => !this.IsUnderground && this.State != WorkingState.Ready;
        public virtual IEnumerable<Rot4> ActiveOutputDirections {
            get { yield return this.Rotation; }
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
            // Don't draw underground things by default:
            if (this.IsUnderground && !OverlayDrawHandler_UGConveyor.ShouldDraw)
            {
                // 地下コンベアの場合には表示しない.
                return;
            }
            base.Draw();
            if (this.State != WorkingState.Ready)
            {
                DrawCarried();
            }
        }
        /// <summary>
        /// Draw the carried item (there should be one). This allows
        ///   derived classes to decide when/how to draw their items.
        /// </summary>
        public virtual void DrawCarried() {
            Thing t = CarryingThing();
            // Took this line from MinifiedThing; don't know if it's needed:
            var g = t.Graphic.ExtractInnerGraphicFor(t);
            // Graphic's GetCopy() fails on any Graphic_RandomRotated
            //   There is no easy way to get around this, but this seems
            //   to work the best so far:
            if (g is Graphic_RandomRotated grr) {
                var d = t.def.graphicData;
                g = GraphicDatabase.Get(d.graphicClass, d.texPath, g.Shader,
                          new Vector2(carriedItemScale, carriedItemScale), g.color, g.colorTwo);
            } else {
                g = g.GetCopy(new Vector2(carriedItemScale, carriedItemScale));
            }
            g.Draw(this.CarryPosition(), CarryingThing().Rotation, CarryingThing(), 0f);
        }
        protected Vector3 CarryPosition() {
            if (stuck) {
                return (this.TrueCenter() + new Vector3(0, carriedItemDrawHeight, 0) +
                  this.OutputDirection.FacingCell.ToVector3()
                    * (stuckDrawPercent + Mathf.Clamp01(WorkLeft)));
            } else {
                return (this.TrueCenter() + new Vector3(0, carriedItemDrawHeight, 0) +
                  this.OutputDirection.FacingCell.ToVector3()
                    * (1f - Mathf.Clamp01(WorkLeft)));
            }
        }
        /******** AutoMachineTool logic *********/
        protected override void ForceStartWork(Thing working, float workAmount) {
            base.ForceStartWork(working, workAmount);
            thingOwnerInt.TryAdd(working);
        }
        protected override void Reset() {
            /*            if (this.State != WorkingState.Ready) {
                            if (this.working != null) {
                                this.products.Add(this.working);
                            }
                        }*/
            //TODO: Underground belts should not spawn items above ground on reset...
            if (thingOwnerInt.Any) {// remove them from care of thingOwner:
                this.products = new List<Thing>(thingOwnerInt.InnerListForReading);
                thingOwnerInt.Clear();
            }
            base.Reset();
            this.products = thingOwnerInt.InnerListForReading;
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
            Debug.Warning(Debug.Flag.Conveyors, "" + this + " was asked if it will accept " + newThing);
            if (!this.IsActive()) return false;
            // verify proper levels:
            if (giver is AutoMachineTool.IBeltConveyorLinkable linkableGiver) {
                if (!this.CanLinkFrom(linkableGiver, false)) return false;
            }
            Debug.Message(Debug.Flag.Conveyors, "  It can accept items from " +
                (giver == null ? "that direction." : giver.ToString()));
            if (this.State == WorkingState.Ready)
            {
                // Note: I don't think there is any benefit to 
                //   an item being spanwed?  But what do I know?
                if (newThing.Spawned) newThing.DeSpawn();
                Debug.Message(Debug.Flag.Conveyors, "  And accepted " + newThing
                    + (newThing.Spawned ? "" : " (not spanwed)"));
                newThing.Position = this.Position;
                stuck = false;
                this.ForceStartWork(newThing, 1f);
                return true;
            }
            else
            {
                var target = this.State == WorkingState.Working ? this.Working : this.products[0];
                Debug.Message(Debug.Flag.Conveyors, "  but busy with " + target +
                                   ". Will try to absorb");
                return target.TryAbsorbStack(newThing, true);
            }
        }

        public bool CanAcceptNow(Thing thing) {
            Debug.Message(Debug.Flag.Conveyors, "  " + this + " was asked if it can accept " + thing);
            if (!this.IsActive()) {
                Debug.Message(Debug.Flag.Conveyors, "      but it's not active and can't");
                return false;
            }
            switch (this.State) {
                case WorkingState.Ready:
                    Debug.Message(Debug.Flag.Conveyors, "    Ready state: yes");
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
            //            var thing = products[0];
            //todo: test for work interruption first....
            if (thingOwnerInt.Count == 0) {
                Debug.Message(Debug.Flag.Conveyors, "Conveyor " + this + " no longer has anything to place");
                return true; // ready for next action.
            }
            // Has something else taken the Thing away from us? (b/c they are spawned? or something else?)
            if (this.WorkInterruption(products[0])) {
                Debug.Message(Debug.Flag.Conveyors, "  but something has already moved the item.");
                return true;
            }
            Thing thing = thingOwnerInt.Take(thingOwnerInt[0]);
            Debug.Warning(Debug.Flag.Conveyors, "Conveyor " + this + 
                (stuck?" is stuck with ":" is about to try placing ") + thing);
            if (stuck) {
                thingOwnerInt.TryAdd(thing);
                if (this.CanOutput(thing)) {
                    ChangeStuckStatus(thing);
                    return false; // still not ready for new action
                }
                return false;
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
                Debug.Message(Debug.Flag.Conveyors, " but next belt cannot take it now; stuck.");
                // Don't try anything else if other belt is busy
            }
            else // if no conveyor, place if can
            {
                Debug.Message(Debug.Flag.Conveyors, "  trying to place at end of conveyor:");
                if (this.CanSendToLevel(ConveyorLevel.Ground) && this.PRFTryPlaceThing(thing, 
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
            thingOwnerInt.TryAdd(thing); // put back
            Debug.Message(Debug.Flag.Conveyors, "    Could not place it. Stuck.");
            ChangeStuckStatus(thing);
            return false; // not ready for next work
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
        protected virtual IBeltConveyorLinkable OutputBeltAt(IntVec3 location)
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
/*            var o = working.holdingOwner;
            if (o != null) {
                string o1 = "null";
                string o2 = "null";
                if (o.Owner != null) o1 = o.Owner.ToString();
                if (thingOwnerInt.Owner != null) o2 = thingOwnerInt.Owner.ToString();
                Log.Error("Working holding owner: " + o1 + " vs this: " + o2 + " -> " + (working.holdingOwner.Owner == thingOwnerInt.Owner)
                   + " ... actual test: " + (working.holdingOwner == thingOwnerInt));
            } else Log.Error("working is " + working + " and has no own....fuck, of course no=t.");*/
            return working.holdingOwner != thingOwnerInt;
//            return false;
            //TODO: this was originally designed, as far as I can tell, to 
            //  trigger if something was picked up or moved (which can in 
            //  theory happen if it's spawned...) but I cannot seem to make
            //  it happen?
            // So, do we want to keep any of that logic around?
            // return this.IsUnderground ? false : !working.Spawned || working.Position != this.Position;
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
                if (target.Spawned) target.DeSpawn();
                target.Position = this.Position;
            }
            if (target != null) thingOwnerInt.TryAdd(target);
            return target != null;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            products = this.products;
            return products.Count > 0;
        }

        protected override void CheckWork() {
            //TODO: Add test here :p
            if (stuck) {
                if (CanOutput(working)) {
                    // start going forward again:
                    ChangeStuckStatus(working);
                }
            } else {
                // if work done is below stuckDrawPercent, let it continue forward
                if (WorkLeft < 1-this.stuckDrawPercent) {
                    if (!CanOutput(working)) {
                        ChangeStuckStatus(working);
                        return;
                    }
                }
            }
            base.CheckWork();
        }
        protected virtual bool CanOutput(Thing t) {
            var belt = this.OutputBeltAt(this.OutputCell());
            if (belt != null) {
                Debug.Message(Debug.Flag.Conveyors,
                    "  CanOutput: Testing can " + " accept " + t);
                return belt.CanAcceptNow(t);
            }
            Debug.Message(Debug.Flag.Conveyors, "    No belts can take " + t);
            return this.CanSendToLevel(ConveyorLevel.Ground) 
                    && PlaceThingUtility.CallNoStorageBlockersIn(OutputCell(), Map, t);
        }

        protected void ChangeStuckStatus(Thing t) {
            bool willBeStuck = !stuck;
            thingOwnerInt.RemoveAll(thing=>true);
            working = null;
            if (willBeStuck) {
                Debug.Message(Debug.Flag.Conveyors, this + " is now stuck with " + t);
                this.ForceStartWork(t, 1 - stuckDrawPercent
                        - Mathf.Clamp01(WorkLeft));
                stuck = true;
            } else {
                Debug.Message(Debug.Flag.Conveyors, this + " is no longer stuck with " + t);
                this.ForceStartWork(t, 1 - stuckDrawPercent
                          - Mathf.Clamp(WorkLeft, 0, 1 - stuckDrawPercent));
                stuck = false;
            }
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

        public Thing Pickup()//<) TODO
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
                if (def.GetModExtension<ModExtension_Conveyor>()?.underground != true)
                    return false;
            } else { // Ground
                if (def.GetModExtension<ModExtension_Conveyor>()?.underground == true)
                    return false;
            }
            return defRotation == queryRotation;
        }
        /*public static IEnumerable<Rot4> AllRot4DefCanSendToAtLevel(ThingDef def, Rot4 defRotation,
            ConveyorLevel level) {
            if (level == ConveyorLevel.Underground &&
                 def.GetModExtension<ModExtension_Conveyor>().underground)//wrong
                yield return new Rot4(defRotation.AsInt);
        }*/
        public static bool CanDefReceiveFromRot4AtLevel(ThingDef def, Rot4 defRotation, 
                      Rot4 queryRotation, ConveyorLevel queryLevel) {
            if ((queryLevel == ConveyorLevel.Ground &&
                 def.GetModExtension<ModExtension_Conveyor>()?.underground != true)
                || (queryLevel == ConveyorLevel.Underground &&
                    def.GetModExtension<ModExtension_Conveyor>()?.underground == true))
                return (defRotation != queryRotation.Opposite);
            return false;
        }
    }
}
