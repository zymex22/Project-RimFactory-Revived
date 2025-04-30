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
            refuelableComp.Refuel(extension.GetDronesOnSpawn(refuelableComp));
        }
        public override int DronesLeft => Mathf.RoundToInt(refuelableComp.Fuel) - spawnedDrones.Count;

        Dictionary<string, int> IAdditionalPowerConsumption.AdditionalPowerConsumption => new Dictionary<string, int> { { "Drone Count", (int)(refuelableComp?.Fuel ?? 0) * 10 } };

        public override void Notify_DroneLost()
        {
            refuelableComp.ConsumeFuel(1);
            RangePowerSupplyMachine?.RefreshPowerStatus();
        }
        public override void Notify_DroneGained()
        {
            refuelableComp.Refuel(1);
            RangePowerSupplyMachine?.RefreshPowerStatus();
        }

        private int last_fuel_cnt = 0;

        public override void Tick()
        {
            base.Tick();

            if (this.IsHashIntervalTick(60))
            {
                if (last_fuel_cnt != refuelableComp.Fuel)
                {
                    last_fuel_cnt = (int)refuelableComp.Fuel;
                    RangePowerSupplyMachine?.RefreshPowerStatus();
                }

            }
        }
    }
}
