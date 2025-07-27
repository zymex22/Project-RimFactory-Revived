using HarmonyLib;
using RimWorld;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local

namespace ProjectRimFactory.Industry.HarmonyPatches
{
    /* Vanilla Deep Drills can disturb insects.  We want the same functionality for PRF miners
     * So, CompCreatesInfestations.
     * However, PRF miners do not use the Deep Drill comp, and the CompCreatesInfestations
     * checks for that comp, or, not finding it, just goes on and checks everything else.
     * However, this means the PRF miners can cause infestations even when powered down.
     * So, we patch CanCreateInfestationNow to fix that:
     */
    [HarmonyPatch(typeof(CompCreatesInfestations), "get_CanCreateInfestationNow")]
    static class Patch_CanCreateInfestationNow
    {
        static bool Prefix(CompCreatesInfestations __instance, ref bool __result)
        {
            if (__instance.parent is not Building_DeepQuarry) return true; // not deep quarry, run vanilla test
            
            if (__instance.parent.GetComp<CompPowerTrader>()?.PowerOn == true &&
                !__instance.CantFireBecauseCreatedInfestationRecently &&
                !__instance.CantFireBecauseSomethingElseCreatedInfestationRecently)
            {
                __result = true;  // can cause infestation
            }
            else
            { // any logic for DeepQuarries that do not use power should go here: (fuel?  etc)
                __result = false;
            }
            return false; // skip vanilla result
        }
    }
}
