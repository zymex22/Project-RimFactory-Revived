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
    public class Building_SmartHopper : Building, IStoreSettingsParent, IPowerSupplyMachineHolder, IPickupSettings
    {
        private OutputSettings outputSettings;

        public List<IntVec3> cachedDetectorCells = new List<IntVec3>();
        private bool cachedDetectorCellsDirty = true;

        protected virtual bool ShouldRespectStackLimit => true;

        public StorageSettings settings;

        public Thing StoredThing => Position.GetFirstItem(Map);

        private CompPowerWorkSetting compPowerWorkSetting;

        public IPowerSupplyMachine RangePowerSupplyMachine => compPowerWorkSetting ?? this.GetComp<CompPowerWorkSetting>();

        private IEnumerable<IntVec3> CellsToTarget
        {
            get
            {
                IEnumerable<IntVec3> cells = compPowerWorkSetting?.GetRangeCells() ?? GenRadial.RadialCellsAround(Position, this.def.specialDisplayRadius, false);
                return cells.Where(c => c.GetFirst<Building_SmartHopper>(this.Map) == null); //Exclude other Smart Hoppers
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

        public List<IntVec3> CellsToSelect
        {
            get
            {
                if (Find.TickManager.TicksGame % 50 != 0 && !cachedDetectorCellsDirty)
                {
                    return cachedDetectorCells;
                }

                cachedDetectorCellsDirty = false;
                cachedDetectorCells.Clear();
                foreach (IntVec3 c in CellsToTarget)
                {
                    if (!c.InBounds(this.Map)) continue;
                    if  ((allowStockpilePickup && cellIsZone_Stockpile(c)) || 
                        (allowStoragePickup && cellIsBuilding_Storage(c)) || 
                        (allowBeltPickup && cellIsBuilding_BeltConveyor(c)) ||
                        (this.allowGroundPickup && !cellIsZone_Stockpile(c) && !cellIsBuilding_Storage(c) && !cellIsBuilding_BeltConveyor(c))
                        )
                    {
                        cachedDetectorCells.Add(c);
                    }
                }

                return cachedDetectorCells;
            }
        }

        public IEnumerable<Thing> ThingsToSelect
        {
            get
            {
                foreach (var c in CellsToSelect)
                {
                    foreach (Thing t in GatherThingsUtility.AllThingsInCellForUse(c, Map,AllowStockpilePickup))
                    {
                        var SlotGroupParrent = t.GetSlotGroup()?.parent;
                        if (allowForbiddenPickup || !t.IsForbidden(Faction.OfPlayer))
                        {
                            yield return t;
                        }
                    }
                }
            }
        }

        public bool StorageTabVisible => true;

        public OutputSettings OutputSettings
        {
            get
            {
                if (outputSettings == null)
                {
                    outputSettings = new OutputSettings("SmartHopper_Minimum_UseTooltip", "SmartHopper_Maximum_UseTooltip");
                }
                return outputSettings;
            }
            set => outputSettings = value;
        }



        private bool allowGroundPickup = false;
        private bool allowStockpilePickup = false;
        private bool allowStoragePickup = false;
        private bool allowForbiddenPickup = false;
        private bool allowBeltPickup = false;

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
            compPowerWorkSetting = this.GetComp<CompPowerWorkSetting>();
            if (settings == null)
            {
                settings = new StorageSettings();
                settings.CopyFrom(GetParentStoreSettings());
            }
            if (!respawningAfterLoad)
            {
                this.allowGroundPickup = true;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref outputSettings, "outputSettings", "SmartHopper_Minimum_UseTooltip", "SmartHopper_Maximum_UseTooltip");
            Scribe_Deep.Look(ref settings, "settings", this);
            Scribe_Values.Look(ref this.allowGroundPickup, "allowGroundPickup", false);
            Scribe_Values.Look(ref this.allowStockpilePickup, "allowStockpilePickup", false);
            Scribe_Values.Look(ref this.allowStoragePickup, "allowStoragePickup", false);
            Scribe_Values.Look(ref this.allowForbiddenPickup, "allowForbiddenPickup", false);
            Scribe_Values.Look(ref this.allowBeltPickup, "allowBeltPickup", false);
        }

        public override string GetInspectString()
        {
            if (OutputSettings.useMin && OutputSettings.useMax) return base.GetInspectString() + "\n" + "SmartHopper_Minimum".Translate(OutputSettings.min) + "\n" + "SmartHopper_Maximum".Translate(OutputSettings.max);
            else if (OutputSettings.useMin && !OutputSettings.useMax) return base.GetInspectString() + "\n" + "SmartHopper_Minimum".Translate(OutputSettings.min);
            else if (!OutputSettings.useMin && OutputSettings.useMax) return base.GetInspectString() + "\n" + "SmartHopper_Maximum".Translate(OutputSettings.max);
            else return base.GetInspectString();
        }

        protected override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 35 == 0 && GetComp<CompPowerTrader>().PowerOn)
            {
                foreach (var element in ThingsToSelect)
                {
                    bool withinLimits = true;
                    if (OutputSettings.useMin) withinLimits = (element.stackCount >= OutputSettings.min);

                    if (element.def.category == ThingCategory.Item && settings.AllowedToAccept(element) && withinLimits)
                    {
                        TryStoreThing(element);
                        break;
                    }
                }
                if (StoredThing != null)
                {
                    if (settings.AllowedToAccept(StoredThing))
                    {
                        bool forbidItem = true;

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
            }
        }

        public virtual void TryStoreThing(Thing element)
        {
            if (StoredThing != null)
            {
                if (StoredThing.CanStackWith(element))
                {
                    var num = Mathf.Min(element.stackCount, (StoredThing.def.stackLimit - StoredThing.stackCount));
                    if (OutputSettings.useMax) num = Mathf.Min(element.stackCount, Mathf.Min((StoredThing.def.stackLimit - StoredThing.stackCount), (OutputSettings.max - StoredThing.stackCount)));

                    if (num > 0)
                    {
                        var t = element.SplitOff(num);
                        StoredThing.TryAbsorbStack(t, true);
                    }
                }
            }
            else
            {
                var num = element.stackCount;

                if (OutputSettings.useMax) num = Mathf.Min(element.stackCount, OutputSettings.max);
                if (num > 0)
                {
                    // if this is the entire stack, we just get the stack. Important for belts to do it this way:
                    var t = element.SplitOff(num);
                    GenPlace.TryPlaceThing(t, Position, Map, ThingPlaceMode.Direct);
                }
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
            if (settings == null)
            {
                settings = new StorageSettings();
                settings.CopyFrom(GetParentStoreSettings());
            }
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
