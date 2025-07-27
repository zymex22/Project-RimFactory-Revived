using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using ProjectRimFactory.Drones;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    // A patch to the problem of forbidding what drones have mined.
    // When mineable yields, if pawn is Drone, Drone will be Colonist.
    [HarmonyPatch(typeof(Mineable), "TrySpawnYield", new System.Type[] { typeof(Map), typeof(bool), typeof(Pawn) })]
    static class Patch_Mineable_TrySpawnYield
    {
        // ReSharper disable once UnusedMember.Local
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        static void Prefix(Mineable __instance, Map map, bool moteOnWaste, Pawn pawn)
        {
            if (pawn is Pawn_Drone)
            {
                Patch_Pawn_IsColonist.OverrideIsColonist = true;
            }
        }

        // ReSharper disable once UnusedMember.Local
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        static void Postfix(Mineable __instance, Map map, bool moteOnWaste, Pawn pawn)
        {
            if (pawn is Pawn_Drone)
            {
                Patch_Pawn_IsColonist.OverrideIsColonist = false;
            }
        }
    }

    // A patch to the problem of forbidding what drones have mined.
    // When mineable yields, if pawn is Drone, Drone will be Colonist.
    [HarmonyPatch(typeof(Pawn), "get_IsColonist")]
    static class Patch_Pawn_IsColonist
    {
        // ReSharper disable once UnusedMember.Local
        static void Postfix(Pawn __instance, ref bool __result)
        {
            if (OverrideIsColonist && __instance is Pawn_Drone && !__result && __instance.Faction is { IsPlayer: true })
            {
                __result = true;
            }
        }
        public static bool OverrideIsColonist;
    }
}
