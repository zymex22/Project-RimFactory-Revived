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
    public class Building_BeltSplitter : Building_BeltConveyor
    {
        private Rot4 dest = Rot4.Random; // start in random direction if more than one available
//        private Dictionary<Rot4, ThingFilter> filters = new Dictionary<Rot4, ThingFilter>();
//        private Dictionary<Rot4, DirectionPriority> priorities = new Rot4[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }.ToDictionary(d => d, _ => DirectionPriority.Normal);

        //TODO: Maybe save this?
//        [Unsaved]
//        private int round = 0;

//        [Unsaved]
//        private List<Rot4> outputRot = new List<Rot4>();

        [Unsaved]
        private Dictionary<Rot4, OutputLink> outputLinks = new Dictionary<Rot4, OutputLink>();
        public Dictionary<Rot4, OutputLink> OutputLinks => outputLinks;

        [Unsaved]
        private HashSet<IBeltConveyorLinkable> incomingLinks = new HashSet<IBeltConveyorLinkable>();

        protected override Rot4 OutputDirection => dest;

        public IEnumerable<Rot4> AllOutputDirs => this.outputLinks.Keys;

        // Conveyors are dumb. They just dump their stuff onto the ground when they end!
        //   But splitters are smart, they can figure stuff out:
        public override bool ObeysStorageFilters => true;
//        public Dictionary<Rot4, ThingFilter> Filters { get => this.filters; }
//        public Dictionary<Rot4, DirectionPriority> Priorities { get => this.priorities; }

        // TODO: revisit:
        //public bool HideItems => !this.IsUnderground && this.State != WorkingState.Ready;
        //public bool HideRightClickMenus => !this.IsUnderground && this.State != WorkingState.Ready;
        //public bool ForbidPawnOutput => !this.IsUnderground && this.State != WorkingState.Ready;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.dest, "dest");
//            Scribe_Values.Look(ref this.previousDir, "PRF_prevDest");
            Scribe_Collections.Look(ref this.outputLinks, "outputLinks", LookMode.Value, LookMode.Deep);
/*            Scribe_Collections.Look(ref this.filters, "filters", LookMode.Value, LookMode.Deep);
            if (this.filters == null)
            {
                this.filters = new Dictionary<Rot4, ThingFilter>();
            }
            Scribe_Collections.Look(ref this.priorities, "priorities", LookMode.Value, LookMode.Value);
            if (this.priorities == null)
            {
                this.priorities = new Rot4[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }.ToDictionary(d => d, _ => DirectionPriority.Normal);
            }
            */
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            
//            this.FilterSetting();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.showProgressBar = false;
            outputLinks.Clear();
            incomingLinks.Clear();

            var links = AllNearbyLinks().ToList();
            foreach (var c in AllNearbyLinks()) {
                c.Link(this);
                this.Link(c);
            }
/*            if (!respawningAfterLoad)
            {
                //TODO: ???
                if (links.Count==0)
                    this.FilterSetting();
            }*/
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (var c in outputLinks.Values.Select(l=>l.link).Union(incomingLinks)) {
                c.Unlink(this);
            }
            base.DeSpawn(mode);

            outputLinks.Clear();
            incomingLinks.Clear();
        }

        protected override void Reset()
        {
/*            if (this.State != WorkingState.Ready)
            {
                // TODO:<)
                this.FilterSetting();
            }
            base.Reset();
            */          
        }

        //TODO:<)
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

        //TODO:<)
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

        // What does this even mean?
        public override bool CanStackWith(Thing other)
        {
            return base.CanStackWith(other) && this.State == WorkingState.Ready;
        }

        //TODO: test this:
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
                    if (!olink.link.CanAcceptNow(t)) continue;
                } else { // just a spot on the ground?
                    if (!PlaceThingUtility.CallNoStorageBlockersIn(this.Position + dir.FacingCell,
                                this.Map, t)) continue;
                }
                newDir = dir;
                return true;
            }
            //??
            if (outputLinks.ContainsKey(this.dest)) {
                newDir = dest;
                return true;
            }
