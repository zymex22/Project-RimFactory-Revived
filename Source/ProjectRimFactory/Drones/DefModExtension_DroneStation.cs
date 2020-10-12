using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace ProjectRimFactory.Drones
{
    public class DefModExtension_DroneStation : DefModExtension
    {
        public int maxNumDrones;
        public bool displayDormantDrones;
        public List<WorkTypeDef> workTypes;
        public int SquareJobRadius = 0; //0 Means infinite
        public string Sleeptimes = ""; //Comma seperated List of sleep Times

        private int spawnWithDrones = 0;
        private bool spawnWithFullDrones = false;
        
        /// <summary>
        /// Returns the number of Drones that should be availibale on Spawn.
        /// </summary>
        public int GetDronesOnSpawn(CompRefuelable fuelcomp = null)
        {

            if (spawnWithFullDrones)
            {
                return (int)(fuelcomp?.Props.fuelCapacity ?? maxNumDrones);
            }
            return Mathf.Clamp(spawnWithDrones, 0, (int)(fuelcomp?.Props.fuelCapacity ?? maxNumDrones));
        }



    }
}
