using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace ProjectRimFactory.Drones
{
    public class DefModExtension_DroneStation : DefModExtension , ProjectRimFactory.Common.IXMLThingDescription
    {
        public int maxNumDrones;
        public bool displayDormantDrones;
        public List<WorkTypeDef> workTypes;
        public int SquareJobRadius = 0; //0 Means infinite
        public string Sleeptimes = ""; //Comma seperated List of sleep Times

        private int spawnWithDrones = 0;
        private bool spawnWithFullDrones = false;

        public string GetDescription(ThingDef def)
        {
            string text = "";
            CompProperties_Refuelable refule = (CompProperties_Refuelable)def.comps.Where(c => ( c.compClass == typeof(CompRefuelable))).FirstOrDefault();
            int maxdrones = maxNumDrones;
            if (refule != null)
            {
                maxdrones = (int)refule.fuelCapacity;
            }

            text += "PRF_UTD_DefModExtension_DroneStation_MaxDrones".Translate(maxdrones) + "\r\n";
            text += "PRF_UTD_DefModExtension_DroneStation_IncludedDrones".Translate(GetDronesOnSpawn( null, refule)) + "\r\n";
            if (Sleeptimes != "")
            {
                text += "PRF_UTD_DefModExtension_DroneStation_SleepTimes".Translate(Sleeptimes) + "\r\n";
            }
            if (SquareJobRadius == 0)
            {
                text += "PRF_UTD_DefModExtension_DroneStation_Range".Translate("PRF_UTD_DefModExtension_DroneStation_UnlimitedRange".Translate()) + "\r\n";
            }
            else
            {
                text += "PRF_UTD_DefModExtension_DroneStation_Range".Translate(SquareJobRadius) + "\r\n";
            }

            text += "PRF_UTD_DefModExtension_DroneStation_WorkTypes".Translate() + "\r\n";
            foreach (var wt in workTypes)
            {
                text += "  - " + wt.defName + "\r\n";
            }


            return text;
        }

        /// <summary>
        /// Returns the number of Drones that should be availibale on Spawn.
        /// </summary>
        public int GetDronesOnSpawn(CompRefuelable fuelcomp = null , CompProperties_Refuelable fuelcompp = null)
        {
            CompProperties_Refuelable props = fuelcomp?.Props ?? fuelcompp;
            if (spawnWithFullDrones)
            {
                return (int)(props?.fuelCapacity ?? maxNumDrones);
            }
            return Mathf.Clamp(spawnWithDrones, 0, (int)(props?.fuelCapacity ?? maxNumDrones));
        }



    }
}
