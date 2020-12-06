using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(ModContentPack), "get_Patches")]
    internal class Patch_ModContentPack_Pathes
    {
        private static void Postfix(ModContentPack __instance, ref IEnumerable<PatchOperation> __result)
        {
            if (__instance.PackageId.ToLower() ==
                LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().Content.PackageId.ToLower())
            {
                var setting = LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().Settings;
                __result = __result.Concat(setting.Patches);
            }
        }
    }
}