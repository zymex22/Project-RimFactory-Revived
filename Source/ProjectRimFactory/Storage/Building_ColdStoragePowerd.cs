using ProjectRimFactory.Storage.Editables;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage
{
    [StaticConstructorOnStartup]
    // ReSharper disable once UnusedType.Global
    public class Building_ColdStoragePowered : Building_ColdStorage
    {
        private static readonly Texture2D StoragePawnAccessSwitchIcon = ContentFinder<Texture2D>.Get("PRFUi/dsu");

        //Initialized on spawn
        private CompPowerTrader compPowerTrader;

        public override bool Powered => compPowerTrader?.PowerOn ?? false;

        protected override bool CanStoreMoreItems => (Powered) && Spawned &&
                                                     (ModExtensionCrate == null || StoredItemsCount < MaxNumberItemsInternal);
        public override bool CanReceiveIO => base.CanReceiveIO && (compPowerTrader?.PowerOn ?? false) && Spawned;

        private bool pawnAccess = true;

        private void UpdatePowerConsumption()
        {
            compPowerTrader ??= GetComp<CompPowerTrader>();
            compPowerTrader.PowerOutput = -10 * StoredItemsCount;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pawnAccess, "pawnAccess", true);
            compPowerTrader ??= GetComp<CompPowerTrader>();
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (this.IsHashIntervalTick(60))
            {
                UpdatePowerConsumption();
            }
            //ThingOwnerTick
            thingOwner.DoTick();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            compPowerTrader ??= GetComp<CompPowerTrader>();
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
                if (def.GetModExtension<DefModExtension_Crate>()?.destroyContainsItems ?? false)
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
                return "PRFCrateUIThingLabel".Translate(StoredItemsCount, def.GetModExtension<DefModExtension_Crate>().limit);
            }

            return base.GetUIThingLabel();
        }

        public override string GetITabString(int itemsSelected)
        {
            if ((def.GetModExtension<DefModExtension_Crate>()?.limit).HasValue)
            {
                return "PRFItemsTabLabel_Crate".Translate(StoredItemsCount, def.GetModExtension<DefModExtension_Crate>().limit, itemsSelected);
            }

            return base.GetITabString(itemsSelected);
        }

        public override void PostMake()
        {
            base.PostMake();
            thingOwner ??= new ThingOwner<Thing>(this);
        }
    }
}
