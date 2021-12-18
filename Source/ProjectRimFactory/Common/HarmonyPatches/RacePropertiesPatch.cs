using HarmonyLib;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{

    [HarmonyPatch(typeof(Pawn), "get_IsColonist")]
    static class Patch_Pawn_IsColonist
    {
        static void Postfix(Pawn __instance, ref bool __result)
        {
            if (!__result && __instance.kindDef == PRFDefOf.PRFDroneKind)
            {
                __result = true;
            }
        }
    }
}