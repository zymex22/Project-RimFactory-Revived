using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones
{
    public class Building_DroneStationRefuelable : Building_WorkGiverDroneStation
    {
        public override void PostMake()
        {
            base.PostMake();
            //Load the initial Drone Count. This must not happen on each load
            refuelableComp.Refuel(extension.GetDronesOnSpawn(refuelableComp));
        }
        public override int DronesLeft
        {
            get
            {
                return Mathf.RoundToInt(refuelableComp.Fuel) - spawnedDrones.Count;
            }
        }

        public override void Notify_DroneLost()
        {
            refuelableComp.ConsumeFuel(1);
        }
        public override void Notify_DroneGained()
        {
            refuelableComp.Refuel(1);
        }
    }
}
