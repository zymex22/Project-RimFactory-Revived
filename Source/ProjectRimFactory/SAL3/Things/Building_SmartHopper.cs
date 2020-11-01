using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using ProjectRimFactory.SAL3.UI;
using ProjectRimFactory.SAL3.Tools;
using ProjectRimFactory.Storage;
using ProjectRimFactory.Storage.UI;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.SAL3.Things
{
    public class Building_SmartHopper : Building, IStoreSettingsParent, IPowerSupplyMachineHolder
    {
        private OutputSettings outputSettings;

        public IEnumerable<IntVec3> cachedDetectorCells;

        protected virtual bool ShouldRespectStackLimit => true;

        public StorageSettings settings;

        public Thing StoredThing => Position.GetFirstItem(Map);

        protected bool PickupFromGround => (this.def.GetModExtension<ModExtension_Settings>()?.GetByName<bool>("pickupFromGround") ?? false) && pickupFromGround;

        private bool pickupFromGround;

        public IPowerSupplyMachine RangePowerSupplyMachine => this.GetComp<CompPowerWorkSetting>();

        private IEnumerable<IntVec3> CellsToTarget => (this.GetComp<CompPowerWorkSetting>()?.GetRangeCells() ?? GenRadial.RadialCellsAround(Position, this.def.specialDisplayRadius, false)).Where(c => c.GetFirst<Building_SmartHopper>(this.Map) == null);

        public IEnumerable<IntVec3> CellsToSelect
        {
            get
            {
                if (Find.TickManager.TicksGame % 50 != 0 && cachedDetectorCells != null)
                {
                    return cachedDetectorCells;
                }

                var resultCache = from IntVec3 c
                                  in this.CellsToTarget
                                  where this.PickupFromGround || c.HasSlotGroupParent(Map)
                                  select c;
                cachedDetectorCells = resultCache;
                return resultCache;
            }
        }

        public IEnumerable<Thing> ThingsToSelect
        {
            get
            {
                foreach (var c in CellsToSelect)
                {
                    foreach (Thing t in GatherThingsUtility.AllThingsInCellForUse(c, Map)) {
                        yield return t;
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
            set
            {
                outputSettings = value;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (settings == null)
            {
                settings = new StorageSettings();
                settings.CopyFrom(GetParentStoreSettings());
            }
            if (!respawningAfterLoad)
            {
                this.pickupFromGround = this.def.GetModExtension<ModExtension_Settings>()?.GetByName<bool>("pickupFromGround") ?? false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref outputSettings, "outputSettings", "SmartHopper_Minimum_UseTooltip", "SmartHopper_Maximum_UseTooltip");
            Scribe_Deep.Look(ref settings, "settings", this);
            Scribe_Values.Look(ref this.pickupFromGround, "pickupFromGround");
        }

        public override string GetInspectString()
        {
            if (OutputSettings.useMin && OutputSettings.useMax) return base.GetInspectString() + "\n" + "SmartHopper_Minimum".Translate(OutputSettings.min) + "\n" + "SmartHopper_Maximum".Translate(OutputSettings.max);
            else if (OutputSettings.useMin && !OutputSettings.useMax) return base.GetInspectString() + "\n" + "SmartHopper_Minimum".Translate(OutputSettings.min);
            else if (!OutputSettings.useMin && OutputSettings.useMax) return base.GetInspectString() + "\n" + "SmartHopper_Maximum".Translate(OutputSettings.max);
            else return base.GetInspectString();
        }

        public override void Tick()
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
                    if (OutputSettings.useMax) num = Mathf.Min(element.stackCount, Mathf.Min((StoredThing.def.stackLimit - StoredThing.stackCount),(OutputSettings.max - StoredThing.stackCount)));
                    
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
                else if (num > 0)
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
            if (!this.PickupFromGround)
                GenDraw.DrawFieldEdges(CellsToSelect.ToList(), Color.green);
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
            if (this.def.GetModExtension<ModExtension_Settings>()?.GetByName<bool>("pickupFromGround") ?? false)
            {
                yield return new Command_Toggle
                {
                    icon = ContentFinder<Texture2D>.Get("UI/PickupFromGround"),
                    defaultLabel = "SmartHopper_PickupFromGround".Translate(),
                    toggleAction = () => this.pickupFromGround = !this.pickupFromGround,
                    isActive = () => this.pickupFromGround
                };
            }
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
    }
}
