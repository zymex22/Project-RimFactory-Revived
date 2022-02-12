using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using ProjectRimFactory.Drones;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    static class Patch_Locks2_ConfigRuleRace_Allows
    {

        static public void Prefix(Pawn pawn)
        {
            if (pawn is Pawn_Drone)
            {
                Patch_Pawn_IsColonist.overrideIsColonist = true;
            }
        }

        static public void Postfix(Pawn pawn)
        {
            if (pawn is Pawn_Drone)
            {
                Patch_Pawn_IsColonist.overrideIsColonist = false;
            }
        }
    }



    // A patch to the problem of forbidding what drones have mined.
    // When mineable yields, if pawn is Drone, Drone will be Colonist.
    [HarmonyPatch(typeof(Mineable), "TrySpawnYield")]
    static class Patch_Mineable_TrySpawnYield
    {
        static void Prefix(Mineable __instance, Map map, float yieldChance, bool moteOnWaste, Pawn pawn)
        {
            if (pawn is Pawn_Drone)
            {
                Patch_Pawn_IsColonist.overrideIsColonist = true;
            }
        }

        static void Postfix(Mineable __instance, Map map, float yieldChance, bool moteOnWaste, Pawn pawn)
        {
            if (pawn is Pawn_Drone)
            {
                Patch_Pawn_IsColonist.overrideIsColonist = false;
            }
        }
    }

    // A patch to the problem of forbidding what drones have mined.
    // When mineable yields, if pawn is Drone, Drone will be Colonist.
    [HarmonyPatch(typeof(Pawn), "get_IsColonist")]
    static class Patch_Pawn_IsColonist
    {
        static void Postfix(Pawn __instance, ref bool __result)
        {
            if (overrideIsColonist && __instance is Pawn_Drone && !__result && __instance.Faction != null && __instance.Faction.IsPlayer)
            {
                __result = true;
            }
        }
        public static bool overrideIsColonist = false;
    }
}
