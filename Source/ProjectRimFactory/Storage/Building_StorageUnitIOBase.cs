using System;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Common.HarmonyPatches;
using ProjectRimFactory.Storage.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage;

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

    protected CompPowerTrader PowerTrader;

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
            if (IOMode != StorageIOMode.Output || !OutputSettings.UseMax) return false;
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
        if (OutputSettings.UseMin && OutputSettings.UseMax) 
            return base.GetInspectString() + "\n" + "IOPort_Minimum".Translate(OutputSettings.Min) + "\n" + "IOPort_Maximum".Translate(OutputSettings.Max);
        if (OutputSettings.UseMin && !OutputSettings.UseMax) 
            return base.GetInspectString() + "\n" + "IOPort_Minimum".Translate(OutputSettings.Min);
        if (!OutputSettings.UseMin && OutputSettings.UseMax) 
            return base.GetInspectString() + "\n" + "IOPort_Maximum".Translate(OutputSettings.Max);
        return base.GetInspectString();
    }


    public override void PostMake()
    {
        base.PostMake();
        PowerTrader = GetComp<CompPowerTrader>();
        outputStoreSettings = new StorageSettings(this);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        PowerTrader = GetComp<CompPowerTrader>();

        //Issues occurs if the boundStorageUnit spawns after this... Needs a check form the other way
        if (boundStorageUnit?.Map != map && (linkedStorageParentBuilding?.Spawned ?? false))
        {
            BoundStorageUnit = null;
        }
        
        // Unlink on destroyed linkedStorageParentBuilding when placing the Gravship
        if ((linkedStorageParentBuilding?.Destroyed ?? false) && GravshipPlacementUtility.placingGravship)
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
        if (!PowerTrader.PowerOn) return;
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
            var minRequired = OutputSettings.UseMin ? outputSettings.Min : 0;
            var count = currentItem.stackCount;
            var i = 0;
            while (i < itemCandidates.Count && count < minRequired)
            {
                count += itemCandidates[i].stackCount;
                i++;
            }
            return OutputSettings.SatisfiesMin(count);
        }
        //I wonder if GroupBy is beneficial or not
        return itemCandidates.GroupBy(t => t.def)
            .FirstOrDefault(g => OutputSettings.SatisfiesMin(g.Sum(t => t.stackCount)))?.Any() ?? false;
    }


    protected virtual void RefreshOutput() //
    {
        if (!PowerTrader.PowerOn) return;
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
                var storageBuildings = Map.listerBuildings.allBuildingsColonist
                    .Where(b => b is ILinkableStorageParent { CanUseIOPort: true }).ToList();
                if (IsAdvancedPort) storageBuildings.RemoveAll(b => !(b as ILinkableStorageParent)!.AdvancedIOAllowed);
                var list = new List<FloatMenuOption>(
                    storageBuildings.Select(b => new FloatMenuOption(((IRenameable)b).RenamableLabel, () => SelectedPorts().ToList().ForEach(p => p.BoundStorageUnit = (b as ILinkableStorageParent))))
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