//            newDir = this.Rotation;//TODO:
            newDir = dest;  // slightly better than Rot4.Random, I suppose :p
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
            foreach (var priority in (DirectionPriority[])Enum.GetValues(typeof(DirectionPriority))) {
                Log.Message("" + this + " Checking priority " + priority);
                // I will rotate counterclockwise because that is the "positive" direction
                Rot4 nextDir = previousDir.RotateAsNew(RotationDirection.Counterclockwise);
                for (int i = 0; i < 4; i++, nextDir.Rotate(RotationDirection.Counterclockwise)) {
                    if (nextDir != this.Rotation.Opposite && // don't look backwards
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

/*        private Rot4 Destination(Thing t, bool doRotate)
        {
            var conveyors = this.OutputBeltConveyor();
            var allowed = this.filters
                .Where(f => f.Value.Allows(t.def)).Select(f => f.Key)
                .ToList();
            List<IBeltConveyorLinkable> placeable = allowed.Where(r => this.outgoingLinks.Where(l => l.Position == this.Position + r.FacingCell).FirstOption().Select(b => b.ReceivableNow(this.IsUnderground, t) || !b.IsStuck).GetOrDefault(true))
                .ToList();

            if (placeable.Count == 0)
            {
                if(allowed.Count == 0)
                {
                    placeable = this.OutputRots.Where(r =>
                        conveyors
                            .Where(l => l.Position == this.Position + r.FacingCell)
                            .FirstOption()
//                            .Select(b => b.ReceivableNow(this.IsUnderground, t) || !b.IsStuck)
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
*/
/*        private bool SendableConveyor(Thing t, out Rot4 dir)
        {
            dir = default(Rot4);
            var result = this.filters
                .Where(f => f.Value.Allows(t.def))
                .Select(f => f.Key)
                .SelectMany(r => this.OutputBeltConveyor()
                                 .Where(l => l.Position == this.Position + r.FacingCell)
                                 .Select(b => new { Dir = r, Conveyor = b }))
                .Where(b => b.Conveyor.ReceivableNow(this.IsUnderground, t))
                .FirstOption();
            if (result.HasValue)
            {
                dir = result.Value.Dir;
            }
            return result.HasValue;
        }
        */
        protected override bool PlaceProduct(ref List<Thing> products)
        {
            var thing = products[0];
            Debug.Warning(Debug.Flag.Conveyors, "Splitter " + this + " is about to try placing " + thing);
            if (this.WorkInterruption(thing))
            {
                Debug.Message(Debug.Flag.Conveyors, "   But interrupted; failing");
                return true;
            }
            var output = outputLinks.TryGetValue(dest, null);
            if (output != null && output.Allows(thing)) {
                // Try to send to another conveyor first:
                // コンベアある場合、そっちに流す.
                if (output.link != null) {
                    Debug.Message(Debug.Flag.Conveyors, "" + this + ": found " + output.link +
                                                        "; going to try passing it along");
                    if ((output.link as IPRF_Building).AcceptsThing(thing, this)) {
                        NotifyAroundSender();
                        this.stuck = false;
                        Debug.Message(Debug.Flag.Conveyors, "" + this +
                                      ": and successfully passed it to " + output.link);
                        return true;
                    }
                } else // if no conveyor, place if can
                  {
                    Debug.Message(Debug.Flag.Conveyors, "" + this + ": trying to place directly:");
                    if (!this.IsUnderground && this.PRFTryPlaceThing(thing,
                          this.dest.FacingCell + this.Position, this.Map)) {
                        NotifyAroundSender();
                        this.stuck = false;
                        Debug.Message(Debug.Flag.Conveyors, "" + this + "Successfully\t placed!");
                        return true;
                    }
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
            Debug.Message(Debug.Flag.Conveyors, "" + this + ": Is stuck.");
            return false;
        }

        public override void Link(IBeltConveyorLinkable link)
        {
            if (this.CanLinkTo(link) && link.CanLinkFrom(this)) {
                if (PositionToRot4(link, out Rot4 r)) {
                    this.outputLinks[r] = new OutputLink(link);
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
        }

/*        private void FilterSetting()
        {
            Func<ThingFilter> createNew = () =>
            {
                var f = new ThingFilter();
                f.SetAllowAll(null);
                return f;
            };
            var output = this.OutputBeltConveyor();
            this.filters = Enumerable.Range(0, 3).Select(x => new Rot4(x))
                .Select(x => new { Rot = x, Pos = this.Position + x.FacingCell })
                .Where(x => output.Any(l => l.Position == x.Pos) || this.Rotation == x.Rot)
                .ToDictionary(r => r.Rot, r => this.filters.ContainsKey(r.Rot) ? this.filters[r.Rot] : createNew());
            if(this.filters.Count <= 1)
            {
                this.filters.ForEach(x => x.Value.SetAllowAll(null));
            }
            this.outputRot = this.filters.Select(x => x.Key).ToList();
        }*/

        protected IEnumerable<IBeltConveyorLinkable> AllNearbyLinks() {
            return AllNearbyLinkables()
                .Where(b => (this.CanLinkTo(b) && b.CanLinkFrom(this))
                         || (this.CanLinkFrom(b) && b.CanLinkTo(this)));
        }

        protected IBeltConveyorLinkable BeltLinkableAt(IntVec3 location)
        {
            return location.GetThingList(this.Map)
                .Where(t => t is IBeltConveyorLinkable)
                .Select(t => t as IBeltConveyorLinkable)
                .Where(b=>this.CanLinkTo(b))
                .Where(b=>b.CanLinkFrom(this))
                .FirstOrDefault();
        }

/*        private IEnumerable<IBeltConveyorLinkable> LinkTargetConveyor()
        {
            return Enumerable.Range(0, 3).Select(i => this.Position + new Rot4(i).FacingCell)
                .SelectMany(c => c.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => CanLink(this, t, this.def, t.def))
                .Select(t => (t as IBeltConveyorLinkable));
        }

        private IEnumerable<IBeltConveyorLinkable> OutputBeltConveyor()
        {
            var links = this.LinkTargetConveyor();
            return links.Where(x =>
                    (x.Rotation.Opposite.FacingCell + x.Position == this.Position && x.Position != this.Position + this.Rotation.Opposite.FacingCell) ||
                    (x.Rotation.Opposite.FacingCell + x.Position == this.Position && links.Any(l => l.Position + l.Rotation.FacingCell == this.Position))
                );
        }*/

            /*Superceded by Acceptable(Thing) and CanLinkTo/From
        public bool Acceptable(Rot4 rot, bool underground)
        {
            return rot != this.Rotation && this.IsUnderground == underground;
        }

        public bool ReceivableNow(bool underground, Thing thing)
        {
            if(!this.IsActive())//TODO: || this.IsUnderground != underground)
            {
                return false;
            }
            // TODO: C#-ify this?
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
        */

        //TODO: <)?
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
            return false; // TODO: this would assume the working thing is spawned?
            //return this.IsUnderground ? false : !working.Spawned || working.Position != this.Position;
        }

        //TODO: I think this should just return false?
        //  Because TryStartWorking is if something falls on top of it, right?
        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            target = null;
            workAmount = 0f;
            return false;
/*            workAmount = 1f;
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
            return target != null;*/
        }

        /*protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            products = new List<Thing>().Append(working);
            return true;
        }*/

        //???TODO: <)
        protected override bool WorkingIsDespawned()
        {
            return true;
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
            // Conveyor Belts can link forward, right, and left:
            if (this.Position + this.Rotation.FacingCell == otherBeltLinkable.Position ||
                this.Position + this.Rotation.RighthandCell == otherBeltLinkable.Position ||
                // Why is there no LefthandCell? Annoying.
                this.Position + this.Rotation.Opposite.RighthandCell == otherBeltLinkable.Position)
                return true;
            return false;
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
            // Conveyor belts can receive only from directly behind:
            if (this.Position + this.Rotation.Opposite.FacingCell == otherBeltLinkable.Position)
                return true;
            return false;
        }
        public override bool HasLinkWith(IBeltConveyorLinkable otherBelt) {
            return incomingLinks.Contains(otherBelt) ||
                outputLinks.Any(kvp => kvp.Value.link == otherBelt);
        }

        new public static bool CanDefSendToRot4AtLevel(ThingDef def, Rot4 defRotation,
                     Rot4 queryRotation, ConveyorLevel queryLevel) {
            // Not going to error check here: if there's a config error, there will be prominent
            //   red error messages in the log.
            if (queryLevel == ConveyorLevel.Underground) {
                if (def.GetModExtension<ModExtension_Conveyor>().underground != true)
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
                 def.GetModExtension<ModExtension_Conveyor>().underground)
                yield return new Rot4(defRotation.AsInt);
        }*/
        new public static bool CanDefReceiveFromRot4AtLevel(ThingDef def, Rot4 defRotation,
                      Rot4 queryRotation, ConveyorLevel queryLevel) {
            if ((queryLevel == ConveyorLevel.Ground &&
                 def.GetModExtension<ModExtension_Conveyor>()?.underground != true)
                || (queryLevel == ConveyorLevel.Underground &&
                    def.GetModExtension<ModExtension_Conveyor>()?.underground == true))
                return (defRotation != queryRotation.Opposite);
            return false;
        }

        public class OutputLink : IExposable {
            public OutputLink(IBeltConveyorLinkable l) {
                link = l;
                // TODO: any way to make filter null unless it's actually needed?
                //   maybe not?
                filter = new ThingFilter();
                filter.SetAllowAll(null);
            }
            public void ExposeData() {
                // Skip saving any broken output links:
                if (Scribe.mode == LoadSaveMode.Saving &&
                    link != null && !link.Spawned) return;
                Scribe_Values.Look(ref priority, "PRFB_priority", DirectionPriority.Normal);
                //TODO: only write filter if it's actually interesting?
                Scribe_Deep.Look(ref filter, "PRFB_filter", Array.Empty<object>());
                Scribe_References.Look(ref link, "PRFB_link");
            }
            public bool Allows(Thing t) {
                return (filter == null || filter.Allows(t));
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
        }
    }
}
