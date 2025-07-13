﻿using ProjectRimFactory.Common.HarmonyPatches;
using ProjectRimFactory.Storage.Editables;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage
{
    [StaticConstructorOnStartup]
    public class Building_MassStorageUnitPowered : Building_MassStorageUnit
    {
        private static Texture2D StoragePawnAccessSwitchIcon = ContentFinder<Texture2D>.Get("PRFUi/dsu", true);

        //Initialized on spawn
        private CompPowerTrader compPowerTrader = null;

        public override bool Powered => compPowerTrader?.PowerOn ?? false;

        public override int MaxNumberItemsInternal => (ModExtension_Crate?.limit ?? int.MaxValue);

        public override bool CanStoreMoreItems => (Powered) && this.Spawned &&
            (ModExtension_Crate == null || StoredItemsCount < MaxNumberItemsInternal);
        public override bool CanReceiveIO => base.CanReceiveIO && Powered && this.Spawned;

        public override bool ForbidPawnInput => this.ForbidPawnAccess || !this.pawnAccess || !this.CanStoreMoreItems;

        public override bool ForbidPawnOutput => this.ForbidPawnAccess || !this.pawnAccess;

        public float ExtraPowerDraw => StoredItems.Count * 10f;

        private bool pawnAccess = true;

        public override void Notify_ReceivedThing(Thing newItem)
        {
            base.Notify_ReceivedThing(newItem);
            UpdatePowerConsumption();
        }
        public override void Notify_LostThing(Thing newItem)
        {
            base.Notify_LostThing(newItem);
            UpdatePowerConsumption();
        }
        public void UpdatePowerConsumption()
        {
            compPowerTrader ??= GetComp<CompPowerTrader>();
            FridgePowerPatchUtil.UpdatePowerDraw(this, compPowerTrader);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pawnAccess, "pawnAccess", true);
            compPowerTrader ??= GetComp<CompPowerTrader>();
        }

        protected override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            switch (signal)
            {
                case "PowerTurnedOn":
                    RefreshStorage();
                    break;
                default:
                    break;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (this.IsHashIntervalTick(60))
            {
                UpdatePowerConsumption();
            }
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            compPowerTrader ??= GetComp<CompPowerTrader>();
            this.RefreshStorage();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos()) yield return g;
            if (Prefs.DevMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "DEBUG: Debug actions",
                    action = () =>
                    {
                        Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>(DebugActions())));
                    }
                };
            }

            if (!this.ForbidPawnAccess)
            {
                yield return new Command_Toggle()
                {
                    defaultLabel = "PRFPawnAccessLabel".Translate(),
                    isActive = () => this.pawnAccess,
                    toggleAction = () => this.pawnAccess = !this.pawnAccess,
                    defaultDesc = "PRFPawnAccessDesc".Translate(),
                    icon = StoragePawnAccessSwitchIcon
                };
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.Deconstruct)
            {
                if (def.GetModExtension<DefModExtension_Crate>()?.destroyContainsItems ?? false)
                {
                    this.StoredItems.Where(t => !t.Destroyed).ToList().ForEach(x => x.Destroy());
                }
            }
            base.DeSpawn(mode);
        }

        protected virtual IEnumerable<FloatMenuOption> DebugActions()
        {
            yield return new FloatMenuOption("Update power consumption", UpdatePowerConsumption);
            yield return new FloatMenuOption("Log item count", () => Log.Message(StoredItemsCount.ToString()));
        }

        public override string GetUIThingLabel()
        {
            if ((def.GetModExtension<DefModExtension_Crate>()?.limit).HasValue)
            {
                return "PRFCrateUIThingLabel".Translate(StoredItemsCount, def.GetModExtension<DefModExtension_Crate>().limit);
            }
            else
            {
                return base.GetUIThingLabel();
            }
        }

        public override string GetITabString(int itemsSelected)
        {
            if ((def.GetModExtension<DefModExtension_Crate>()?.limit).HasValue)
            {
                return "PRFItemsTabLabel_Crate".Translate(StoredItemsCount, def.GetModExtension<DefModExtension_Crate>().limit, itemsSelected);
            }
            else
            {
                return base.GetITabString(itemsSelected);
            }
        }
    }
}
