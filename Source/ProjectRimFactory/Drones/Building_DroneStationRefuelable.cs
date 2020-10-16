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

        protected bool postmakeflag = false;

        public override void PostMake()
        {
            base.PostMake();
            postmakeflag = true;
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            //this needs to be called before base.SpawnSetup for the init implementation of droneSkillsRecord in Building_DroneStation
            refuelableComp = GetComp<CompRefuelable>(); 

            base.SpawnSetup(map, respawningAfterLoad);
            //Due to the implementation of droneSkillsRecord in Building_DroneStation this code needs to run after base.SpawnSetup
            //Flag is used to ensure it only runs after a post make.
            if (postmakeflag) 
            {
                refuelableComp = GetComp<CompRefuelable>();
                refuelableComp.Refuel(extension.GetDronesOnSpawn(refuelableComp));
                postmakeflag = false;
            }
            
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
