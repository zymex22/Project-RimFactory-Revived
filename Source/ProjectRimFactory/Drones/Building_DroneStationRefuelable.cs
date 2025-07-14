using ProjectRimFactory.Common;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones
{
    public class Building_DroneStationRefuelable : Building_WorkGiverDroneStation, IAdditionalPowerConsumption
    {
        public override void PostMake()
        {
            base.PostMake();
            //Load the initial Drone Count. This must not happen on each load
            RefuelableComp.Refuel(extension.GetDronesOnSpawn(RefuelableComp));
        }
        public override int DronesLeft => Mathf.RoundToInt(RefuelableComp.Fuel) - spawnedDrones.Count;

        Dictionary<string, int> IAdditionalPowerConsumption.AdditionalPowerConsumption => new() { { "Drone Count", (int)(RefuelableComp?.Fuel ?? 0) * 10 } };

        public override void Notify_DroneLost()
        {
            RefuelableComp.ConsumeFuel(1);
            RangePowerSupplyMachine?.RefreshPowerStatus();
        }
        public override void Notify_DroneGained()
        {
            RefuelableComp.Refuel(1);
            RangePowerSupplyMachine?.RefreshPowerStatus();
        }
        
        private int lastFuelCnt = 0;

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;

            if (!this.IsHashIntervalTick(60)) return;
            // Cache Compare, no need to check for tolerances in this case
            if (lastFuelCnt == RefuelableComp.Fuel) return;
            lastFuelCnt = (int)RefuelableComp.Fuel;
            RangePowerSupplyMachine?.RefreshPowerStatus();
        }
    }
}
