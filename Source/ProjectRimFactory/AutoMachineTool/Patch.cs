using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using static ProjectRimFactory.AutoMachineTool.Ops;
using ProjectRimFactory.Storage;

namespace ProjectRimFactory.AutoMachineTool
{
    [HarmonyPatch(typeof(ForbidUtility), "IsForbidden", new Type[] { typeof(Thing), typeof(Pawn) })]
    class Patch_ForbidUtility_IsForbidden
    {
        static bool Prefix(Thing t, Pawn pawn, out bool __result)
        {
            __result = true;
            if (t != null && t.Map != null && t.def.category == ThingCategory.Item)
            {
                if (PatchUtil.InMassStorageUnitPowered(t.Map, t.Position))
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Thing), "DrawGUIOverlay")]
    class Patch_Thing_DrawGUIOverlay
    {
        static bool Prefix(Thing __instance)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchUtil.InMassStorageUnitPowered(__instance.Map, __instance.Position))
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), "Draw")]
    class Patch_ThingWithComps_Draw
    {
        static bool Prefix(Thing __instance)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchUtil.InMassStorageUnitPowered(__instance.Map, __instance.Position))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /*
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    class Patch_FloatMenuMakerMap_AddHumanlikeOrders
    {
        static bool Prefix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (PatchUtil.InMassStorageUnitPowered(pawn.Map, clickPos.ToIntVec3()))
            {
                return false;
            }
            return true;
        }
    }
    */

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        static bool Prefix(Vector3 clickPos, Pawn pawn, out List<FloatMenuOption> __result)
        {
            __result = new List<FloatMenuOption>();
            if (PatchUtil.InMassStorageUnitPowered(pawn.Map, clickPos.ToIntVec3()))
            {
                return false;
            }
            return true;
        }
    }

    static class PatchUtil
    {
        public static bool InMassStorageUnitPowered(Map map, IntVec3 pos)
        {
            return pos.IsValid && pos.GetFirstBuilding(map) is Building_MassStorageUnitPowered;
        }
    }
}
