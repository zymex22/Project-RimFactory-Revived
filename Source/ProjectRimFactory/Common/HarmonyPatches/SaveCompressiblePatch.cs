﻿using HarmonyLib;
using ProjectRimFactory.Storage;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(CompressibilityDeciderUtility), "IsSaveCompressible")]
    public static class SaveCompressiblePatch
    {
        public static void Postfix(Thing t, ref bool __result)
        {
            if (__result && t.Map != null && t.Position.IsValid &&
                t.Position.GetFirst<Building_MassStorageUnit>(t.Map) is Building_MassStorageUnit) __result = false;
        }
    }
}