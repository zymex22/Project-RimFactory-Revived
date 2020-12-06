﻿using HarmonyLib;
using ProjectRimFactory.Storage;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(Thing), "set_Position")]
    public static class SetPositionPatch
    {
        public static void Prefix(Thing __instance, out Building_MassStorageUnit __state)
        {
            __state = null;
            var pos = __instance.Position;
            if (__instance.def.category == ThingCategory.Item && pos.IsValid && __instance.Map != null)
                if (pos.GetFirst<Building_MassStorageUnit>(__instance.Map) is Building_MassStorageUnit b)
                    __state = b;
        }

        public static void Postfix(Thing __instance, Building_MassStorageUnit __state)
        {
            __state?.Notify_LostThing(__instance);
        }
    }
}