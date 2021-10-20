using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;



namespace ProjectRimFactory.Common.HarmonyPatches
{

    /// <summary>
    /// Used to Patch Methods containd in StaticConstructorOnStartup Classes
    /// This is needed as Patching would Call those constructrs causing issues with graphics
    /// </summary>
    [HarmonyPatch(typeof(StaticConstructorOnStartupUtility), "CallAll")]
    class Patch_StaticConstructorOnStartupUtility_CallAll
    {
        static void Postfix()
        {
            var harmony = LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().HarmonyInstance;
            Log.Message("PRF - Patching STRS and SOS2 for Building_DropPodLoader");
            
            harmony.Patch(HarmonyLib.AccessTools.Method("SRTS.CompLaunchableSRTS:TryLaunch"), 
                new HarmonyMethod(HarmonyLib.AccessTools.Method("ProjectRimFactory.Common.HarmonyPatches.Patch_CompLaunchableSRTS_TryLaunch:Prefix")));
            harmony.Patch(HarmonyLib.AccessTools.Method("RimWorld.CompShuttleLaunchable:TryLaunch"),
                new HarmonyMethod(HarmonyLib.AccessTools.Method("ProjectRimFactory.Common.HarmonyPatches.Patch_CompLaunchableSOS2_TryLaunch:Prefix")));
          
        }

    }
}
