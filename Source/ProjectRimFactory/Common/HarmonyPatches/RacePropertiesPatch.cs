using HarmonyLib;
using ProjectRimFactory.Drones;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    // A patch to the problem of forbidding what drones have mined.
    // When mineable yields, if pawn is Drone, Drone will be Colonist.
    [HarmonyPatch(typeof(Mineable), "TrySpawnYield")]
    internal static class Patch_Mineable_TrySpawnYield
    {
        private static void Prefix(Mineable __instance, Map map, float yieldChance, bool moteOnWaste, Pawn pawn)
        {
            if (pawn is Pawn_Drone) Patch_Pawn_IsColonist.overrideIsColonist = true;
        }

        private static void Postfix(Mineable __instance, Map map, float yieldChance, bool moteOnWaste, Pawn pawn)
        {
            if (pawn is Pawn_Drone) Patch_Pawn_IsColonist.overrideIsColonist = false;
        }
    }

    // A patch to the problem of forbidding what drones have mined.
    // When mineable yields, if pawn is Drone, Drone will be Colonist.
    [HarmonyPatch(typeof(Pawn), "get_IsColonist")]
    internal static class Patch_Pawn_IsColonist
    {
        public static bool overrideIsColonist;

        private static void Postfix(Pawn __instance, ref bool __result)
        {
            if (overrideIsColonist && __instance is Pawn_Drone && !__result && __instance.Faction != null &&
                __instance.Faction.IsPlayer) __result = true;
        }
    }
}