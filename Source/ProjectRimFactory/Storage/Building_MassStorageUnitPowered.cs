using ProjectRimFactory.Common.HarmonyPatches;
using ProjectRimFactory.Storage.Editables;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage
{
    [StaticConstructorOnStartup]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Building_MassStorageUnitPowered : Building_MassStorageUnit
    {
        private static readonly Texture2D StoragePawnAccessSwitchIcon = ContentFinder<Texture2D>.Get("PRFUi/dsu");

        //Initialized on spawn
        private CompPowerTrader compPowerTrader;

        public override bool Powered => compPowerTrader?.PowerOn ?? false;

        protected override int MaxNumberItemsInternal => (ModExtensionCrate?.limit ?? int.MaxValue);

        protected override bool CanStoreMoreItems => (Powered) && Spawned &&
                                                     (ModExtensionCrate == null || StoredItemsCount < MaxNumberItemsInternal);
        public override bool CanReceiveIO => base.CanReceiveIO && Powered && Spawned;

        public override bool ForbidPawnInput => ForbidPawnAccess || !pawnAccess || !CanStoreMoreItems;

        public override bool ForbidPawnOutput => ForbidPawnAccess || !pawnAccess;

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

        private void UpdatePowerConsumption()
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
            RefreshStorage();
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
                        Find.WindowStack.Add(new FloatMenu([..DebugActions()]));
                    }
                };
            }

            if (!ForbidPawnAccess)
            {
                yield return new Command_Toggle()
                {
                    defaultLabel = "PRFPawnAccessLabel".Translate(),
                    isActive = () => pawnAccess,
                    toggleAction = () => pawnAccess = !pawnAccess,
                    defaultDesc = "PRFPawnAccessDesc".Translate(),
                    icon = StoragePawnAccessSwitchIcon
                };
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.Deconstruct)
            {
                if (ModExtensionCrate?.destroyContainsItems ?? false)
                {
                    StoredItems.Where(t => !t.Destroyed).ToList().ForEach(x => x.Destroy());
                }
            }
            base.DeSpawn(mode);
        }

        protected virtual IEnumerable<FloatMenuOption> DebugActions()
        {
            yield return new FloatMenuOption("Update power consumption", UpdatePowerConsumption);
            yield return new FloatMenuOption("Log item count", () => Log.Message(StoredItemsCount.ToString()));
        }

        protected override string GetUIThingLabel()
        {
            if ((def.GetModExtension<DefModExtension_Crate>()?.limit).HasValue)
            {
                return "PRFCrateUIThingLabel".Translate(StoredItemsCount, ModExtensionCrate.limit);
            }

            return base.GetUIThingLabel();
        }

        public override string GetITabString(int itemsSelected)
        {
            if ((ModExtensionCrate?.limit).HasValue)
            {
                return "PRFItemsTabLabel_Crate".Translate(StoredItemsCount, ModExtensionCrate!.limit, itemsSelected);
            }

            return base.GetITabString(itemsSelected);
        }
    }
}
