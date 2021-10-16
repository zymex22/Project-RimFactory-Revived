using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;


namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(CompLaunchable), "TryLaunch")]
    class Patch_CompLaunchable_TryLaunch
    {
            static bool Prefix(CompLaunchable __instance, int destinationTile, RimWorld.Planet.TransportPodsArrivalAction arrivalAction)
            {
            ProjectRimFactory.Industry.Building_DropPodLoader building;
            if (__instance.parent.Map.GetComponent<PRFMapComponent>().CompLaunchableSelectTargetAtCell(__instance.parent.Position,out  building))
            {
                building.DestinationTile = destinationTile;
                if (arrivalAction is not null && (arrivalAction is RimWorld.Planet.TransportPodsArrivalAction_LandInSpecificCell lasc))
                {
                    building.DestinationCell = (IntVec3)ProjectRimFactory.SAL3.ReflectionUtility.LandInSpecificCellGetCell.GetValue(lasc);
                }
                __instance.parent.Map.GetComponent<PRFMapComponent>().DeRegisterCompLaunchableSelectTarget();
                Log.Message("Patch");
                return false;
            }
            Log.Message("Not Patch");
            return true;
            }
    }
}
