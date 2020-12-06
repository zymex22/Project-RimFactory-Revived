using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones
{
    public class DefModExtension_DroneStation : DefModExtension
    {
        public bool displayDormantDrones;
        public int maxNumDrones;
        public string Sleeptimes = ""; //Comma seperated List of sleep Times

        private readonly int spawnWithDrones = 0;
        private readonly bool spawnWithFullDrones = false;
        public int SquareJobRadius = 0; //0 Means infinite
        public List<WorkTypeDef> workTypes;

        /// <summary>
        ///     Returns the number of Drones that should be availibale on Spawn.
        /// </summary>
        public int GetDronesOnSpawn(CompRefuelable fuelcomp = null)
        {
            if (spawnWithFullDrones) return (int) (fuelcomp?.Props.fuelCapacity ?? maxNumDrones);
            return Mathf.Clamp(spawnWithDrones, 0, (int) (fuelcomp?.Props.fuelCapacity ?? maxNumDrones));
        }
    }
}