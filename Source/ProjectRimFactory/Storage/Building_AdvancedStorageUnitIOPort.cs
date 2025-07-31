using ProjectRimFactory.Common;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Storage
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Building_AdvancedStorageUnitIOPort : Building_StorageUnitIOBase
    {
        protected override bool ShowLimitGizmo => false;

        private readonly List<Thing> placementQueue = [];

        public void AddItemToQueue(Thing thing)
        {
            placementQueue.Add(thing);
        }

        public override void DeSpawn(DestroyMode destroyMode = DestroyMode.Vanish)
        {
            Map.GetComponent<PRFMapComponent>().DeRegisteradvancedIOLocations(Position);
            base.DeSpawn(destroyMode);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            map.GetComponent<PRFMapComponent>().RegisteradvancedIOLocations(Position, this);
        }

        public override StorageIOMode IOMode
        {
            get => StorageIOMode.Output;
            set
            {
            }
        }

        public override bool ForbidPawnInput => true;

        private Thing GetStoredItem()
        {
            var map = Map;
            if (map is null)
            {
                Log.Error($"PRF GetStoredItem @{Position} map is null");
                return null;
            }
            return WorkPosition.GetFirstItem(map);
        }

        public bool CanGetNewItem => GetStoredItem() == null && (PowerTrader?.PowerOn ?? false);

        protected override bool IsAdvancedPort => true;

        public void UpdateQueue()
        {
            if (CanGetNewItem && placementQueue.Count > 0)
            {
                var nextItemInQueue = placementQueue[0];
                PlaceThingNow(nextItemInQueue);
                placementQueue.RemoveAt(0);
            }
        }

        public void PlaceThingNow(Thing thing)
        {
            if (thing != null)
            {
                thing.Position = Position;
            }
        }

        protected override void Tick()
        {
            if (!Spawned) return;
            UpdateQueue();

            if (!this.IsHashIntervalTick(10)) return;
            var thing = GetStoredItem();
            if (thing != null && !Map.reservationManager.AllReservedThings().Contains(thing))
            {
                RefreshInput();
            }

        }

    }
}
