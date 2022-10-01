using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(ModContentPack), "get_Patches")]
    class Patch_ModContentPack_Pathes
    {
        static void Postfix(ModContentPack __instance, ref IEnumerable<PatchOperation> __result)
        {
            if (__instance.PackageId.ToLower() == LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().Content.PackageId.ToLower())
            {
                var setting = LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().Settings;
                var patches = setting.Patches;
                int count = 0;

                foreach (PatchOperation patch in patches)
                {
                    count++;
                    patch.sourceFile = "PRF_SettingsPatch_" + count + "_";
                }

                __result = __result.Concat(patches);
            }
        }
    }
}
