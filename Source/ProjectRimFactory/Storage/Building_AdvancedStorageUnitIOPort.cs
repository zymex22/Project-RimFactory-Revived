using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.Storage
{
    public class Building_AdvancedStorageUnitIOPort : Building_StorageUnitIOBase
    {

        public override bool ShowLimitGizmo => false;

        private List<Thing> placementQueue = new List<Thing>();

        public void AddItemToQueue(Thing thing)
        {
            placementQueue.Add(thing);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.Map.GetComponent<PRFMapComponent>().DeRegisteradvancedIOLocations(this.Position);
            base.DeSpawn(mode);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            map.GetComponent<PRFMapComponent>().RegisteradvancedIOLocations(this.Position, this);
        }

        public override StorageIOMode IOMode
        {
            get
            {
                return StorageIOMode.Output;
            }
            set
            {
            }
        }

        public override bool ForbidPawnInput
        {
            get
            {
                return true;
            }
        }

        private Thing GetstoredItem()
        {
            var map = Map;
            if(map is null)
            {
                Log.Error($"PRF GetstoredItem @{this.Position} map is null");
                return null;
            }
            return WorkPosition.GetFirstItem(Map);
        }

        public bool CanGetNewItem => GetstoredItem() == null && (powerComp?.PowerOn ?? false);

        private void updateQueue()
        {
            if (CanGetNewItem && placementQueue.Count > 0)
            {
                var nextItemInQueue = placementQueue[0];
                if (nextItemInQueue != null)
                {
                    placementQueue[0].Position = this.Position;
                }
                placementQueue.RemoveAt(0);
            }
        }

        public override void Tick()
        {
            updateQueue();

            if (this.IsHashIntervalTick(10))
            {
                var thing = GetstoredItem();
                if (thing != null && !this.Map.reservationManager.AllReservedThings().Contains(thing))
                {
                    RefreshInput();
                }
            }

        }

    }
}
