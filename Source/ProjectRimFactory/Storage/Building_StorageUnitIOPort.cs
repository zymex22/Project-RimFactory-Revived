using ProjectRimFactory.Common.HarmonyPatches;
using ProjectRimFactory.Storage.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage
{

    [StaticConstructorOnStartup]
    public abstract class Building_StorageUnitIOBase : Building_Storage, IForbidPawnInputItem, IRenameable
    {
        private static readonly Texture2D CargoPlatformTex = ContentFinder<Texture2D>.Get("Storage/CargoPlatform");
        protected static readonly Texture2D IOModeTex = ContentFinder<Texture2D>.Get("PRFUi/IoIcon");

        public StorageIOMode Mode;
        private Building linkedStorageParentBuilding;
        public ILinkableStorageParent boundStorageUnit => linkedStorageParentBuilding as ILinkableStorageParent;
        private StorageSettings outputStoreSettings;
        private OutputSettings outputSettings;

        protected virtual IntVec3 WorkPosition => Position;

        protected CompPowerTrader powerComp;

        protected abstract bool IsAdvancedPort { get; }

        protected virtual bool ShowLimitGizmo => true;


        private string uniqueName;
        //IRenameable
        public string RenamableLabel
        {
            get => uniqueName ?? LabelCapNoCount;
            set => uniqueName = value;
        }
        //IRenameable
        public string BaseLabel => LabelCapNoCount;
        //IRenameable
        public string InspectLabel => LabelCap;
        /* TODO Check if we still need that
        public override string LabelNoCount => uniqueName ?? base.LabelNoCount;
        public override string LabelCap => uniqueName ?? base.LabelCap;
        */
        private static readonly Texture2D RenameTex = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

        private bool forbidOnPlacement;
        protected bool ForbidOnPlacement => forbidOnPlacement;


        public override Graphic Graphic => IOMode == StorageIOMode.Input ?
            base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<DefModExtension_StorageUnitIOPortColor>().inColor, Color.white) :
            base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<DefModExtension_StorageUnitIOPortColor>().outColor, Color.white);

        public virtual StorageIOMode IOMode
        {
            get => Mode;
            set
            {
                if (Mode == value) return;
                Mode = value;
                Notify_NeedRefresh();
            }
        }

        public ILinkableStorageParent BoundStorageUnit
        {
            get => boundStorageUnit;
            set
            {
                boundStorageUnit?.DeregisterPort(this);
                linkedStorageParentBuilding = (Building)value;
                value?.RegisterPort(this);
                Notify_NeedRefresh();
            }
        }

        protected OutputSettings OutputSettings => outputSettings ??= new OutputSettings("IOPort_Minimum_UseTooltip", "IOPort_Maximum_UseTooltip");
        
        public virtual bool ForbidPawnInput
        {
            get
            {
                if (IOMode != StorageIOMode.Output || !OutputSettings.useMax) return false;
                //Only get currentItem if needed
                var currentItem = WorkPosition.GetFirstItem(Map);
                if (currentItem != null)
                {
                    return OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit) <= 0;
                }
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Mode, "mode");
            Scribe_References.Look(ref linkedStorageParentBuilding, "boundStorageUnit");
            Scribe_Deep.Look(ref outputStoreSettings, "outputStoreSettings", this);
            Scribe_Deep.Look(ref outputSettings, "outputSettings", "IOPort_Minimum_UseTooltip", "IOPort_Maximum_UseTooltip");
            Scribe_Values.Look(ref uniqueName, "uniqueName");
            Scribe_Values.Look(ref forbidOnPlacement, "forbidOnPlacement");
        }
        public override string GetInspectString()
        {
            if (OutputSettings.useMin && OutputSettings.useMax) 
                return base.GetInspectString() + "\n" + "IOPort_Minimum".Translate(OutputSettings.min) + "\n" + "IOPort_Maximum".Translate(OutputSettings.max);
            if (OutputSettings.useMin && !OutputSettings.useMax) 
                return base.GetInspectString() + "\n" + "IOPort_Minimum".Translate(OutputSettings.min);
            if (!OutputSettings.useMin && OutputSettings.useMax) 
                return base.GetInspectString() + "\n" + "IOPort_Maximum".Translate(OutputSettings.max);
            return base.GetInspectString();
        }


        public override void PostMake()
        {
            base.PostMake();
            powerComp = GetComp<CompPowerTrader>();
            outputStoreSettings = new StorageSettings(this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();

            //Issues occurs if the boundStorageUnit spawns after this... Needs a check form the other way
            if (boundStorageUnit?.Map != map && (linkedStorageParentBuilding?.Spawned ?? false))
            {
                BoundStorageUnit = null;
            }

            def.building.groupingLabel = LabelCapNoCount;
        }

        protected override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == CompPowerTrader.PowerTurnedOnSignal)
            {
                Notify_NeedRefresh();
            }
        }

        public override void DeSpawn(DestroyMode destroyMode = DestroyMode.Vanish)
        {
            base.DeSpawn(destroyMode);
            boundStorageUnit?.DeregisterPort(this);
        }

        protected void Notify_NeedRefresh()
        {
            RefreshStoreSettings();
            switch (IOMode)
            {
                case StorageIOMode.Input:
                    RefreshInput();
                    break;
                case StorageIOMode.Output:
                    RefreshOutput();
                    break;
            }
        }

        public override void Notify_ReceivedThing(Thing newItem)
        {
            base.Notify_ReceivedThing(newItem);
            if (Mode == StorageIOMode.Input)
            {
                RefreshInput();
            }
        }

        public override void Notify_LostThing(Thing newItem)
        {
            base.Notify_LostThing(newItem);
            if (Mode == StorageIOMode.Output)
            {
                RefreshOutput();
            }
        }


        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (this.IsHashIntervalTick(10))
            {
                Notify_NeedRefresh();
            }
        }

        private void RefreshStoreSettings()
        {
            if (IOMode == StorageIOMode.Output)
            {
                settings = outputStoreSettings;
                if (boundStorageUnit != null && settings.Priority != boundStorageUnit.GetSettings.Priority)
                {
                    //the setter of settings.Priority is expensive
                    settings.Priority = boundStorageUnit.GetSettings.Priority;
                }
            }
            else if (boundStorageUnit != null)
            {
                settings = boundStorageUnit.GetSettings;
            }
            else
            {
                settings = new StorageSettings(this);
            }
        }

        protected virtual void RefreshInput()
        {
            if (!powerComp.PowerOn) return;
            var item = WorkPosition.GetFirstItem(Map);
            if (Mode == StorageIOMode.Input && item != null && (boundStorageUnit?.CanReceiveThing(item) ?? false))
            {
                boundStorageUnit.HandleNewItem(item);
            }
        }

        protected bool ItemsThatSatisfyMin(ref List<Thing> itemCandidates, Thing currentItem)
        {
            if (currentItem != null)
            {
                itemCandidates = itemCandidates.Where(currentItem.CanStackWith).ToList();
                var minRequired = OutputSettings.useMin ? outputSettings.min : 0;
                var count = currentItem.stackCount;
                var i = 0;
                while (i < itemCandidates.Count && count < minRequired)
                {
                    count += itemCandidates[i].stackCount;
                    i++;
                }
                return OutputSettings.SatisfiesMin(count);
            }
            //I wonder if GroupBy is benifficial or not
            return itemCandidates.GroupBy(t => t.def)
                .FirstOrDefault(g => OutputSettings.SatisfiesMin(g.Sum(t => t.stackCount)))?.Any() ?? false;
        }


        protected virtual void RefreshOutput() //
        {
            if (!powerComp.PowerOn) return;
            var currentItem = WorkPosition.GetFirstItem(Map);
            var storageSlotAvailable = currentItem == null || (settings.AllowedToAccept(currentItem) &&
                                                                OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit));
            if (boundStorageUnit is not { CanReceiveIO: true }) return;
            if (storageSlotAvailable)
            {
                var itemCandidates = new List<Thing>(from Thing t in boundStorageUnit.StoredItems where settings.AllowedToAccept(t) select t); // ToList very important - evaluates enumerable
                if (ItemsThatSatisfyMin(ref itemCandidates, currentItem))
                {
                    foreach (var item in itemCandidates)
                    {
                        if (currentItem != null)
                        {
                            if (currentItem.CanStackWith(item))
                            {
                                var count = Math.Min(item.stackCount, 
                                    OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit));
                                if (count > 0)
                                {
                                    var thingToRemove = item.SplitOff(count);
                                    if (item.stackCount <= 0) boundStorageUnit.HandleMoveItem(item);
                                    currentItem.TryAbsorbStack(thingToRemove, true);
                                }
                            }
                        }
                        else
                        {
                            var count = OutputSettings.CountNeededToReachMax(0, item.stackCount);
                            if (count > 0)
                            {
                                var thingToRemove = item.SplitOff(count);
                                if (item.stackCount <= 0) boundStorageUnit.HandleMoveItem(item);
                                currentItem = GenSpawn.Spawn(thingToRemove, WorkPosition, Map);
                            }
                        }
                        if (currentItem != null && !OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit))
                        {
                            break;
                        }
                    }
                }
            }
            //Transfer an item back if it is either too few or disallowed
            if (currentItem != null && (!settings.AllowedToAccept(currentItem) 
                                        || !OutputSettings.SatisfiesMin(currentItem.stackCount)) 
                                    && boundStorageUnit.GetSettings.AllowedToAccept(currentItem))
            {
                currentItem.SetForbidden(false, false);
                boundStorageUnit.HandleNewItem(currentItem);
            }
            //Transfer the difference back if it is too much
            if (currentItem != null && (!OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit) && boundStorageUnit.GetSettings.AllowedToAccept(currentItem)))
            {
                var splitCount = -OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit);
                if (splitCount > 0)
                {
                    var returnThing = currentItem.SplitOff(splitCount);
                    returnThing.SetForbidden(false, false);
                    boundStorageUnit.HandleNewItem(returnThing);
                }
            }

            currentItem?.SetForbidden(ForbidOnPlacement, false);
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos()) yield return g;
            yield return new Command_Action()
            {
                defaultLabel = "PRFBoundStorageBuilding".Translate() + ": " + (((IRenameable)boundStorageUnit)?.RenamableLabel ?? "NoneBrackets".Translate()),
                action = () =>
                {
                    //ILinkableStorageParent
                    var mylist = Map.listerBuildings.allBuildingsColonist.Where(b => (b as ILinkableStorageParent) != null && (b as ILinkableStorageParent).CanUseIOPort).ToList();
                    if (IsAdvancedPort) mylist.RemoveAll(b => !(b as ILinkableStorageParent).AdvancedIOAllowed);
                    var list = new List<FloatMenuOption>(
                        mylist.Select(b => new FloatMenuOption(((IRenameable)b).RenamableLabel, () => SelectedPorts().ToList().ForEach(p => p.BoundStorageUnit = (b as ILinkableStorageParent))))
                    );
                    if (list.Count == 0)
                    {
                        list.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                },
                icon = CargoPlatformTex
            };
            yield return new Command_Action
            {
                icon = RenameTex,
                action = () => Find.WindowStack.Add(new Dialog_RenameStorageUnitIOBase(this)),
                hotKey = KeyBindingDefOf.Misc1,
                defaultLabel = "PRFRenameMassStorageUnitLabel".Translate(),
                defaultDesc = "PRFRenameMassStorageUnitDesc".Translate()
            };
            if (IOMode == StorageIOMode.Output && ShowLimitGizmo)
            {
                yield return new Command_Action()
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel"),
                    defaultLabel = "PRFIOOutputSettings".Translate(),
                    action = () => Find.WindowStack.Add(new Dialog_OutputMinMax(OutputSettings, () => SelectedPorts().Where(p => p.IOMode == StorageIOMode.Output).ToList().ForEach(p => OutputSettings.Copy(p.OutputSettings))))
                };
            }
            if (Mode == StorageIOMode.Output)
            {
                yield return new Command_Toggle()
                {
                    isActive = () => forbidOnPlacement,
                    toggleAction = () => forbidOnPlacement = !forbidOnPlacement,
                    defaultLabel = "PRF_Toggle_ForbidOnPlacement".Translate(),
                    defaultDesc = "PRF_Toggle_ForbidOnPlacementDesc".Translate(),
                    icon = forbidOnPlacement ? RS.ForbidOn : RS.ForbidOff

                };
            }



        }

        private IEnumerable<Building_StorageUnitIOBase> SelectedPorts()
        {
            var l = Find.Selector.SelectedObjects.OfType<Building_StorageUnitIOBase>().ToList();
            if (!l.Contains(this))
            {
                l.Add(this);
            }
            return l;
        }

        public virtual bool OutputItem(Thing thing)
        {
            if (boundStorageUnit?.CanReceiveIO ?? false)
            {
                return GenPlace.TryPlaceThing(thing.SplitOff(thing.stackCount), WorkPosition, Map, ThingPlaceMode.Near,
                    null, pos =>
                    {
                        
                        if (settings.AllowedToAccept(thing) && OutputSettings.SatisfiesMin(thing.stackCount) && pos == WorkPosition)
                        {
                            return true;
                        }

                        foreach (var t in Map.thingGrid.ThingsListAt(pos))
                        {
                            if (t is Building_StorageUnitIOPort) return false;
                        }

                        return true;
                    });
            }

            return false;
        }
    }




    [StaticConstructorOnStartup]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Building_StorageUnitIOPort : Building_StorageUnitIOBase
    {

        public override Graphic Graphic => IOMode == StorageIOMode.Input ?
            base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<DefModExtension_StorageUnitIOPortColor>().inColor, Color.white) :
            base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<DefModExtension_StorageUnitIOPortColor>().outColor, Color.white);

        public override StorageIOMode IOMode
        {
            get => Mode;
            set
            {
                if (Mode == value) return;
                Mode = value;
                Notify_NeedRefresh();
            }
        }

        protected override bool IsAdvancedPort => false;

        protected override void RefreshInput()
        {
            if (!powerComp.PowerOn) return;
            var item = Position.GetFirstItem(Map);
            if (Mode == StorageIOMode.Input && item != null && (boundStorageUnit?.CanReceiveThing(item) ?? false))
            {
                boundStorageUnit.HandleNewItem(item);
            }
        }

        /// <summary>
        /// Modified version of Verse.Thing.TryAbsorbStack (based on 1.3.7964.22648)
        /// Might Cause unexpected things as 
        /// DS Has a patch for Thing.TryAbsorbStack
        /// Thing.SplitOff has a CommonSense Transpiler
        /// </summary>
        /// <param name="baseThing"></param>
        /// <param name="toBeAbsorbed"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static bool AbsorbAmount(ref Thing baseThing, ref Thing toBeAbsorbed, int count)
        {

            if (!baseThing.CanStackWith(toBeAbsorbed))
            {
                return false;
            }
            int num = count;


            if (baseThing.def.useHitPoints)
            {
                baseThing.HitPoints = Mathf.CeilToInt((baseThing.HitPoints * baseThing.stackCount + toBeAbsorbed.HitPoints * num) / (float)(baseThing.stackCount + num));
            }


            baseThing.stackCount += num;
            toBeAbsorbed.stackCount -= num;
            if (baseThing.Map != null)
            {
                baseThing.DirtyMapMesh(baseThing.Map);
            }
            StealAIDebugDrawer.Notify_ThingChanged(baseThing);
            if (baseThing.Spawned)
            {
                baseThing.Map.listerMergeables.Notify_ThingStackChanged(baseThing);
            }
            if (toBeAbsorbed.stackCount <= 0)
            {
                toBeAbsorbed.Destroy();
                return true;
            }
            return false;


        }

        protected override void RefreshOutput()
        {
            if (!powerComp.PowerOn) return;
            var currentItem = Position.GetFirstItem(Map);
            var storageSlotAvailable = currentItem == null || (settings.AllowedToAccept(currentItem) &&
                                                               OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit));
            if (boundStorageUnit is not { CanReceiveIO: true }) return;
            if (storageSlotAvailable)
            {
                var itemCandidates = new List<Thing>(from Thing t in boundStorageUnit.StoredItems where settings.AllowedToAccept(t) select t); // ToList very important - evaluates enumerable
                //ItemsThatSatisfyMin somtimes spikes to 0.1 but it is mostly an none issue
                if (ItemsThatSatisfyMin(ref itemCandidates, currentItem))
                {
                    foreach (var item in itemCandidates)
                    {
                        if (currentItem != null)
                        {
                            if (currentItem.CanStackWith(item))
                            {
                                var count = Math.Min(item.stackCount, OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit));
                                if (count > 0)
                                {
                                    var Mything = item;
                                    //Merge Stacks - Gab count required to fulfill settings and merge them to the stuff on the IO Port
                                    //For SplitOff "MakeThing" is expensive
                                    //For TryAbsorbStack "Destroy" is expensive
                                    AbsorbAmount(ref currentItem, ref Mything, count);
                                    if (Mything.stackCount <= 0) boundStorageUnit.HandleMoveItem(Mything);
                                }
                            }
                        }
                        else
                        {
                            var count = OutputSettings.CountNeededToReachMax(0, item.stackCount);
                            if (count > 0)
                            {
                                //Nothing on the IO Port - grab thing from storage and place it on the port
                                //For SplitOff "MakeThing" is expensive
                                var thingToRemove = item.SplitOff(count);
                                if (item.stackCount <= 0 || thingToRemove == item) boundStorageUnit.HandleMoveItem(item);
                                currentItem = GenSpawn.Spawn(thingToRemove, Position, Map);
                            }
                        }
                        if (currentItem != null && !OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit))
                        {
                            break;
                        }
                    }
                }
            }
            //Transfre a item back if it is either too few or disallowed
            if (currentItem != null && (!settings.AllowedToAccept(currentItem) || !OutputSettings.SatisfiesMin(currentItem.stackCount)) && boundStorageUnit.GetSettings.AllowedToAccept(currentItem))
            {
                currentItem.SetForbidden(false, false);
                boundStorageUnit.HandleNewItem(currentItem);
            }
            //Transfer the diffrence back if it is too much
            if (currentItem != null && (!OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit) && boundStorageUnit.GetSettings.AllowedToAccept(currentItem)))
            {
                int splitCount = -OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit);
                if (splitCount > 0)
                {
                    Thing returnThing = currentItem.SplitOff(splitCount);
                    returnThing.SetForbidden(false, false);
                    boundStorageUnit.HandleNewItem(returnThing);
                }
            }
            if (currentItem != null)
            {
                currentItem.SetForbidden(ForbidOnPlacement, false);
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos()) yield return g;
            yield return new Command_Action()
            {
                defaultLabel = "PRFIOMode".Translate() + ": " + (IOMode == StorageIOMode.Input ? "PRFIOInput".Translate() : "PRFIOOutput".Translate()),
                action = () =>
                {
                    Find.WindowStack.Add(new FloatMenu([
                        new FloatMenuOption("PRFIOInput".Translate(),
                            () => SelectedPorts().ToList().ForEach(p => p.IOMode = StorageIOMode.Input)),
                        new FloatMenuOption("PRFIOOutput".Translate(),
                            () => SelectedPorts().ToList().ForEach(p => p.IOMode = StorageIOMode.Output))
                    ]));
                },
                icon = IOModeTex
            };
        }

        private IEnumerable<Building_StorageUnitIOPort> SelectedPorts()
        {
            var l = Find.Selector.SelectedObjects.OfType<Building_StorageUnitIOPort>().ToList();
            if (!l.Contains(this))
            {
                l.Add(this);
            }
            return l;
        }

        public override bool OutputItem(Thing thing)
        {
            if (!(boundStorageUnit?.CanReceiveIO ?? false)) return false;
            
            return GenPlace.TryPlaceThing(thing.SplitOff(thing.stackCount), Position, Map, ThingPlaceMode.Near,
                null, pos =>
                {
                    if (settings.AllowedToAccept(thing) && OutputSettings.SatisfiesMin(thing.stackCount))
                        if (pos == Position)
                            return true;
                    foreach (var t in Map.thingGrid.ThingsListAt(pos))
                    {
                        if (t is Building_StorageUnitIOPort) return false;
                    }

                    return true;
                });

        }
    }

    public class DefModExtension_StorageUnitIOPortColor : DefModExtension
    {
        public Color inColor;
        public Color outColor;
    }
}
