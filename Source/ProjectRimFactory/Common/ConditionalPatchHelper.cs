using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;
using Verse.AI;

namespace ProjectRimFactory.Common
{
    public static class ConditionalPatchHelper
    {
        //conditional
        private static Harmony harmony_instance = null;

        private static bool Patch_Reachability_CanReach = false;

        public static void InitHarmony(Harmony harmony)
        {
            harmony_instance = harmony;
        }

        public static void Update_Patch_Reachability_CanReach()
        {
            var patch = AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_Reachability_CanReach), "Prefix");
            var base_m = AccessTools.Method(typeof(Verse.Reachability), "CanReach", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) });
            if (ProjectRimFactory_ModSettings.PRF_Patch_Reachability_CanReach)
            {
                harmony_instance.Patch(base_m, new HarmonyMethod(patch) );
                Patch_Reachability_CanReach = true;
            }
            else if(Patch_Reachability_CanReach)
            {
                harmony_instance.Unpatch(base_m, patch);
                Patch_Reachability_CanReach = false;
            }
        }

    }
}
