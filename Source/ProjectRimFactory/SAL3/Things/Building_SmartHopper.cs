using ProjectRimFactory.AutoMachineTool;
using ProjectRimFactory.Common;
using ProjectRimFactory.Storage;
using ProjectRimFactory.Storage.UI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{
    // ReSharper disable once UnusedType.Global
    public class Building_SmartHopper : Building, IStoreSettingsParent, IPowerSupplyMachineHolder, IPickupSettings
    {
        private OutputSettings outputSettings;

        private readonly List<IntVec3> cachedDetectorCells = [];
        private bool cachedDetectorCellsDirty = true;

        private StorageSettings settings;

        private Thing StoredThing => Position.GetFirstItem(Map);

        private CompPowerWorkSetting compPowerWorkSetting;

        public IPowerSupplyMachine RangePowerSupplyMachine => compPowerWorkSetting ?? GetComp<CompPowerWorkSetting>();

        private IEnumerable<IntVec3> CellsToTarget
        {
            get
            {
                var cells = compPowerWorkSetting?.GetRangeCells() ?? GenRadial.RadialCellsAround(Position, def.specialDisplayRadius, false);
                return cells.Where(c => c.GetFirst<Building_SmartHopper>(Map) == null); //Exclude other Smart Hoppers
            }
        }


        private bool cellIsZone_Stockpile(IntVec3 cell)
        {
            return cell.GetZone(Map) is Zone_Stockpile;
        }
        private bool cellIsBuilding_Storage(IntVec3 cell)
        {
            return cell.GetThingList(Map).FirstOrDefault(t => t is ISlotGroupParent) is Building_Storage;
        }
        private bool cellIsBuilding_BeltConveyor(IntVec3 cell)
        {
            return cell.GetThingList(Map).Any(t => t is Building_BeltConveyor);
        }

        private List<IntVec3> CellsToSelect
        {
            get
            {
                if (Find.TickManager.TicksGame % 50 != 0 && !cachedDetectorCellsDirty)
                {
                    return cachedDetectorCells;
                }

                cachedDetectorCellsDirty = false;
                cachedDetectorCells.Clear();
                foreach (var cell in CellsToTarget)
                {
                    if (!cell.InBounds(Map)) continue;
                    if  ((allowStockpilePickup && cellIsZone_Stockpile(cell)) || 
                        (allowStoragePickup && cellIsBuilding_Storage(cell)) || 
                        (allowBeltPickup && cellIsBuilding_BeltConveyor(cell)) ||
                        (allowGroundPickup && !cellIsZone_Stockpile(cell) && !cellIsBuilding_Storage(cell) && !cellIsBuilding_BeltConveyor(cell))
                        )
                    {
                        cachedDetectorCells.Add(cell);
                    }
                }

                return cachedDetectorCells;
            }
        }

        private IEnumerable<Thing> ThingsToSelect
        {
            get
            {
                foreach (var cell in CellsToSelect)
                {
                    foreach (var t in cell.AllThingsInCellForUse(Map,AllowStockpilePickup))
                    {
                        if (allowForbiddenPickup || !t.IsForbidden(Faction.OfPlayer))
                        {
                            yield return t;
                        }
                    }
                }
            }
        }

        public bool StorageTabVisible => true;

        private OutputSettings OutputSettings
        {
            get
            {
                return outputSettings ??= new OutputSettings("SmartHopper_Minimum_UseTooltip", "SmartHopper_Maximum_UseTooltip");
            }
        }
        
        private bool allowGroundPickup;
        private bool allowStockpilePickup;
        private bool allowStoragePickup;
        private bool allowForbiddenPickup;
        private bool allowBeltPickup;

        public bool AllowGroundPickup
        {
            get => allowGroundPickup; 
            
            set
            {
                if (value != allowGroundPickup) cachedDetectorCellsDirty = true;
                allowGroundPickup = value;
            }
        }
        public bool AllowStockpilePickup
        {
            get => allowStockpilePickup; 
            set
            {
                if (value != allowStockpilePickup) cachedDetectorCellsDirty = true;
                allowStockpilePickup = value;
            }
        }
        public bool AllowStoragePickup
        {
            get => allowStoragePickup; 
            set
            {
                if (value != allowStoragePickup) cachedDetectorCellsDirty = true;
                allowStoragePickup = value;
            }
        }
        public bool AllowForbiddenPickup { get => allowForbiddenPickup; set => allowForbiddenPickup = value; }
        public bool AllowBeltPickup
        {
            get => allowBeltPickup; 
            set
            {
                if (value != allowBeltPickup) cachedDetectorCellsDirty = true;
                allowBeltPickup = value;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPowerWorkSetting = GetComp<CompPowerWorkSetting>();
            if (settings == null)
            {
                settings = new StorageSettings();
                settings.CopyFrom(GetParentStoreSettings());
            }
            if (!respawningAfterLoad)
            {
                allowGroundPickup = true;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref outputSettings, "outputSettings", "SmartHopper_Minimum_UseTooltip", "SmartHopper_Maximum_UseTooltip");
            Scribe_Deep.Look(ref settings, "settings", this);
            Scribe_Values.Look(ref allowGroundPickup, "allowGroundPickup");
            Scribe_Values.Look(ref allowStockpilePickup, "allowStockpilePickup");
            Scribe_Values.Look(ref allowStoragePickup, "allowStoragePickup");
            Scribe_Values.Look(ref allowForbiddenPickup, "allowForbiddenPickup");
            Scribe_Values.Look(ref allowBeltPickup, "allowBeltPickup");
        }

        public override string GetInspectString()
        {
            if (OutputSettings.useMin && OutputSettings.useMax)
                return base.GetInspectString() + "\n" + "SmartHopper_Minimum".Translate(OutputSettings.min) + "\n" + "SmartHopper_Maximum".Translate(OutputSettings.max);
            if (OutputSettings.useMin && !OutputSettings.useMax)
                return base.GetInspectString() + "\n" + "SmartHopper_Minimum".Translate(OutputSettings.min);
            if (!OutputSettings.useMin && OutputSettings.useMax)
                return base.GetInspectString() + "\n" + "SmartHopper_Maximum".Translate(OutputSettings.max);
            
            return base.GetInspectString();
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (Find.TickManager.TicksGame % 35 != 0 || !GetComp<CompPowerTrader>().PowerOn) return;
            foreach (var element in ThingsToSelect)
            {
                var withinLimits = true;
                if (OutputSettings.useMin) withinLimits = (element.stackCount >= OutputSettings.min);

                if (element.def.category == ThingCategory.Item && settings.AllowedToAccept(element) && withinLimits)
                {
                    TryStoreThing(element);
                    break;
                }
            }

            if (StoredThing == null) return;
            if (settings.AllowedToAccept(StoredThing))
            {
                var forbidItem = true;

                if (OutputSettings.useMin || OutputSettings.useMax)
                {
                    if (OutputSettings.useMin && StoredThing.stackCount < OutputSettings.min)
                        forbidItem = false;
                    else if (OutputSettings.useMax && StoredThing.stackCount > OutputSettings.max)
                        forbidItem = false;
                }
                if (forbidItem)
                {
                    StoredThing.SetForbidden(true, false);
                    return;
                }
            }
            StoredThing.SetForbidden(false, false);
        }

        protected virtual void TryStoreThing(Thing element)
        {
            if (StoredThing != null)
            {
                if (!StoredThing.CanStackWith(element)) return;
                var num = Mathf.Min(element.stackCount, (StoredThing.def.stackLimit - StoredThing.stackCount));
                if (OutputSettings.useMax) num = Mathf.Min(element.stackCount, Mathf.Min((StoredThing.def.stackLimit - StoredThing.stackCount), (OutputSettings.max - StoredThing.stackCount)));

                if (num <= 0) return;
                var t = element.SplitOff(num);
                StoredThing.TryAbsorbStack(t, true);
            }
            else
            {
                var num = element.stackCount;

                if (OutputSettings.useMax) num = Mathf.Min(element.stackCount, OutputSettings.max);
                if (num <= 0) return;
                // if this is the entire stack, we just get the stack. Important for belts to do it this way:
                var t = element.SplitOff(num);
                GenPlace.TryPlaceThing(t, Position, Map, ThingPlaceMode.Direct);
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(CellsToSelect, Color.green);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;
            foreach (Gizmo g2 in StorageSettingsClipboard.CopyPasteGizmosFor(settings))
                yield return g2;
            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel"),
                defaultLabel = "SmartHopper_SetTargetAmount".Translate(),
                action = () => Find.WindowStack.Add(new Dialog_OutputMinMax(OutputSettings)),
            };
        }

        public StorageSettings GetStoreSettings()
        {
            if (settings != null) return settings;
            settings = new StorageSettings();
            settings.CopyFrom(GetParentStoreSettings());
            return settings;
        }

        public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;

        public void Notify_SettingsChanged()
        {
            // Might allow us to cache StorageSettings
            // unsure about the potential gains / current load
        }
    }
}
