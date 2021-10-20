using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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
                return false;
            }
            return true;
            }
    }

    //This patch is manually called after all StaticConstructorOnStartup are Called via Patch_StaticConstructorOnStartupUtility_CallAll
    class Patch_CompLaunchableSRTS_TryLaunch
    {

       static MethodBase TargetMethod()//The target method is found using the custom logic defined here
        {


            var m = HarmonyLib.AccessTools.Method("SRTS.CompLaunchableSRTS:TryLaunch");
            if (m == null)
            {
                Log.Error("PRF Harmony Error - m == null for Patch_CompLaunchableSRTS_TryLaunch.TargetMethod()");
            }
            return m;
        }
        static bool Prefix(ThingComp __instance, int destinationTile, RimWorld.Planet.TransportPodsArrivalAction arrivalAction)
        {
            ProjectRimFactory.Industry.Building_DropPodLoader building;
            if (__instance.parent.Map.GetComponent<PRFMapComponent>().CompLaunchableSelectTargetAtCell(__instance.parent.Position, out building))
            {
                building.DestinationTile = destinationTile;
                if (arrivalAction is not null && (arrivalAction is RimWorld.Planet.TransportPodsArrivalAction_LandInSpecificCell lasc))
                {
                    building.DestinationCell = (IntVec3)ProjectRimFactory.SAL3.ReflectionUtility.LandInSpecificCellGetCell.GetValue(lasc);
                }
                __instance.parent.Map.GetComponent<PRFMapComponent>().DeRegisterCompLaunchableSelectTarget();
                return false;
            }
            return true;
        }
    }

    //This patch is manually called after all StaticConstructorOnStartup are Called via Patch_StaticConstructorOnStartupUtility_CallAll
    class Patch_CompLaunchableSOS2_TryLaunch
    {
        
        static MethodBase TargetMethod()//The target method is found using the custom logic defined here
        {


            var m = HarmonyLib.AccessTools.Method("RimWorld.CompShuttleLaunchable:TryLaunch");
            if (m == null)
            {
                Log.Error("PRF Harmony Error - m == null for Patch_CompLaunchableSOS2_TryLaunch.TargetMethod()");
            }
            return m;
        }
        static bool Prefix(ThingComp __instance, GlobalTargetInfo target, RimWorld.Planet.TransportPodsArrivalAction arrivalAction)
        {
            ProjectRimFactory.Industry.Building_DropPodLoader building;
            if (__instance.parent.Map.GetComponent<PRFMapComponent>().CompLaunchableSelectTargetAtCell(__instance.parent.Position, out building))
            {
                building.DestinationTile = target.Tile;
                if (arrivalAction is not null && (arrivalAction is RimWorld.Planet.TransportPodsArrivalAction_LandInSpecificCell lasc))
                {
                    building.DestinationCell = (IntVec3)ProjectRimFactory.SAL3.ReflectionUtility.LandInSpecificCellGetCell.GetValue(lasc);
                }
                __instance.parent.Map.GetComponent<PRFMapComponent>().DeRegisterCompLaunchableSelectTarget();
                return false;
            }
            return true;
        }
    }

}
