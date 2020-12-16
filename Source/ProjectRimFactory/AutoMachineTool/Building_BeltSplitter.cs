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
    /***************************************
     *  The belt splitter has several features normal conveyor belts don't have.
     *   * Omnidirectional - it can take in links from any direction and give them out
     *     in any direction.
     *   * It caches valid output links and can even turn them off (block output to 
     *     them entirely)
     *   * It switches where the next output item goes each time (splitting input
     *     items among output lines)
     *     * NOTE: this may need further consideration with multiple inputs
     *   * Its graphic has an extra building on top of it.
     */
    public class Building_BeltSplitter : Building_BeltConveyor
    {
        private Rot4 dest = Rot4.Random; // start in random direction if more than one available

        public Dictionary<Rot4, OutputLink> OutputLinks => outputLinks;
        public IEnumerable<IBeltConveyorLinkable> IncomingLinks => incomingLinks;
        private Dictionary<Rot4, OutputLink> outputLinks = new Dictionary<Rot4, OutputLink>();
        [Unsaved]
        private HashSet<IBeltConveyorLinkable> incomingLinks = new HashSet<IBeltConveyorLinkable>();

        public Building_BeltSplitter() : base() {
            // Conveyors are dumb. They just dump their stuff onto the ground when they end!
            //   But splitters are smart, they can figure stuff out:
            this.obeysStorageFilters = true;
        }
        public override PRFBSetting SettingsOptions { // allow player to change this at playtime:
            get => base.SettingsOptions | PRFBSetting.optionObeysStorageFilters;
        }

        protected override Rot4 OutputDirection => dest;
        public override IEnumerable<Rot4> ActiveOutputDirections {
            get {
                foreach (var kvp in outputLinks.Where(kvp=>kvp.Value.Active)) {
                    yield return kvp.Key;
                }
            }
        }

        // TODO: revisit:
        //public bool HideItems => !this.IsUnderground && this.State != WorkingState.Ready;
        //public bool HideRightClickMenus => !this.IsUnderground && this.State != WorkingState.Ready;
        //public bool ForbidPawnOutput => !this.IsUnderground && this.State != WorkingState.Ready;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.dest, "dest");
            Scribe_Collections.Look(ref this.outputLinks, "outputLinks", LookMode.Value, LookMode.Deep);
        }

        /*public override void PostMapInit()
        {
            // maybe this should do the Link() stuff SpawnSetup does?
            base.PostMapInit();
        }*/

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.showProgressBar = false;
            //outputLinks.Clear(); // TOD\O <) ??
            incomingLinks.Clear();

            var links = AllNearbyLinks().ToList();
            foreach (var c in AllNearbyLinks()) {
                c.Link(this);
                this.Link(c);
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (var c in this.AllNearbyLinkables()) {
                c.Unlink(this);
            }
            base.DeSpawn(mode);

            outputLinks.Clear();
            incomingLinks.Clear();
        }
        /// <summary>
        /// If the graphic changes - for example, the number of arrows for output
        ///    we probably need to refresh the graphic.  So, call this from Link,
        ///    Unlink, and anything that changes whether links are active.
        /// </summary>
        protected void UpdateGraphic() {
            if (Spawned) this.Map.mapDrawer.MapMeshDirty(this.Position,
                MapMeshFlag.Buildings | MapMeshFlag.Things);
        }

        // What does this even mean?
        public override bool CanStackWith(Thing other)
        {
            return base.CanStackWith(other) && this.State == WorkingState.Ready;
        }

        public override bool AcceptsThing(Thing newThing, IPRF_Building giver = null) {
            var origState = this.State;
            if (base.AcceptsThing(newThing, giver) && origState == WorkingState.Ready) {
                NextDirection(newThing, out dest);
                Debug.Message(Debug.Flag.Conveyors, "  Spitter " + this 
                              + " decided " + newThing + " should go " + dest.ToStringHuman());
                return true;
            }
            return false;
        }
        /// <summary>
        /// Try to find a conveyor/location to pass on the next item to.
        /// </summary>
        private bool NextDirection(Thing t, out Rot4 newDir) {
            foreach (var dir in NextDirectionByPriority(t)) {
                var olink = outputLinks.TryGetValue(dir, null);
                if (olink != null) {
                    if (olink.IsValidOutputLinkFor(t)) {
                        newDir = dir;
                        return true;
                    }
                }/* else { // just a spot on the ground?
                    if (!PlaceThingUtility.CallNoStorageBlockersIn(this.Position + dir.FacingCell,
                                this.Map, t)) continue;
                }*/
            }
            newDir = this.Rotation;  // slightly better than Rot4.Random, I suppose :p
            return false; // oh well. Fail.
        }
        /// <summary>
        /// Suggest the next directions to try placing an item; direction will switch between
        /// valid directions given filters
        /// </summary>
        /// <returns>The highest priority direction to try sending something.</returns>
        private IEnumerable<Rot4> NextDirectionByPriority(Thing t) {
            // We need to try each direction in decreasing priority, and for each 
            //   priority, we need to check each direction that matches it.
            //   I'm sure there is a more elegant solution to this that doesn't
            //   involve going through each direction, but this works, isn't too
            //   slow, and I'm sure there are no mistakes here?
            var previousDir = new Rot4(dest.AsInt);
            var prevPriority = this.outputLinks.TryGetValue(previousDir, null)?.priority;
            foreach (var priority in ((DirectionPriority[])Enum
                     .GetValues(typeof(DirectionPriority))).Reverse()) {
                // I will rotate counterclockwise because that is the "positive" direction
                Rot4 nextDir = previousDir.RotateAsNew(RotationDirection.Counterclockwise);
                for (int i = 0; i < 4; i++, nextDir.Rotate(RotationDirection.Counterclockwise)) {
                    if ( // we look for an appropriate next direction:
                                nextDir != previousDir &&
                                this.outputLinks.ContainsKey(nextDir) &&
                                outputLinks[nextDir].priority == priority &&
                                outputLinks[nextDir].Allows(t))
                        //                                this.priorities[nextDir] == priority &&
                        //                                this.filters[nextDir].Allows(t)) { 
                        yield return nextDir;
                }
                // if prevPriority == null, it means we don't have a valid link there
                //   and we won't return that direction.
                if (priority == prevPriority) yield return previousDir;
            }
        }
        protected override IBeltConveyorLinkable OutputBeltAt(IntVec3 location) {
            foreach (var kvp in this.outputLinks) {
                if (kvp.Key.FacingCell + this.Position == location) {
                    if (!kvp.Value.Active) return null;
                    return kvp.Value.link;
                }
            }
            return null;
        }
        protected override bool CanOutput(Thing t) {
            if (!outputLinks.ContainsKey(dest)) return false;
            return outputLinks[dest].IsValidOutputLinkFor(t);
        }

        protected override bool PlaceProduct(ref List<Thing> products)
        {
            var thing = thingOwnerInt.Take(products[0]);
            Debug.Warning(Debug.Flag.Conveyors, "Splitter " + this + " is about to try placing " + thing);
            if (this.WorkInterruption(thing))
            {
                Debug.Message(Debug.Flag.Conveyors, "   But interrupted; failing");
                return true;
            }
            var output = outputLinks.TryGetValue(dest, null);
            if (output != null && output.Allows(thing)) {
                if (output.TryPlace(thing)) {
                    NotifyAroundSender();
                    this.stuck = false;
                    return true;
                }
            }
            // If we have failed to place, look for another direction:
            Debug.Message(Debug.Flag.Conveyors, "" + this + "Failed to place " + dest.ToStringHuman() +
                                              "; looking for another direction");
            if (NextDirection(thing, out Rot4 tmp))
            {
                Debug.Message(Debug.Flag.Conveyors, "  going to try new direction " + dest.ToStringHuman());
                // 他に流す方向があれば、やり直し.
                // If there is another direction, try again.
                this.Reset();
                this.AcceptsThing(thing, this); // should always work
                return false;
            }
            // 配置失敗.
            // Placement failure
            this.stuck = true;
            thingOwnerInt.TryAdd(thing);//put it back
            Debug.Message(Debug.Flag.Conveyors, "" + this + ": Is stuck.");
            return false;
        }

        /***************** Outgoing/Incoming Links ********************/
        public override void Link(IBeltConveyorLinkable link)
        {
            if (this.CanLinkTo(link) && link.CanLinkFrom(this)) {
                if (PositionToRot4(link, out Rot4 r)) {
                    UpdateGraphic();
                    if (outputLinks.ContainsKey(r)) {
                        outputLinks[r].link = link;
                    } else {
                        this.outputLinks[r] = new OutputLink(this, link);
                    }
                }
            }
            if (this.CanLinkFrom(link) && link.CanLinkTo(this)) {
                incomingLinks.Add(link);
            }
        }
        // Utility fn for linking to belt link
        private bool PositionToRot4(IBeltConveyorLinkable link, out Rot4 r) {
            foreach (var d in Enumerable.Range(0,4).Select(i=>new Rot4(i))) {
                if (this.Position+d.FacingCell == link.Position) {
                    r = d;
                    return true;
                }
            }
            r = Rot4.Random;
            return false;
        }
        public override void Unlink(IBeltConveyorLinkable unlink)
        {
            incomingLinks.Remove(unlink);
            var tmpl = outputLinks.Where(kvp => kvp.Value.link == unlink)
                .Select(kvp => kvp.Key).ToList();
            foreach (var k in tmpl) {
                outputLinks.Remove(k);
            }
            UpdateGraphic();
        }
        public void AddOutgoingLink(Rot4 dir) {
            // Assume that if it had a linked belt, the link would have formed via
            //   the Link() method. So this is activating a link to a direction. I hope.
            if (outputLinks.ContainsKey(dir)) {
                outputLinks[dir].Active = true;
                return;
            }
            outputLinks[dir] = new OutputLink(this, Position + dir.FacingCell);
            UpdateGraphic();
        }


        protected IEnumerable<IBeltConveyorLinkable> AllNearbyLinks() {
            return AllNearbyLinkables()
                .Where(b => (this.CanLinkTo(b) && b.CanLinkFrom(this))
                         || (this.CanLinkFrom(b) && b.CanLinkTo(this)));
        }

        protected IBeltConveyorLinkable BeltLinkableAt(IntVec3 location)
        {
            return location.GetThingList(this.Map)
                .OfType<IBeltConveyorLinkable>()
                .Where(b => this.CanLinkTo(b))
                .FirstOrDefault(b => b.CanLinkFrom(this));
        }

        //TODO: <)?
        private void NotifyAroundSender() //TODO: rotation directions okay?
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
            return false; // TODO: this would assume the working thing is spawned?
            // todo: should this happen if something takes the belt's item?
            //return this.IsUnderground ? false : !working.Spawned || working.Position != this.Position;
        }

        //TODO: I think this should just return false?
        //  Because TryStartWorking is if something falls on top of it, right?
        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            target = null;
            workAmount = 0f;
            return false;
            #if false
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
                NextDirection(target, out this.dest);
                if (target.Spawned && this.IsUnderground) target.DeSpawn();
                target.Position = this.Position;
            }
            return target != null;
            #endif
        }

        public override bool CanLinkTo(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition=true) {
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
            // Allow to link in any direction (including backwards, yes - who knows what direction will be appropriate?):
            return this.Position.IsNextTo(otherBeltLinkable.Position);
        }
        public override bool CanLinkFrom(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition=true) {
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
            // Allow links from any direction (including backwards, yes)
            return Position.IsNextTo(otherBeltLinkable.Position);
        }
        public override bool HasLinkWith(IBeltConveyorLinkable otherBelt) {
            return incomingLinks.Contains(otherBelt) ||
                outputLinks.Any(kvp => kvp.Value.link == otherBelt);
        }

        //TODO: this is all probably wrong?
        new public static bool CanDefSendToRot4AtLevel(ThingDef def, Rot4 defRotation,
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
            return true;
        }
        /*public static IEnumerable<Rot4> AllRot4DefCanSendToAtLevel(ThingDef def, Rot4 defRotation,
            ConveyorLevel level) {
            if (level == ConveyorLevel.Underground &&
                 def.GetModExtension<ModExtension_Conveyor>().underground)
                yield return new Rot4(defRotation.AsInt);
        }*/
        new public static bool CanDefReceiveFromRot4AtLevel(ThingDef def, Rot4 defRotation,
                      Rot4 queryRotation, ConveyorLevel queryLevel) {
            if ((queryLevel == ConveyorLevel.Ground &&
                 def.GetModExtension<ModExtension_Conveyor>()?.underground != true)
                || (queryLevel == ConveyorLevel.Underground &&
                    def.GetModExtension<ModExtension_Conveyor>()?.underground == true))
                return true;
            return false;
        }
        /// <summary> /////////////////////////////////////////////////////////////////
        /// A class to handle each output link, with filters, knowing belts, etc.
        /// </summary>
        public class OutputLink : IExposable {
            public OutputLink(Building_BeltSplitter parent, IBeltConveyorLinkable link) 
                  : this(parent,link.Position) {
                this.link = link;
            }
            public OutputLink(Building_BeltSplitter parent, IntVec3 pos) {
                this.parent = parent;
                link = null;
                this.position = pos;
                // TODO: any way to make filter null unless it's actually needed?
                //   maybe not?
                filter = new ThingFilter();
                filter.SetAllowAll(null);
            }
            public OutputLink() {
                // This is used purely so ExposeData can create these things
                //   when loading a saved game.
                // Do not actually use this constructor.
            }
            public void ExposeData() {
                Scribe_Values.Look(ref priority, "PRFB_priority", DirectionPriority.Normal);
                //TODO: only write filter if it's actually interesting?
                Scribe_Deep.Look(ref filter, "PRFBSL_filter", Array.Empty<object>());
                Scribe_References.Look(ref link, "PRFBSL_link");
                Scribe_Values.Look(ref active, "PRFBSL_active");
                Scribe_Values.Look(ref position, "PRFBSL_position");
                Scribe_References.Look(ref parent, "PRFBSL_parent", false);
            }
            public bool Active {
                get => active;
                set {
                    if (active == value) return;
                    active = value;
                    parent.UpdateGraphic();
                }
            }
            public bool Allows(Thing t) {
                return Active && (filter == null || filter.Allows(t));
            }
            public bool IsValidOutputLinkFor(Thing t) {
                if (!Allows(t)) return false;
                if (link != null) return link.CanAcceptNow(t);
                return PlaceThingUtility.CallNoStorageBlockersIn(position, 
                                            parent.Map, t);
            }
            public bool TryPlace(Thing thing) {
                // Try to send to another conveyor first:
                // コンベアある場合、そっちに流す.
                if (link != null) {
                    Debug.Message(Debug.Flag.Conveyors, "" + this + ": found " + link +
                                                        "; going to try passing it along");
                    if ((link as IPRF_Building).AcceptsThing(thing, parent)) {
                        Debug.Message(Debug.Flag.Conveyors, "" + this +
                                      ": and successfully passed it to " + link);
                        return true;
                    }
                } else {// if no conveyor, place if can
                    Debug.Message(Debug.Flag.Conveyors, "" + this + ": trying to place directly:");
                    if (!parent.IsUnderground && parent.PRFTryPlaceThing(thing,
                        position, parent.Map)) {
                        Debug.Message(Debug.Flag.Conveyors, "" + this + "Successfully placed!");
                        return true;
                    }
                }
                return false;
            }
            public void CopyAllowancesFrom(ThingFilter o) {
                if (this.filter == null) filter = new ThingFilter();
                filter.CopyAllowancesFrom(o);
            }
            public void DoThingFilterConfigWindow(Rect r, ref Vector2 sp) {
                if (filter == null) {
                    filter = new ThingFilter();
                    filter.SetAllowAll(null);
                }
                ThingFilterUI.DoThingFilterConfigWindow(r, ref sp, this.filter);
            }
            public IBeltConveyorLinkable link;
            public DirectionPriority priority=DirectionPriority.Normal;
            private ThingFilter filter = null;
            private bool active = true;
            private IntVec3 position = IntVec3.Invalid;
            private Building_BeltSplitter parent;
        }
    }
    public static class StupidCSharpStaticClassRequirements
    {
        public static bool IsNextTo(this IntVec3 one, IntVec3 two)
        {
            int t;
            if (one.x == two.x) {
                t = one.z - two.z;
                return t == 1 || t == -1;
            }
            if (one.z == two.z) {
                t = one.x - two.x;
                return t == 1 || t == -1;
            }
            return false;
        }
    }
}
