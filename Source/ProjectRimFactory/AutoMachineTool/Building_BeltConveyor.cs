using System;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Common;
using ProjectRimFactory.Common.HarmonyPatches;
using RimWorld;
using UnityEngine;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

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
            return ("PRF.AutoMachineTool.Conveyor.DirectionPriority." + pri).Translate();
        }
    }

    /// <summary>
    ///     Conveyor Belts
    /// </summary>
    /// <designNotes>
    ///     Conveyor belts move an unspawned item from their position to
    ///     an adjacent one.  When their work is finished, the item is
    ///     placed.
    ///     Important note: If the item is ever spawned, it needs to be
    ///     either placed immediately, or despanwed before Placing()
    ///     happens - the products[] (from whence items are placed)
    ///     is saved Deep, not Reference, and if the item is spawned,
    ///     it will already be saved by the map.  This creates a small
    ///     window in which a problem can occur.
    /// </designNotes>
    public class Building_BeltConveyor : Building_BaseMachine<Thing>, IBeltConveyorLinkable, IThingHolder, IHideItem,
        IHideRightClickMenu, IForbidPawnOutputItem
    {
        // scale to draw items while on belts:
        //   Note: this is used if nothing is read from the ModExtension_Conveyors
        protected const float defaultCarriedItemScale = 0.75f;

        // additional height over the belt's True Center to draw:
        protected const float defaultCarriedItemDrawHeight = 0.15f;

        public static float supplyPower = 10f;
        private static int version = 2;

        // A few display constants:
        // how far towards the next belt to stop:
        protected readonly float stuckDrawPercent = 0.3f; // also slightly affects game logic

        // and display variables
        protected float carriedItemDrawHeight = defaultCarriedItemDrawHeight;
        protected bool stuck;

        protected ThingOwner_Conveyor thingOwnerInt;

        public Building_BeltConveyor()
        {
            setInitialMinPower = false;
            thingOwnerInt = new ThingOwner_Conveyor(this);
            // this is a horrible bastardization of AutoMachineTool and 
            //  RW's ThingOwner mechanism, made viable by C#'s list-by-
            //  reference mechanism.  It works great!
            products = thingOwnerInt.InnerListForReading;
            // Conveyors are dumb. They just dump their stuff onto the ground when they end!
            obeysStorageFilters = false;
        }

        // Generally useful methods:
        protected ModExtension_Conveyor Extension => def.GetModExtension<ModExtension_Conveyor>();

        // Generally important methods:
        protected virtual Rot4 OutputDirection => Rotation;

        public override float SupplyPowerForSpeed
        {
            get => supplyPower;

            set
            {
                supplyPower = value;
                RefreshPowerStatus();
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
        protected override LookMode WorkingLookMode
        {
            get
            {
                if (Scribe.mode == LoadSaveMode.Saving) return LookMode.Reference;
                if (version == 1) return LookMode.Deep;
                return LookMode.Reference;
            }
        }

        protected override LookMode ProductsLookMode
        {
            get
            {
                if (Scribe.mode == LoadSaveMode.Saving) return LookMode.Reference;
                if (version == 1) return LookMode.Deep;
                return LookMode.Reference;
            }
        }

        // We (LWM) are mean and don't allow conveyors to change the "Obey Storage Filters"
        //   setting.  Maybe if zymex is really nice we can change this....
        public override PRFBSetting SettingsOptions => base.SettingsOptions & ~PRFBSetting.optionObeysStorageFilters;

        /**************** IPRF_Building ***************/
        public override Thing GetThingBy(Func<Thing, bool> optionalValidator = null)
        {
            // TODO: underground?  Mod Setting?
            if (IsUnderground || State == WorkingState.Ready) return null;
            if (optionalValidator == null || optionalValidator(products[0]))
            {
                State = WorkingState.Placing;
                var t = thingOwnerInt.Take(products[0]);
                working = null;
                ForceReady();
                return t;
            }

            return null;
        }

        /************* Conveyors IBeltConveyor ***********/
        public bool IsStuck => stuck;

        public bool IsUnderground => Extension?.underground ?? false;

        // Note: it might be worth making link and unlink do something with a cached
        //   conveyor link; it will certainly be faster in some specific scenarios
        //   (underground belts going through a storehouse with DeepStorage or DSUs?)
        public virtual void Link(IBeltConveyorLinkable link)
        {
        }

        public virtual void Unlink(IBeltConveyorLinkable unlink)
        {
        }

        public virtual bool CanSendToLevel(ConveyorLevel level)
        {
            if (IsUnderground)
            {
                if (level == ConveyorLevel.Underground) return true;
            }
            else // on surface
            if (level == ConveyorLevel.Ground)
            {
                return true;
            }

            return false;
        }

        // Same for Conveyors:
        public virtual bool CanReceiveFromLevel(ConveyorLevel level)
        {
            return CanSendToLevel(level);
        }

        /// <summary>
        ///     Output directions used by the Linked Graphic for drawing little arrows
        /// </summary>
        public virtual IEnumerable<Rot4> ActiveOutputDirections
        {
            get { yield return Rotation; }
        }

        /// <summary>
        ///     External use only - default draw height for carried items
        /// </summary>
        public float CarriedItemDrawHeight => this.TrueCenter().y + defaultCarriedItemDrawHeight;

        /******** IPRF logic *********/
        public override bool AcceptsThing(Thing newThing, IPRF_Building giver = null)
        {
            Debug.Warning(Debug.Flag.Conveyors, "" + this + " was asked if it will accept " + newThing);
            if (!IsActive()) return false;
            // verify proper levels:
            if (giver is IBeltConveyorLinkable linkableGiver)
                if (!CanLinkFrom(linkableGiver, false))
                    return false;
            Debug.Message(Debug.Flag.Conveyors, "  It can accept items from " +
                                                (giver == null ? "that direction." : giver.ToString()));
            if (State == WorkingState.Ready)
            {
                // Note: I don't think there is any benefit to 
                //   an item being spanwed?  But what do I know?
                if (newThing.Spawned) newThing.DeSpawn();
                Debug.Message(Debug.Flag.Conveyors, "  And accepted " + newThing
                                                                      + (newThing.Spawned ? "" : " (not spanwed)"));
                newThing.Position = Position;
                stuck = false;
                ForceStartWork(newThing, 1f);
                return true;
            }

            var target = State == WorkingState.Working ? Working : products[0];
            Debug.Message(Debug.Flag.Conveyors, "  but busy with " + target +
                                                ". Will try to absorb");
            return target.TryAbsorbStack(newThing, true);
        }

        public bool CanAcceptNow(Thing thing)
        {
            Debug.Message(Debug.Flag.Conveyors, "  " + this + " was asked if it can accept " + thing);
            if (!IsActive())
            {
                Debug.Message(Debug.Flag.Conveyors, "      but it's not active and can't");
                return false;
            }

            switch (State)
            {
                case WorkingState.Ready:
                    Debug.Message(Debug.Flag.Conveyors, "    Ready state: yes");
                    return true;
                case WorkingState.Working:
                    return ThisCanAcceptThat(Working, thing);
                case WorkingState.Placing:
                    return ThisCanAcceptThat(products[0], thing);
                default:
                    return false;
            }
        }

        public virtual bool CanLinkTo(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition = true)
        {
            // First test: level (e.g., Ground vs Underground):
            var flag = false;
            // Loop through enum:
            //   (Seriously, C#, this is stupid syntax)
            foreach (var level in (ConveyorLevel[]) Enum.GetValues(typeof(ConveyorLevel)))
                if (CanSendToLevel(level) && otherBeltLinkable.CanReceiveFromLevel(level))
                {
                    flag = true;
                    break;
                }

            if (!flag) return false;
            if (!checkPosition) return true;
            // Conveyor Belts can link forward:
            return OutputCell() == otherBeltLinkable.Position;
        }

        public virtual bool CanLinkFrom(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition = true)
        {
            // First test: level (e.g., Ground vs Underground):
            var flag = false;
            // Loop through enum:
            //   (Seriously, C#, this is stupid syntax)
            foreach (var level in (ConveyorLevel[]) Enum.GetValues(typeof(ConveyorLevel)))
                if (CanReceiveFromLevel(level) && otherBeltLinkable.CanSendToLevel(level))
                {
                    flag = true;
                    break;
                }

            if (!flag) return false;
            if (!checkPosition) return true;
            // If you can dump it on a conveyor belt, it will take it.
            // But it does assume you are right next to the belt?
            // And it doesn't like it if something right in front of it tries
            //   to give it something - what is it going to do, give it back?
            if (OutputCell() == otherBeltLinkable.Position) return false;
            return Position.AdjacentToCardinal(otherBeltLinkable.Position);
        }

        public virtual bool HasLinkWith(IBeltConveyorLinkable otherBelt)
        {
            // TODO: should we cache these?
            return CanLinkTo(otherBelt) && otherBelt.CanLinkFrom(this)
                   || otherBelt.CanLinkTo(this) && CanLinkFrom(otherBelt);
        }

        public bool ForbidPawnOutput => !IsUnderground && State != WorkingState.Ready;

        /********* Display **********/
        public bool HideItems => !IsUnderground && State != WorkingState.Ready;

        /********** Pawn Interactions ********/
        public bool HideRightClickMenus => !IsUnderground && State != WorkingState.Ready;

        /********** IThingHolder ***********/
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            Enumerable.Empty<IThingHolder>();
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return thingOwnerInt;
        }

        // AutoMachineTool: Building_Base
        public override IntVec3 OutputCell()
        {
            return Position + OutputDirection.FacingCell;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            if (!products.NullOrEmpty())
            {
                var dropThing = products[0];
                yield return new Command_Action
                {
                    defaultLabel = "PRF_DropThing".Translate(dropThing.Label),
                    defaultDesc = "PRF_DropFromConveyorDesc".Translate(),
                    icon = (Texture2D) dropThing.Graphic.MatSingleFor(dropThing).mainTexture,
                    action = delegate
                    {
                        DropThing(dropThing);
                        Find.Selector.ClearSelection();
                        Find.Selector.Select(dropThing);
                    }
                };
            }
        }

        public void DropThing(Thing t)
        {
            if (products == null || !products.Contains(t))
            {
                //TODO: translate
                Messages.Message("Error: Conveyor " + this + " does not have " + t.Label,
                    this, MessageTypeDefOf.RejectInput);
                return; // should never happen?
            }

            thingOwnerInt.Take(t);
            Debug.Warning(Debug.Flag.Conveyors, "Conveyor " + this + " attempting to drop " + t);
            if (GenPlace.TryPlaceThing(t, Position, Map, ThingPlaceMode.Near, null,
                // no above-ground conveyors, impassable cells:
                delegate(IntVec3 c)
                {
                    if (c.Impassable(Map)) return false;
                    Debug.Message(Debug.Flag.Conveyors, "  validating " + c);
                    foreach (var jl in c.GetThingList(Map).OfType<IBeltConveyorLinkable>())
                    {
                        Debug.Message(Debug.Flag.Conveyors, "  but found " + jl);
                        if (!jl.IsUnderground)
                        {
                            Debug.Message(Debug.Flag.Conveyors, "  which is above ground, failed");
                            return false;
                        }
                    }

                    return true;
                }))
            {
                // successfully placed
                working = null;
                products.Remove(t);
                ForceReady();
            }
            else
            {
                // failed to place
                Messages.Message("PRF_CouldNotPlaceThing".Translate(t.Label),
                    this, MessageTypeDefOf.NegativeEvent);
                thingOwnerInt.TryAdd(t);
            }
        }

        public void Notify_LostItem(Thing item)
        {
            Debug.Warning(Debug.Flag.Conveyors, this + " was notified it has lost " + item);
            if (item.holdingOwner != thingOwnerInt && !thingOwnerInt.Any
                                                   && State != WorkingState.Ready // nothing happening
                                                   && State != WorkingState.Placing
            ) // already placing; should know it's gone
                Reset(); // something took it!
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving) version = 2;
            Scribe_Values.Look(ref version, "v", 1);
            base.ExposeData();

            Scribe_Values.Look(ref supplyPower, "supplyPower", 10f);
            Scribe_Deep.Look(ref thingOwnerInt, "thingOwner", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (thingOwnerInt == null) thingOwnerInt = new ThingOwner_Conveyor(this);
                if (version < 2)
                {
                    if (working != null) thingOwnerInt.TryAdd(working);
                    foreach (var t in products) thingOwnerInt.TryAdd(t);
                }

                products = thingOwnerInt.InnerListForReading;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            showProgressBar = false;
            foreach (var c in AllNearbyLinkables())
            {
                c.Link(this);
                Link(c);
            }

            // already set, but just in case:
            products = thingOwnerInt.InnerListForReading;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var targets = AllNearbyLinkables().ToList();
            base.DeSpawn(mode);

            targets.ForEach(x => x.Unlink(this));
        }

        // What does this even mean for a building, anyway?
        public override bool CanStackWith(Thing other)
        {
            return base.CanStackWith(other) && State == WorkingState.Ready;
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (IsUnderground && !OverlayDrawHandler_UGConveyor.ShouldDraw)
                // 地下コンベアの場合には表示しない.
                return;
            if (State != WorkingState.Ready && Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                var p = CarryPosition();
                Vector2 result = Find.Camera.WorldToScreenPoint(p + new Vector3(0, 0, -0.4f)) / Prefs.UIScale;
                result.y = UI.screenHeight - result.y;
                GenMapUI.DrawThingLabel(result, CarryingThing().stackCount.ToStringCached(),
                    GenMapUI.DefaultThingLabelColor);
            }
        }

        public override void Draw()
        {
            // Don't draw underground things by default:
            if (IsUnderground && !OverlayDrawHandler_UGConveyor.ShouldDraw)
                // 地下コンベアの場合には表示しない.
                return;
            base.Draw();
            if (State != WorkingState.Ready) DrawCarried();
        }

        /// <summary>
        ///     Draw the carried item (there should be one). This allows
        ///     derived classes to decide when/how to draw their items.
        /// </summary>
        public virtual void DrawCarried()
        {
            var t = CarryingThing();
            if (t == null) return;
            var scale = def.GetModExtension<ModExtension_Conveyor>()?.carriedItemScale ?? defaultCarriedItemScale;
            // Took this line from MinifiedThing; don't know if it's needed:
            //   However, it's insuffient to use Graphic's GetCopy() :(
            var g = t.Graphic.ExtractInnerGraphicFor(t);
            // Graphic's GetCopy() fails on any Graphic_RandomRotated
            //   And on minified things.  And who knows what else?
            //   There is no easy way to get around this, but this seems
            //   to work the best so far:
            var gd = t.def.graphicData;
            if (gd == null) gd = g.data;
            if (gd == null)
            {
                t.DrawAt(CarryPosition());
                return;
            }

            g = GraphicDatabase.Get(gd.graphicClass, gd.texPath, g.Shader,
                new Vector2(scale, scale), g.color, g.colorTwo);
            g.Draw(CarryPosition(), CarryingThing().Rotation, CarryingThing());
        }

        protected Vector3 CarryPosition()
        {
            if (stuck)
                return this.TrueCenter() + new Vector3(0, carriedItemDrawHeight, 0) +
                       OutputDirection.FacingCell.ToVector3()
                       * (stuckDrawPercent + Mathf.Clamp01(WorkLeft));
            return this.TrueCenter() + new Vector3(0, carriedItemDrawHeight, 0) +
                   OutputDirection.FacingCell.ToVector3()
                   * (1f - Mathf.Clamp01(WorkLeft));
        }

        protected void CalculateCarriedItemDrawHeight()
        {
            var nextBelt = OutputBeltAt(OutputCell());
            if (nextBelt != null)
            {
                var theirs = nextBelt.CarriedItemDrawHeight;
                var ours = CarriedItemDrawHeight;
                if (ours < theirs)
                {
                    //comparing whose is higher - what, are
                    //                    conveyor belts schoolboys?
                    carriedItemDrawHeight = theirs - this.TrueCenter().y;
                    return;
                }
            }

            carriedItemDrawHeight = defaultCarriedItemDrawHeight;
        }

        protected bool ThisCanAcceptThat(Thing t1, Thing t2)
        {
            return t1.CanStackWith(t2) && t1.stackCount < t1.def.stackLimit;
        }

        /******** AutoMachineTool logic *********/
        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            CalculateCarriedItemDrawHeight();
            workAmount = 1f;
            if (IsUnderground)
            {
                target = null;
                return false;
            }

            target = Position.GetThingList(Map).FirstOrDefault(t => t.def.EverHaulable);
            if (target != null)
            {
                if (target.Spawned) target.DeSpawn();
                target.Position = Position;
            }

            if (target != null) thingOwnerInt.TryAdd(target);
            return target != null;
        }

        protected override void ForceStartWork(Thing working, float workAmount)
        {
            CalculateCarriedItemDrawHeight();
            base.ForceStartWork(working, workAmount);
            thingOwnerInt.TryAdd(working);
        }

        protected override void Reset()
        {
            //TODO: Underground belts should not spawn items above ground on reset...
            if (thingOwnerInt.Any)
            {
                // remove them from care of thingOwner:
                products = new List<Thing>(thingOwnerInt.InnerListForReading);
                thingOwnerInt.Clear();
            }

            base.Reset();
            products = thingOwnerInt.InnerListForReading;
            // Any of thse could come up in weird scenarios: remove them all?
            MapManager.RemoveAfterAction(CheckWork);
            MapManager.RemoveAfterAction(Placing);
            MapManager.RemoveAfterAction(StartWork);
            MapManager.RemoveAfterAction(FinishWork);
        }

        protected override bool PlaceProduct(ref List<Thing> products)
        {
            //            var thing = products[0];
            //todo: test for work interruption first....
            if (thingOwnerInt.Count == 0)
            {
                Debug.Message(Debug.Flag.Conveyors, "Conveyor " + this + " no longer has anything to place");
                return true; // ready for next action.
            }

            // Has something else taken the Thing away from us? (b/c they are spawned? or something else?)
            if (WorkInterruption(products[0]))
            {
                Debug.Message(Debug.Flag.Conveyors, "  but something has already moved the item.");
                return true;
            }

            var thing = thingOwnerInt.Take(thingOwnerInt[0]);
            Debug.Warning(Debug.Flag.Conveyors, "Conveyor " + this +
                                                (stuck ? " is stuck with " : " is about to try placing ") + thing);
            if (stuck)
            {
                thingOwnerInt.TryAdd(thing);
                if (CanOutput(thing))
                {
                    ChangeStuckStatus(thing);
                    return false; // still not ready for new action
                }

                return false;
            }

            // Try to send to another conveyor first:
            // コンベアある場合、そっちに流す.
            var outputBelt = OutputBeltAt(OutputCell());
            if (outputBelt != null)
            {
                if (outputBelt.AcceptsThing(thing, this))
                {
                    NotifyAroundSender();
                    stuck = false;
                    Debug.Message(Debug.Flag.Conveyors, " and successfully passed it to " + outputBelt);
                    return true;
                }

                Debug.Message(Debug.Flag.Conveyors, " but next belt cannot take it now; stuck.");
                // Don't try anything else if other belt is busy
            }
            else // if no conveyor, place if can
            {
                Debug.Message(Debug.Flag.Conveyors, "  trying to place at end of conveyor:");
                if (CanSendToLevel(ConveyorLevel.Ground) && this.PRFTryPlaceThing(thing,
                    OutputCell(), Map))
                {
                    NotifyAroundSender();
                    stuck = false;
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

        protected Thing CarryingThing()
        {
            if (State == WorkingState.Working)
                return Working;
            if (State == WorkingState.Placing) return products?[0];
            return null;
        }

        /// <summary>
        ///     Return the first belt at <paramref name="location" /> that this can send to
        /// </summary>
        /// <returns>The belt, or null if none found</returns>
        /// <param name="location">Valid IntVec3 this conveyor can send to</param>
        protected virtual IBeltConveyorLinkable OutputBeltAt(IntVec3 location)
        {
            return location.GetThingList(Map)
                .OfType<IBeltConveyorLinkable>()
                .Where(b => CanLinkTo(b, false))
                .Where(b => b.CanLinkFrom(this))
                .FirstOrDefault();
        }

        protected IEnumerable<IBeltConveyorLinkable> AllNearbyLinkables()
        {
            return Enumerable.Range(0, 4).Select(i => Position + new Rot4(i).FacingCell)
                .SelectMany(c => c.GetThingList(Map))
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
            new[]
                {
                    Rotation.Opposite, Rotation.Opposite.RotateAsNew(RotationDirection.Clockwise),
                    Rotation.Opposite.RotateAsNew(RotationDirection.Counterclockwise)
                }
                .Select(r => Position + r.FacingCell)
                .SelectMany(p => p.GetThingList(Map).ToList())
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

        protected override void CheckWork()
        {
            if (working == null || thingOwnerInt.Count == 0) return;
            //TODO: Add test here :p
            if (stuck)
            {
                if (CanOutput(working)) // start going forward again:
                    ChangeStuckStatus(working);
            }
            else
            {
                CalculateCarriedItemDrawHeight();
                // if work done is below stuckDrawPercent, let it continue forward
                if (WorkLeft < 1 - stuckDrawPercent)
                    if (!CanOutput(working))
                    {
                        ChangeStuckStatus(working);
                        return;
                    }
            }

            base.CheckWork();
        }

        protected virtual bool CanOutput(Thing t)
        {
            if (t == null) return true; // Sure? Nothing to place, so can place it trivially.
            var belt = OutputBeltAt(OutputCell());
            if (belt != null)
            {
                Debug.Message(Debug.Flag.Conveyors,
                    "  CanOutput: Testing can " + " accept " + t);
                return belt.CanAcceptNow(t);
            }

            Debug.Message(Debug.Flag.Conveyors, "    No belts can take " + t);
            if (!CanSendToLevel(ConveyorLevel.Ground)) return false;
            if (OutputToEntireStockpile)
            {
                var slotGroup = OutputCell().GetSlotGroup(Map);
                if (slotGroup != null) return PlaceThingUtility.CanPlaceThingInSlotGroup(t, slotGroup, Map);
            }

            return PlaceThingUtility.CallNoStorageBlockersIn(OutputCell(), Map, t);
        }

        protected void ChangeStuckStatus(Thing t)
        {
            var willBeStuck = !stuck;
            thingOwnerInt.RemoveAll(thing => true);
            working = null;
            if (willBeStuck)
            {
                Debug.Message(Debug.Flag.Conveyors, this + " is now stuck with " + t);
                ForceStartWork(t, 1 - stuckDrawPercent
                                    - Mathf.Clamp01(WorkLeft));
                stuck = true;
            }
            else
            {
                Debug.Message(Debug.Flag.Conveyors, this + " is no longer stuck with " + t);
                ForceStartWork(t, 1 - stuckDrawPercent
                                    - Mathf.Clamp(WorkLeft, 0, 1 - stuckDrawPercent));
                stuck = false;
            }
        }

        public Thing Carrying()
        {
            if (State == WorkingState.Working)
                return Working;
            if (State == WorkingState.Placing) return products.FirstOption().GetOrDefault(null);
            return null;
        }

        public static bool CanDefSendToRot4AtLevel(ThingDef def, Rot4 defRotation,
            Rot4 queryRotation, ConveyorLevel queryLevel)
        {
            // Not going to error check here: if there's a config error, there will be prominent
            //   red error messages in the log.
            if (queryLevel == ConveyorLevel.Underground)
            {
                if (def.GetModExtension<ModExtension_Conveyor>()?.underground != true)
                    return false;
            }
            else
            {
                // Ground
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
            Rot4 queryRotation, ConveyorLevel queryLevel)
        {
            if (queryLevel == ConveyorLevel.Ground &&
                def.GetModExtension<ModExtension_Conveyor>()?.underground != true
                || queryLevel == ConveyorLevel.Underground &&
                def.GetModExtension<ModExtension_Conveyor>()?.underground == true)
                return defRotation != queryRotation.Opposite;
            return false;
        }
    }
}