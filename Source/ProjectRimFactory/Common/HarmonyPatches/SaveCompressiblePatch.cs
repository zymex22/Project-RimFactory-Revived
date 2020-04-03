using HarmonyLib;
using ProjectRimFactory.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(CompressibilityDeciderUtility), "IsSaveCompressible")]
    public static class SaveCompressiblePatch
    {
        public static void Postfix(Thing t, ref bool __result)
        {
            if (__result && t.Map != null && t.Position.IsValid && t.Position.GetFirstBuilding(t.Map) is Building_MassStorageUnit)
            {
                __result = false;
            }
        }
    }
}
