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
                if (PatchStorageUtil.Get<IForbidPawnOutputItem>(t.Map, t.Position)?.ForbidPawnOutput ?? false)
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
            if ((__instance as IForbidPawnInputItem)?.ForbidPawnInput ?? false)
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
            if (pawn.Map.GetComponent<PRFMapComponent>().iHideRightMenus.Contains(clickPos.ToIntVec3()))
            {
                __result = new List<FloatMenuOption>();
                return false;
            }
            __result = null;
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
                if (PatchStorageUtil.GetPRFMapComponent(__instance.Map)?.CheckIHideItemPos(__instance.Position)?.HideItems ?? false)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), "DrawGUIOverlay")]
    class Patch_ThingWithComps_DrawGUIOverlay
    {
        static bool Prefix(Thing __instance)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtil.GetPRFMapComponent(__instance.Map)?.CheckIHideItemPos(__instance.Position)?.HideItems ?? false)
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
                if (PatchStorageUtil.GetPRFMapComponent(__instance.Map)?.CheckIHideItemPos(__instance.Position)?.HideItems ?? false)
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
                if (PatchStorageUtil.GetPRFMapComponent(__instance.Map)?.CheckIHideItemPos(__instance.Position)?.HideItems ?? false)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MinifiedThing), "Print")]
    class Patch_MinifiedThing_Print
    {
        static bool Prefix(Thing __instance, SectionLayer layer)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtil.GetPRFMapComponent(__instance.Map)?.CheckIHideItemPos(__instance.Position)?.HideItems ?? false)
                {
                    return false;
                }
            }
            return true;
        }
    }

    static class PatchStorageUtil
    {
        private static Dictionary<Tuple<Map, IntVec3, Type>, object> cache = new Dictionary<Tuple<Map, IntVec3, Type>, object>();
        private static int lastTick = 0;
        private static Dictionary<Map, PRFMapComponent> mapComps = new Dictionary<Map, PRFMapComponent>();

        public static PRFMapComponent GetPRFMapComponent (Map map)
        {
            PRFMapComponent outval = null;
            if (!mapComps.TryGetValue(map,out outval))
            {
                outval = map.GetComponent<PRFMapComponent>();
                mapComps.Add(map, outval);
            }
            return outval;
        }

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
            if (!cache.TryGetValue(key, out object val))
            {
                val = Get<T>(map, pos);
                cache.Add(key, val);
            }

            return (T)val;
        }
    }

    public interface IHideItem
    {
        bool HideItems { get; }
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
