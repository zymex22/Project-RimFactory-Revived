using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(ModContentPack), "get_Patches")]
    class Patch_ModContentPack_Pathes
    {
        // ReSharper disable once UnusedMember.Local
        static void Postfix(ModContentPack __instance, ref IEnumerable<PatchOperation> __result)
        {
            if (__instance.PackageId.ToLower() == LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().Content.PackageId.ToLower())
            {
                var setting = LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().Settings;
                var patches = setting.Patches.ToList();
                int count = 0;

                foreach (var patch in patches)
                {
                    count++;
                    patch.sourceFile = "PRF_SettingsPatch_" + count + "_";
                }

                __result = __result.Concat(patches);
            }
        }
    }
}
