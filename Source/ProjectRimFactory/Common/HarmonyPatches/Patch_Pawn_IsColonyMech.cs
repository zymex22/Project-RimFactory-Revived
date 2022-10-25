using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //Required for the Biotech DLC. If a drone would be considered a ColonyMech this would lead to disabled work types
    [HarmonyPatch(typeof(Pawn), "get_IsColonyMech")]
    public class Patch_Pawn_IsColonyMech
    {
        static bool Prefix(Pawn __instance, ref bool __result)
        {
            if(__instance.kindDef == PRFDefOf.PRFDroneKind)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
