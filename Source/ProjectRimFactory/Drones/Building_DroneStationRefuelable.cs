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
        protected CompRefuelable refuelableComp;


        public override void PostMake()
        {
            base.PostMake();

            refuelableComp = GetComp<CompRefuelable>();
            refuelableComp.Refuel(extension.GetDronesOnSpawn(refuelableComp));
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            //this needs to be called before base.SpawnSetup for the init implementation of droneSkillsRecord in Building_DroneStation
            refuelableComp = GetComp<CompRefuelable>(); 

            base.SpawnSetup(map, respawningAfterLoad);
        }
        public override int DronesLeft
        {
            get
            {
                //needed as SpawnSetup is not called if the building is Minified
                if (refuelableComp == null)
                {
                    refuelableComp = GetComp<CompRefuelable>();
                }
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
