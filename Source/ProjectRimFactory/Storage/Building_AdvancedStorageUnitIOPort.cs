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
            base.DeSpawn(mode);
            this.Map.GetComponent<PRFMapComponent>().DeRegisteradvancedIOLocations(this.Position);
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

        private Thing storedItem => WorkPosition.GetFirstItem(Map);
        public bool CanGetNewItem => storedItem == null;

        public override void Tick()
        {

            if (CanGetNewItem && placementQueue.Count > 0)
            {
                placementQueue[0].Position = this.Position;
                placementQueue.RemoveAt(0);
            }
            if (this.IsHashIntervalTick(10))
            {
                if (!this.Map.reservationManager.AllReservedThings().Contains(storedItem))
                {
                    RefreshInput();
                }
            }

        }

    }
}
