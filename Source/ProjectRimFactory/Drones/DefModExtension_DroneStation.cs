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
        public int maxNumDrones = 2;
        public bool displayDormantDrones;
        public List<WorkTypeDef> workTypes;
        public int SquareJobRadius = 0; //0 Means infinite
        public string Sleeptimes = ""; //Comma seperated List of sleep Times

        private int spawnWithDrones = 0;
        private bool spawnWithFullDrones = false;
        
        /// <summary>
        /// Returns the number of Drones that should be availibale on Spawn.
        /// </summary>
        public int GetDronesOnSpawn
        {
            get
            {
                if (spawnWithFullDrones)
                {
                    return maxNumDrones;
                }
                //Helper to find XML Errors
                if (spawnWithDrones > maxNumDrones)
                {
                    Log.Error("PRF XML Config Error in DefModExtension_DroneStation --> spawnWithDrones (" + spawnWithDrones + ") > maxNumDrones (" + maxNumDrones + ")");
                }
                return Mathf.Clamp(spawnWithDrones, 0, maxNumDrones);

            }

        }



    }
}
