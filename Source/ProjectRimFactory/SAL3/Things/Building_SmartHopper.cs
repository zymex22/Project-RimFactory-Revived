using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3.Tools;
using ProjectRimFactory.Storage;
using ProjectRimFactory.Storage.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{
    public class Building_SmartHopper : Building, IStoreSettingsParent, IPowerSupplyMachineHolder
    {
        public IEnumerable<IntVec3> cachedDetectorCells;
        private OutputSettings outputSettings;

        private bool pickupFromGround;

        public StorageSettings settings;

        protected virtual bool ShouldRespectStackLimit => true;

        public Thing StoredThing => Position.GetFirstItem(Map);

        protected bool PickupFromGround =>
            (def.GetModExtension<ModExtension_Settings>()?.GetByName<bool>("pickupFromGround") ?? false) &&
            pickupFromGround;

        private IEnumerable<IntVec3> CellsToTarget =>
            (GetComp<CompPowerWorkSetting>()?.GetRangeCells() ??
             GenRadial.RadialCellsAround(Position, def.specialDisplayRadius, false))
            .Where(c => c.GetFirst<Building_SmartHopper>(Map) == null);

        public IEnumerable<IntVec3> CellsToSelect
        {
            get
            {
                if (Find.TickManager.TicksGame % 50 != 0 && cachedDetectorCells != null) return cachedDetectorCells;

                var resultCache = from IntVec3 c
                        in CellsToTarget
                    where PickupFromGround || c.HasSlotGroupParent(Map)
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
                foreach (var t in c.AllThingsInCellForUse(Map))
                    yield return t;
            }
        }

        public OutputSettings OutputSettings
        {
            get
            {
                if (outputSettings == null)
                    outputSettings = new OutputSettings("SmartHopper_Minimum_UseTooltip",
                        "SmartHopper_Maximum_UseTooltip");
                return outputSettings;
            }
            set => outputSettings = value;
        }

        public IPowerSupplyMachine RangePowerSupplyMachine => GetComp<CompPowerWorkSetting>();

        public bool StorageTabVisible => true;

        public StorageSettings GetStoreSettings()
        {
            if (settings == null)
            {
                settings = new StorageSettings();
                settings.CopyFrom(GetParentStoreSettings());
            }

            return settings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return def.building.fixedStorageSettings;
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
                pickupFromGround = def.GetModExtension<ModExtension_Settings>()?.GetByName<bool>("pickupFromGround") ??
                                   false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref outputSettings, "outputSettings", "SmartHopper_Minimum_UseTooltip",
                "SmartHopper_Maximum_UseTooltip");
            Scribe_Deep.Look(ref settings, "settings", this);
            Scribe_Values.Look(ref pickupFromGround, "pickupFromGround");
        }

        public override string GetInspectString()
        {
            if (OutputSettings.useMin && OutputSettings.useMax)
                return base.GetInspectString() + "\n" + "SmartHopper_Minimum".Translate(OutputSettings.min) + "\n" +
                       "SmartHopper_Maximum".Translate(OutputSettings.max);
            if (OutputSettings.useMin && !OutputSettings.useMax)
                return base.GetInspectString() + "\n" + "SmartHopper_Minimum".Translate(OutputSettings.min);
            if (!OutputSettings.useMin && OutputSettings.useMax)
                return base.GetInspectString() + "\n" + "SmartHopper_Maximum".Translate(OutputSettings.max);
            return base.GetInspectString();
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 35 == 0 && GetComp<CompPowerTrader>().PowerOn)
            {
                foreach (var element in ThingsToSelect)
                {
                    var withinLimits = true;
                    if (OutputSettings.useMin) withinLimits = element.stackCount >= OutputSettings.min;

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
            }
        }

        public virtual void TryStoreThing(Thing element)
        {
            if (StoredThing != null)
            {
                if (StoredThing.CanStackWith(element))
                {
                    var num = Mathf.Min(element.stackCount, StoredThing.def.stackLimit - StoredThing.stackCount);
                    if (OutputSettings.useMax)
                        num = Mathf.Min(element.stackCount,
                            Mathf.Min(StoredThing.def.stackLimit - StoredThing.stackCount,
                                OutputSettings.max - StoredThing.stackCount));

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

                if (OutputSettings.useMax)
                {
                    num = Mathf.Min(element.stackCount, OutputSettings.max);
                }
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
            if (!PickupFromGround)
                GenDraw.DrawFieldEdges(CellsToSelect.ToList(), Color.green);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;
            foreach (var g2 in StorageSettingsClipboard.CopyPasteGizmosFor(settings))
                yield return g2;
            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel"),
                defaultLabel = "SmartHopper_SetTargetAmount".Translate(),
                action = () => Find.WindowStack.Add(new Dialog_OutputMinMax(OutputSettings))
            };
            if (def.GetModExtension<ModExtension_Settings>()?.GetByName<bool>("pickupFromGround") ?? false)
                yield return new Command_Toggle
                {
                    icon = ContentFinder<Texture2D>.Get("PRFUi/PickupFromGround"),
                    defaultLabel = "SmartHopper_PickupFromGround".Translate(),
                    toggleAction = () => pickupFromGround = !pickupFromGround,
                    isActive = () => pickupFromGround
                };
        }
    }
}