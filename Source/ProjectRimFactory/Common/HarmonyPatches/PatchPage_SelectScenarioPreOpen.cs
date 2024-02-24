using HarmonyLib;
using RimWorld;
using System.Linq;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //Used for Lite Mode to remove Scenarios (#549)
    [HarmonyPatch(typeof(Page_SelectScenario), "PreOpen")]
    class PatchPage_SelectScenarioPreOpen
    {
        static bool Prefix()
        {
            if (ProjectRimFactory_ModSettings.PRF_LiteMode)
            {
                PRF_CustomizeDefs.RemoveScenario(PRF_CustomizeDefs.ExcludeScenario.ToList());
            }

            return true;
        }
    }
}
