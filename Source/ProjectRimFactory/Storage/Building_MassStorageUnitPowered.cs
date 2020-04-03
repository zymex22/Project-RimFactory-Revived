using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Storage
{
    public class Building_MassStorageUnitPowered : Building_MassStorageUnit
    {
        public override bool CanStoreMoreItems => GetComp<CompPowerTrader>().PowerOn;
        public override bool CanReceiveIO => base.CanReceiveIO && GetComp<CompPowerTrader>().PowerOn;

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

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(60))
            {
                UpdatePowerConsumption();
            }
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
        }

        protected virtual IEnumerable<FloatMenuOption> DebugActions()
        {
            yield return new FloatMenuOption("Update power consumption", UpdatePowerConsumption);
            yield return new FloatMenuOption("Log item count", () => Log.Message(StoredItemsCount.ToString()));
        }
    }
}
