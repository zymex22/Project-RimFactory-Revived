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
using ProjectRimFactory.Industry;

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
    [StaticConstructorOnStartup]
    public class Building_BeltConveyor : Building_BaseMachine<Thing>, IBeltConveyorLinkable, 
                     IThingHolder, IHideItem, IHideRightClickMenu, ILogicSignalReciver
    {
        public Building_BeltConveyor()
        {
            base.setInitialMinPower = false;
            this.thingOwnerInt = new ThingOwner_Conveyor(this);
            // this is a horrible bastardization of AutoMachineTool and 
            //  RW's ThingOwner mechanism, made viable by C#'s list-by-
            //  reference mechanism.  It works great!
            products = thingOwnerInt.InnerListForReading;
            // Conveyors are dumb. They just dump their stuff onto the ground when they end!
            this.obeysStorageFilters = false;
        }

        protected ThingOwner_Conveyor thingOwnerInt;
        // flag for Notify_ItemLost, to let it know it's okay
        //   that it lost an item and to not do anything.
        static protected bool usingThingOwnerInt = false;

        public static float supplyPower = 10f;
        protected bool stuck = false;
        private bool isEndOfLine = false; //todo: show window text explaining WhyTF it's outputting anything.

        // A few display constants:
        // how far towards the next belt to stop:
        protected readonly float stuckDrawPercent = 0.3f; // also slightly affects game logic
        // scale to draw items while on belts:
        //   Note: this is used if nothing is read from the ModExtension_Conveyors
        protected const float defaultCarriedItemScale = 0.75f;
        // additional height over the belt's True Center to draw:
        protected const float defaultCarriedItemDrawHeight = 0.15f;
        // and display variables
        protected float carriedItemDrawHeight = defaultCarriedItemDrawHeight;

        // Generally useful methods:
        protected ModExtension_Conveyor Extension => this.def.GetModExtension<ModExtension_Conveyor>();

        // Generally important methods:
        protected virtual Rot4 OutputDirection => this.Rotation;
        // AutoMachineTool: Building_Base
        public override IntVec3 OutputCell() => this.Position + this.OutputDirection.FacingCell;

        private LogicSignal refrerenceSignal = null;
        
        public LogicSignal RefrerenceSignal { get => refrerenceSignal; set => refrerenceSignal = value; }

        protected override bool IsActive()
        {
/*
            if (RefrerenceSignal != null)
            {
                Thing thing = null;
                if (products.Count > 0) thing = products[0];
                return RefrerenceSignal.GetValue( new DynamicSlotGroup(heldthings(thing)  )) == 1;
            } 

            */


            return base.IsActive();
        }



        /**************** IPRF_Building ***************/
        public override Thing GetThingBy(Func<Thing, bool> optionalValidator = null) {
            // TODO: underground?  Mod Setting?
            if (IsUnderground || this.State == WorkingState.Ready) return null;
            if (optionalValidator == null || optionalValidator(products[0])) {
                this.State = WorkingState.Placing;
                Thing t = thingOwnerInt.Take(products[0]);
                working = null;
                this.ForceReady();
                return t;
            }
            return null;
        }

        public override IEnumerable<Thing> AvailableThings
        {
            get
            {
                if (products.Count == 0) yield return null;
                yield return products[0];
            }
        }

        /************* Conveyors IBeltConveyor ***********/
        public bool IsStuck => this.stuck;
        public bool IsUnderground { get => this.Extension?.underground ?? false; }

        private PRFGameComponent pRFGameComponent = null;

        public bool LogicSignaStatus 
        {
            get
            {
                //Verify it still exists
                if (pRFGameComponent == null) pRFGameComponent = Current.Game.GetComponent<PRFGameComponent>();
                if (pRFGameComponent != null && refrerenceSignal != null && !pRFGameComponent.LoigSignalRegestry.ContainsKey(refrerenceSignal))
                {
                    //Refrence Signal got removed
                    refrerenceSignal = null;
                    Messages.Message("PRF_LogicController_MsgLogicSignalDestoyed".Translate(this.Label), this, MessageTypeDefOf.CautionInput);
                }
                if (RefrerenceSignal != null)
                {

                    DynamicSlotGroup input = null;
                    DynamicSlotGroup output = null;
                    

                    if (RefrerenceSignal.dynamicSlot == EnumDynamicSlotGroupID.NA)
                    {
                        if (RefrerenceSignal.GetValue() == 0) return true;
                    }
                    else if (RefrerenceSignal.dynamicSlot == EnumDynamicSlotGroupID.Both || RefrerenceSignal.dynamicSlot == EnumDynamicSlotGroupID.Group_1)
                    {
                        input = new DynamicSlotGroup(this.AvailableThings);
                    }
                    else if (RefrerenceSignal.dynamicSlot == EnumDynamicSlotGroupID.Both || RefrerenceSignal.dynamicSlot == EnumDynamicSlotGroupID.Group_2)
                    {
                        var outputBelt = this.OutputBeltAt(this.OutputCell());
                        if (outputBelt != null)
                        {
                            output = new DynamicSlotGroup(outputBelt.AvailableThings);
                        }
                        else
                        {
                            if (this.CanSendToLevel(ConveyorLevel.Ground))
                            {
                                SlotGroup slotGroup = this.OutputCell().GetSlotGroup(this.Map);
                                if (slotGroup != null && OutputToEntireStockpile)
                                {
                                    output = new DynamicSlotGroup(slotGroup);
                                }
                                else
                                {
                                    output = new DynamicSlotGroup(this.Map.thingGrid.ThingsListAt(this.OutputCell()));
                                }
                            }
                        }
                    }

                    if (RefrerenceSignal.GetValue(input, output) == 0) return true;
                }
                return false;

            } 
        
        }


        public virtual bool IsEndOfLine {
            get { return isEndOfLine; }
            set {
                if (isEndOfLine && value == false) {
                    isEndOfLine = false;
                    if (this.working != null) {
                        this.ForceStartWork(working, 1);
                    } else {
                        Reset();
                    }
                } else {
                    isEndOfLine = value;
                }
                // force redraw of graphic - links might have changed?
                if (Spawned) this.Map.mapDrawer.MapMeshDirty(this.Position,
                          MapMeshFlag.Buildings | MapMeshFlag.Things);
            }
        }
        // Note: it might be worth making link and unlink do something with a cached
        //   conveyor link; it will certainly be faster in some specific scenarios
        //   (underground belts going through a storehouse with DeepStorage or DSUs?)
        public virtual void Link(IBeltConveyorLinkable link) { }
        public virtual void Unlink(IBeltConveyorLinkable unlink) { }
        public virtual bool CanSendToLevel(ConveyorLevel level)
        {
            if (this.IsUnderground) {
                if (level == ConveyorLevel.Underground) return true;
            } else // on surface
                if (level == ConveyorLevel.Ground) return true;
            return false;
        }
        // Same for Conveyors:
        public virtual bool CanReceiveFromLevel(ConveyorLevel level) => CanSendToLevel(level);

        /********** IThingHolder ***********/
        public void GetChildHolders(List<IThingHolder> outChildren) => Enumerable.Empty<IThingHolder>();
        public ThingOwner GetDirectlyHeldThings() => thingOwnerInt;

        /********** Pawn Interactions ********/
        public bool HideRightClickMenus => !this.IsUnderground && this.State != WorkingState.Ready;
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
                    action=delegate()
                    {
                        DropThing(dropThing);
                        Find.Selector.ClearSelection();
                        Find.Selector.Select(dropThing);
                    },
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
        public void Notify_LostItem(Thing item) {
            if ((item.holdingOwner != this.thingOwnerInt) && !thingOwnerInt.Any
                 && this.State != WorkingState.Ready // nothing happening
                 && !usingThingOwnerInt)
            {
                Debug.Warning(Debug.Flag.Conveyors, this + " was notified it has lost ----- " + item);
                Reset(); // something took it!
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

            Scribe_Values.Look(ref stuck, "isStuck", false);
            Scribe_Values.Look(ref supplyPower, "supplyPower", 10f);
            Scribe_Values.Look(ref isEndOfLine, "isEOL", false);
            Scribe_Deep.Look(ref thingOwnerInt, "thingOwner", new object[] { this });
            //ILogicSignalReciver
            Scribe_References.Look(ref refrerenceSignal, "RefrerenceSignal");


            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                if (thingOwnerInt == null) {
                    thingOwnerInt = new ThingOwner_Conveyor(this);
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
        /// <summary>
        /// Output directions used by the Linked Graphic for drawing little arrows
        /// </summary>
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
            Thing c = this.CarryingThing();
            if (c != null && Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                var p = CarryPosition();
                Vector2 result = Find.Camera.WorldToScreenPoint(p + new Vector3(0, 0, -0.4f)) / Prefs.UIScale;
                result.y = (float)UI.screenHeight - result.y;
                GenMapUI.DrawThingLabel(result, c.stackCount.ToStringCached(), GenMapUI.DefaultThingLabelColor);
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
            if (t == null) return;
            float scale = this.def.GetModExtension<ModExtension_Conveyor>()?.
                          carriedItemScale ?? defaultCarriedItemScale;
            // Took this line from MinifiedThing; don't know if it's needed:
            //   However, it's insuffient to use Graphic's GetCopy() :(
            var g = t.Graphic.ExtractInnerGraphicFor(t);
            // Graphic's GetCopy() fails on any Graphic_RandomRotated
            //   And on minified things.  And who knows what else?
            //   There is no easy way to get around this, but this seems
            //   to work the best so far:
            GraphicData gd = t.def.graphicData;
            if (gd == null) gd = g.data;
            if (gd == null) {
                t.DrawAt(CarryPosition());
                return;
            }
            g = GraphicDatabase.Get(gd.graphicClass, gd.texPath, g.Shader,
                      new Vector2(scale, scale), g.color, g.colorTwo);
            g.Draw(this.CarryPosition(), CarryingThing().Rotation, CarryingThing(), 0f);
        }
        protected Vector3 CarryPosition() {
            if (IsEndOfLine || LogicSignaStatus) {
                return (this.TrueCenter() + new Vector3(0, carriedItemDrawHeight, 0));
            }
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
        protected void CalculateCarriedItemDrawHeight() {
            var nextBelt = this.OutputBeltAt(this.OutputCell());
            if (nextBelt != null) {
                var theirs = nextBelt.CarriedItemDrawHeight;
                var ours = this.CarriedItemDrawHeight;
                if (ours < theirs) {  //comparing whose is higher - what, are
                    //                    conveyor belts schoolboys?
                    carriedItemDrawHeight = theirs - this.TrueCenter().y;
                    return;
                }
            }
            carriedItemDrawHeight = defaultCarriedItemDrawHeight;
        }
        /// <summary>
        /// External use only - default draw height for carried items
        /// </summary>
        public float CarriedItemDrawHeight {
            get {
                return this.TrueCenter().y + defaultCarriedItemDrawHeight;
            }
        }
        public override string GetInspectString()
        {
            if (IsEndOfLine) {
                return base.GetInspectString()+"\n"+"PRF.Conveyor.IsAtEndOfLine".Translate();
            }
            return base.GetInspectString();
        }

        /******** IPRF logic *********/
        public override bool AcceptsThing(Thing newThing, IPRF_Building giver = null) {
            Debug.Warning(Debug.Flag.Conveyors, "" + this + " was asked if it will accept " + newThing);
            if (!this.IsActive()) return false;
            // verify proper levels:
            if (giver is AutoMachineTool.IBeltConveyorLinkable linkableGiver) {
                if (!this.CanLinkFrom(linkableGiver, false)) return false;
            }
            Debug.Message(Debug.Flag.Conveyors, "  It can accept items from " +
                (giver == null ? "that direction." : giver.ToString()));
            if (this.State == WorkingState.Ready) {
                // Note: I don't think there is any benefit to 
                //   an item being spanwed?  But what do I know?
                if (newThing.Spawned) newThing.DeSpawn();
                Debug.Message(Debug.Flag.Conveyors, "  And accepted " + newThing
                    + (newThing.Spawned ? "" : " (not spanwed)"));
                newThing.Position = this.Position;
                stuck = false;
                this.ForceStartWork(newThing, 1f);
                return true;
            } else {
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
                    return this.products.Count == 0 || ThisCanAcceptThat(this.products[0], thing);
                default:
                    return false;
            }
        }
        protected bool ThisCanAcceptThat(Thing t1, Thing t2) =>
                       t1.CanStackWith(t2) && t1.stackCount < t1.def.stackLimit;
        // We (LWM) are mean and don't allow conveyors to change the "Obey Storage Filters"
        //   setting.  Maybe if zymex is really nice we can change this....
        public override PRFBSetting SettingsOptions {
            get => base.SettingsOptions & ~PRFBSetting.optionObeysStorageFilters;
        }

        bool ILogicSignalReciver.SupportsAdvancedReciverMode => false;

        bool ILogicSignalReciver.UsesAdvancedReciverMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        List<LSR_Entry> ILogicSignalReciver.LSR_Advanced { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /******** AutoMachineTool logic *********/
        protected override bool TryStartWorking(out Thing target, out float workAmount) {
            CalculateCarriedItemDrawHeight();
            workAmount = 1f;
            if (this.IsUnderground) {
                target = null;
                return false;
            }
            target = this.Position.GetThingList(this.Map).FirstOrDefault(t => t.def.EverHaulable);
            if (target != null) {
                if (target.Spawned) target.DeSpawn();
                target.Position = this.Position;
            }
            if (target != null) thingOwnerInt.TryAdd(target);

            return target != null;
        }
        protected override void ForceStartWork(Thing working, float workAmount) {
            CalculateCarriedItemDrawHeight();
            if (thingOwnerInt.Contains(working)) {
                thingOwnerInt.Remove(working);
            }
            base.ForceStartWork(working, workAmount);
            thingOwnerInt.TryAdd(working);
        }
        protected override void StartWork()
        {
            base.StartWork();
            MapManager.RemoveAfterAction(CheckWork); // StartWork adds this, but...
            this.CheckWork();  // ...make sure we're not stuck NOW
        }

        protected override void Reset() {
            //TODO: Underground belts should not spawn items above ground on reset...?
            if (thingOwnerInt.Any) {// remove them from care of thingOwner:
                this.products = new List<Thing>(thingOwnerInt.InnerListForReading);
                thingOwnerInt.Clear();
            }
            usingThingOwnerInt = false;
            base.Reset();
            this.products = thingOwnerInt.InnerListForReading;
            // Any of thse could come up in weird scenarios: remove them all?
            MapManager.RemoveAfterAction(CheckWork);
            MapManager.RemoveAfterAction(Placing);
            MapManager.RemoveAfterAction(StartWork);
            MapManager.RemoveAfterAction(FinishWork);
        }

        /// <summary>
        /// PlaceProduct - the AutoMachineTool product placement.
        /// This is marked `sealed` to make sure internal conveyor logic
        /// is not overridden or messed up.
        /// To override this, override ConveyorPlaceItem() below
        /// </summary>
        protected sealed override bool PlaceProduct(ref List<Thing> products) {
            if (thingOwnerInt.Count == 0) {
                // (this can happen if the belt is in Placing mode and something takes it from belt)
                Debug.Message(Debug.Flag.Conveyors, "Conveyor " + this + " no longer has anything to place!!");
                return true; // ready for next action.
            }
            // Has something else taken the Thing away from us? (b/c they are spawned? or something else?)
            // (I don't think anything is actually using this)
            if (this.WorkInterruption(products[0])) {
                Debug.Message(Debug.Flag.Conveyors, "  but something has already moved the item.");
                return true;
            }
            // this will continue to get called every ~30 ticks; this is okay. In case something goes wrong,
            //   it might fix itself.
            if (IsEndOfLine) return false;
            //ILogicSignalReciver
            if (LogicSignaStatus) return false;

            usingThingOwnerInt = true;
            Thing thing = thingOwnerInt.Take(thingOwnerInt[0]);
            usingThingOwnerInt = false;
            if (thing == null) return true; // trivially true?
            Debug.Warning(Debug.Flag.Conveyors, "Conveyor " + this +
                (stuck ? " is stuck with " : " is about to try placing ----- ") + thing);
            if (stuck) {
                thingOwnerInt.TryAdd(thing); // put it back
                TryUnstick(thing);
                return false; // whatever happens in TryUnstick, we're not placing *this* iteration
            }

            // Do Conveyor placement:
            if (ConveyorPlaceItem(thing)) return true;
            // 配置失敗.
            // Placement failure
            thingOwnerInt.TryAdd(thing); // put back
            Debug.Message(Debug.Flag.Conveyors, "    Could not place it. Stuck.");
            ChangeStuckStatus(thing, true);
            return false; // not ready for next work
        }

        /// <summary>
        /// Use this to actually place an item; you have full control over it
        /// return `false` to return control to belt
        /// </summary>
        protected virtual bool ConveyorPlaceItem(Thing thing)
        {
            // Try to send to another conveyor first:
            // コンベアある場合、そっちに流す.
            var outputBelt = this.OutputBeltAt(this.OutputCell());
            if (outputBelt != null) {
                if ((outputBelt as IPRF_Building).AcceptsThing(thing, this)) {
                    NotifyAroundSender();
                    this.stuck = false;
                    Debug.Message(Debug.Flag.Conveyors, " and successfully passed it to " + outputBelt);
                    return true;
                }
                Debug.Message(Debug.Flag.Conveyors, " but next belt cannot take it now; stuck.");
                // Don't try anything else if other belt is busy
            } else // if no conveyor, place if can
              {
                Debug.Message(Debug.Flag.Conveyors, "  trying to place at end of conveyor:");
                if (this.CanSendToLevel(ConveyorLevel.Ground) && this.PRFTryPlaceThing(thing,
                      this.OutputCell(), this.Map)) {
                    NotifyAroundSender();
                    this.stuck = false;
                    Debug.Message(Debug.Flag.Conveyors, "Successfully placed!");
                    return true;
                }
            }
            return false;
        }

        protected Thing CarryingThing()
        {
            if (this.State == WorkingState.Working)
            {
                return this.Working;
            }
            else if (this.State == WorkingState.Placing)
            {
                if (this.products == null || products.Count == 0) return null;
                return products[0];
            }
            return null;
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
                .Where(c => c.InBounds(this.Map))
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
            return working.holdingOwner != thingOwnerInt;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            products = this.products;
            return products.Count > 0;
        }

        protected override void CheckWork() {
            if (!this.Spawned) return; // maybe a callback hits this after it's destroyed?
            if (this.thingOwnerInt.Count==0 || thingOwnerInt[0] == null || this.IsEndOfLine) {
                Debug.Message(Debug.Flag.Conveyors, "      CheckWork: " + this + " empty or EndOfLine");
                return;
            }
            if (stuck) {
                Debug.Message(Debug.Flag.Conveyors, "      CheckWork: " + this + " am I still stuck?");
                if (CanOutput(working)) {
                    // start going forward again:
                    ChangeStuckStatus(working);
                }
            } else {
                Debug.Message(Debug.Flag.Conveyors, "      CheckWork: " + this + " checking if all is well");
                this.CalculateCarriedItemDrawHeight();
                // if work done is below stuckDrawPercent, let it continue forward
                if (WorkLeft < 1 - this.stuckDrawPercent) {
                    if (!CanOutput(working)) {
                        ChangeStuckStatus(working);
                        return;
                    }
                }
            }
            base.CheckWork();
        }
        protected virtual bool CanOutput(Thing t) {
            if (t == null) {
                return true; // Sure? Nothing to place, so can place it trivially.
            }
            var belt = this.OutputBeltAt(this.OutputCell());
            if (belt != null) {
                Debug.Message(Debug.Flag.Conveyors,
                    "  CanOutput: Testing can " + " accept " + t);
                return belt.CanAcceptNow(t);
            }
            Debug.Message(Debug.Flag.Conveyors, "    No belts can take " + t);
            if (!CanSendToLevel(ConveyorLevel.Ground)) return false;
            if (OutputToEntireStockpile) {
                SlotGroup slotGroup = OutputCell().GetSlotGroup(Map);
                if (slotGroup!=null) {
                    return PlaceThingUtility.CanPlaceThingInSlotGroup(t, slotGroup, Map);
                }
            }
            return PlaceThingUtility.CallNoStorageBlockersIn(OutputCell(), Map, t);
        }

        /// <summary>
        /// Attempt to change status from stuck to working
        /// </summary>
        /// <returns><c>true</c>, if unstuck, <c>false</c> otherwise.</returns>
        /// <param name="t">T.</param>
        protected virtual bool TryUnstick(Thing t)
        {
            // a simple check to see if we should be unstuck:
            if (!stuck) return true; // ??  Only call if stuck, please
            if (this.CanOutput(t)) {
                ChangeStuckStatus(t, false);
                return true;
            }
            return false;
        }
        protected void ChangeStuckStatus(Thing t, bool? willBeStuck = null) {
            if (willBeStuck == null) willBeStuck = !stuck;
            usingThingOwnerInt = true;
            thingOwnerInt.RemoveAll(thing=>true);
            usingThingOwnerInt = false;
            working = null;
            if ((bool)willBeStuck) {
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

        // Static Constructor: rgister with Settings ITab:
        static Building_BeltConveyor()
        {
            ITab_ProductionSettings.RegisterSetting(ShowEndOfLineSetting, HeightForEOLSetting, DoEOLSettingsWindowContents);
        }
        // End of Line setting: is the conveyor at the end of its line
        //   If it is, it should hold onto whatever it has unless something takes it?
        static bool ShowEndOfLineSetting(Thing t)
        {
            return (t is IBeltConveyorLinkable);
        }
        static float HeightForEOLSetting(Thing t)
        {
            return 23f;
        }
        static void DoEOLSettingsWindowContents(Thing t, Listing_Standard list)
        {
            if (t is IBeltConveyorLinkable belt) {
                bool tmp = belt.IsEndOfLine;
                list.CheckboxLabeled("PRF.Conveyor.IsEndOfLineSetting".Translate(), ref tmp, 
                                     "PRF.Conveyor.IsEndOfLineSetting.Desc".Translate());
                if (tmp != belt.IsEndOfLine) belt.IsEndOfLine = tmp;
            }
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
                return (defRotation != queryRotation);
            return false;
        }
    }
}
