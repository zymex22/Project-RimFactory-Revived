using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(ForbidUtility), "IsForbidden", typeof(Thing), typeof(Pawn))]
    internal class Patch_ForbidUtility_IsForbidden
    {
        private static bool Prefix(Thing t, Pawn pawn, out bool __result)
        {
            __result = true;
            if (t != null && t.Map != null && t.def.category == ThingCategory.Item)
                if (PatchStorageUtil.Get<IForbidPawnOutputItem>(t.Map, t.Position)?.ForbidPawnOutput ?? false)
                    return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Building_Storage), "Accepts")]
    internal class Patch_Building_Storage_Accepts
    {
        private static bool Prefix(Building_Storage __instance, Thing t, out bool __result)
        {
            __result = false;
            if (PatchStorageUtil.Get<IForbidPawnInputItem>(__instance.Map, __instance.Position)?.ForbidPawnInput ??
                false)
                if (!__instance.slotGroup.HeldThings.Contains(t))
                    return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    internal class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        private static bool Prefix(Vector3 clickPos, Pawn pawn, out List<FloatMenuOption> __result)
        {
            __result = new List<FloatMenuOption>();
            if (PatchStorageUtil.Get<IHideRightClickMenu>(pawn.Map, clickPos.ToIntVec3())?.HideRightClickMenus ??
                false) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Thing), "DrawGUIOverlay")]
    internal class Patch_Thing_DrawGUIOverlay
    {
        private static bool Prefix(Thing __instance)
        {
            if (__instance.def.category == ThingCategory.Item)
                if (PatchStorageUtil.GetWithTickCache<IHideItem>(__instance.Map, __instance.Position)?.HideItems ??
                    false)
                    return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), "Draw")]
    internal class Patch_ThingWithComps_Draw
    {
        private static bool Prefix(Thing __instance)
        {
            if (__instance.def.category == ThingCategory.Item)
                if (PatchStorageUtil.GetWithTickCache<IHideItem>(__instance.Map, __instance.Position)?.HideItems ??
                    false)
                    return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Thing), "Print")]
    internal class Patch_Thing_Print
    {
        private static bool Prefix(Thing __instance, SectionLayer layer)
        {
            if (__instance.def.category == ThingCategory.Item)
                if (PatchStorageUtil.GetWithTickCache<IHideItem>(__instance.Map, __instance.Position)?.HideItems ??
                    false)
                    return false;
            return true;
        }
    }

    internal static class PatchStorageUtil
    {
        private static readonly Dictionary<Tuple<Map, IntVec3, Type>, object> cache =
            new Dictionary<Tuple<Map, IntVec3, Type>, object>();

        private static int lastTick;

        public static T Get<T>(Map map, IntVec3 pos) where T : class
        {
            return pos.IsValid ? pos.GetFirst<T>(map) : null;
        }

        public static T GetWithTickCache<T>(Map map, IntVec3 pos) where T : class
        {
            if (Find.TickManager.TicksGame != lastTick)
            {
                cache.Clear();
                lastTick = Find.TickManager.TicksGame;
            }

            var key = new Tuple<Map, IntVec3, Type>(map, pos, typeof(T));
            if (!cache.TryGetValue(key, out var val))
            {
                val = Get<T>(map, pos);
                cache.Add(key, val);
            }

            return (T) val;
        }
    }

    public interface IHideItem
    {
        bool HideItems { get; }
    }

    public interface IHideRightClickMenu
    {
        bool HideRightClickMenus { get; }
    }

    public interface IForbidPawnOutputItem
    {
        bool ForbidPawnOutput { get; }
    }

    public interface IForbidPawnInputItem : ISlotGroupParent, IHaulDestination
    {
        bool ForbidPawnInput { get; }
    }
}