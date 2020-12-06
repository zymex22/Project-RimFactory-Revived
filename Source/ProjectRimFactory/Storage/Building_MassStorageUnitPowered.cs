using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Storage.Editables;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage
{
    [StaticConstructorOnStartup]
    public class Building_MassStorageUnitPowered : Building_MassStorageUnit
    {
        private static readonly Texture2D StoragePawnAccessSwitchIcon = ContentFinder<Texture2D>.Get("PRFUi/dsu");

        private bool pawnAccess = true;

        public override bool CanStoreMoreItems => GetComp<CompPowerTrader>().PowerOn && Spawned &&
                                                  (!def.HasModExtension<DefModExtension_Crate>() ||
                                                   Position.GetThingList(Map).Count(t =>
                                                       t.def.category == ThingCategory.Item) <
                                                   (def.GetModExtension<DefModExtension_Crate>()?.limit ??
                                                    int.MaxValue));

        public override bool CanReceiveIO => base.CanReceiveIO && GetComp<CompPowerTrader>().PowerOn && Spawned;

        public override bool ForbidPawnInput => ForbidPawnAccess || !pawnAccess || !CanStoreMoreItems;

        public override bool ForbidPawnOutput => ForbidPawnAccess || !pawnAccess;

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
            GetComp<CompPowerTrader>().PowerOutput = -10 * StoredItemsCount;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pawnAccess, "pawnAccess", true);
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

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(60)) UpdatePowerConsumption();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            RefreshStorage();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
            if (Prefs.DevMode)
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Debug actions",
                    action = () => { Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>(DebugActions()))); }
                };

            if (!ForbidPawnAccess)
                yield return new Command_Toggle
                {
                    defaultLabel = "PRFPawnAccessLabel".Translate(),
                    isActive = () => pawnAccess,
                    toggleAction = () => pawnAccess = !pawnAccess,
                    defaultDesc = "PRFPawnAccessDesc".Translate(),
                    icon = StoragePawnAccessSwitchIcon
                };
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.Deconstruct)
                if (def.GetModExtension<DefModExtension_Crate>()?.destroyContainsItems ?? false)
                    StoredItems.Where(t => !t.Destroyed).ToList().ForEach(x => x.Destroy());
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
                return "PRFCrateUIThingLabel".Translate(StoredItemsCount,
                    def.GetModExtension<DefModExtension_Crate>().limit);
            return base.GetUIThingLabel();
        }

        public override string GetITabString(int itemsSelected)
        {
            if ((def.GetModExtension<DefModExtension_Crate>()?.limit).HasValue)
                return "PRFItemsTabLabel_Crate".Translate(StoredItemsCount,
                    def.GetModExtension<DefModExtension_Crate>().limit, itemsSelected);
            return base.GetITabString(itemsSelected);
        }
    }
}