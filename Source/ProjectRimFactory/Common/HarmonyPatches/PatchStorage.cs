using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using ProjectRimFactory.Storage;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(ForbidUtility), "IsForbidden", new Type[] { typeof(Thing), typeof(Pawn) })]
    class Patch_ForbidUtility_IsForbidden
    {
        static bool Prefix(Thing t, Pawn pawn, out bool __result)
        {
            __result = true;
            if (t != null && t.Map != null && t.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtil.GetMassStorageUnit(t.Map, t.Position)?.ForbidPawnOutput ?? false)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Building_Storage), "Accepts")]
    class Patch_Building_Storage_Accepts
    {
        static bool Prefix(Building_Storage __instance, Thing t, out bool __result)
        {
            __result = false;
            if (PatchStorageUtil.GetMassStorageUnit(__instance.Map, __instance.Position)?.ForbidPawnInput ?? false)
            {
                if (!__instance.slotGroup.HeldThings.Contains(t))
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        static bool Prefix(Vector3 clickPos, Pawn pawn, out List<FloatMenuOption> __result)
        {
            __result = new List<FloatMenuOption>();
            if (PatchStorageUtil.GetMassStorageUnit(pawn.Map, clickPos.ToIntVec3())?.HideRightClickMenus ?? false)
            {
                return false;
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
                if (PatchStorageUtil.GetMassStorageUnit(__instance.Map, __instance.Position)?.HideItems ?? false)
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
                if (PatchStorageUtil.GetMassStorageUnit(__instance.Map, __instance.Position)?.HideItems ?? false)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Thing), "Print")]
    class Patch_Thing_Print
    {
        static bool Prefix(Thing __instance, SectionLayer layer)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtil.GetMassStorageUnit(__instance.Map, __instance.Position)?.HideItems ?? false)
                {
                    return false;
                }
            }
            return true;
        }
    }

    static class PatchStorageUtil
    {
        public static Building_MassStorageUnit GetMassStorageUnit(Map map, IntVec3 pos)
        {
            return pos.IsValid ? pos.GetThingList(map).Where(t => t is Building_MassStorageUnit).Select(t => (Building_MassStorageUnit)t).FirstOrDefault() : null;
        }
    }
}
