using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones
{
    public class DefModExtension_DroneStation : DefModExtension, Common.IXMLThingDescription
    {
        public int maxNumDrones;
        public bool displayDormantDrones;
        public List<WorkTypeDef> workTypes;
        public int SquareJobRadius = 0; //0 Means infinite
        public string Sleeptimes = ""; //Comma seperated List of sleep Times

        private int spawnWithDrones = 0;
        private bool spawnWithFullDrones = false;
        
        public string[] GetSleepTimesString()
        {
            return Sleeptimes.Split(',');
        }

        public int[] GetSleepTimesInt()
        {
            var times = GetSleepTimesString();
            var result = new List<int>();
            for (var i = 0; i < times.Length; i++)
            {
                if (int.TryParse(times[i], out var number))
                {
                    result.Add(number);
                }
            }
            return result.ToArray();
        }
        
        public string GetDescription(ThingDef def)
        {
            var propertiesRefuel = def.GetCompProperties<CompProperties_Refuelable>();
            var maxDrones = maxNumDrones;
            if (propertiesRefuel != null)
            {
                maxDrones = (int)propertiesRefuel.fuelCapacity;
            }

            var text = "PRF_UTD_DefModExtension_DroneStation_MaxDrones".Translate(maxDrones) + "\r\n";
            text += "PRF_UTD_DefModExtension_DroneStation_IncludedDrones".Translate(GetDronesOnSpawn(null, propertiesRefuel)) + "\r\n";
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
        /// Returns the number of Drones that should be available on Spawn.
        /// </summary>
        public int GetDronesOnSpawn(CompRefuelable fuelComp = null, CompProperties_Refuelable fuelCompProperties = null)
        {
            var props = fuelComp?.Props ?? fuelCompProperties;
            if (spawnWithFullDrones)
            {
                return (int)(props?.fuelCapacity ?? maxNumDrones);
            }
            return Mathf.Clamp(spawnWithDrones, 0, (int)(props?.fuelCapacity ?? maxNumDrones));
        }



    }
}
