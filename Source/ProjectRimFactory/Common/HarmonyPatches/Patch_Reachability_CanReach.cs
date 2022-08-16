using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //Managed by ConditionalPatchHelper.Update_Patch_Reachability_CanReach
    class Patch_Reachability_CanReach
    {
        public static bool Prefix(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams ,out bool __result,Map ___map, Reachability __instance)
        {
            __result = false;
            if (dest.Thing == null || dest.Thing.def.category != ThingCategory.Item) return true;
            var mapcomp = PatchStorageUtil.GetPRFMapComponent(___map);
            if(mapcomp == null) return true;
            //Not optimal lets correct that later
            var ThingPos = dest.Thing.Position;
            if (mapcomp.ShouldHideItemsAtPos(ThingPos))
            {
                //Is in a DSU
                var pathToIO = mapcomp.GetadvancedIOLocations.Where(p => p.Value.boundStorageUnit?.Position == ThingPos && __instance.CanReach(start, p.Key, PathEndMode.Touch, traverseParams)).Any();
                if (pathToIO)
                {
                    __result = true;
                    return false;
                }
            }



            return true;
        }

    }
}
