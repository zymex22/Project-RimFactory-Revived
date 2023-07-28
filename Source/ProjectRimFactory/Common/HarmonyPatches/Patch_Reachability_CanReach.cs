using ProjectRimFactory.Storage;
using System.Linq;
using System.Security.Cryptography;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //Managed by ConditionalPatchHelper.Update_Patch_Reachability_CanReach
    class Patch_Reachability_CanReach
    {
        //canReachThing Holds the Last item that was checked and Required the use of a Advanced IO Port
        //This is used in other patches to force the use of an IO Port
        private static Thing canReachThing =null;
        public static bool CanReachThing(Thing thing)
        {
            var ret = thing == canReachThing;
            canReachThing = null;
            return ret;
        }

        /// <summary>
        /// This Patch allows Pawns to receive Items from a Advanced IO Port when the direct Path to the DSU(current Item Location) is Blocked
        /// This Patch has a noticeable Performance Impact and shall only be use if the Path is Blocked
        /// </summary>
        /// <param name="start"></param>
        /// <param name="dest"></param>
        /// <param name="peMode"></param>
        /// <param name="traverseParams"></param>
        /// <param name="__result"></param>
        /// <param name="___map"></param>
        /// <param name="__instance"></param>
        public static void Postfix(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result, Map ___map, Reachability __instance)
        {
            //There is already a Path
            if (__result) return;

            var thing = dest.Thing;


            //Ignore everything that is not a Item
            if (thing == null || thing.def.category != ThingCategory.Item) return;

            //Quickly get the Map Component (abort if nonexistent)
            var mapcomp = PatchStorageUtil.GetPRFMapComponent(___map);
            if (mapcomp == null) return;
 
            //Is in a DSU
            if (hasPathToItem(thing,mapcomp,__instance,start,traverseParams))
            {
                canReachThing = thing;
                 __result = true;
            }
        }

        //I think i need to rework how, what and where stuff is save for event caching

        private static bool hasPathToItem(Thing thing, PRFMapComponent mapComp, Reachability reachability, IntVec3 start, TraverseParms traverseParams)
        {
            var ThingPos = thing.Position;

            var ParrentHolder = thing.ParentHolder;

            var IsDSU_Link = mapComp.ShouldHideItemsAtPos(ThingPos);
            var IsColdLink = ParrentHolder is Building_ColdStorage;

            //Quickly Check if the Item is in a Storage Unit
            //TODO: Rework that -> This includes items in PRF Crates & Excludes items from Cold Storage(Note they currently have bigger issues)
            if (! (IsDSU_Link || IsColdLink)) return false;

            var AdvancedIOLocations = mapComp.GetadvancedIOLocations;
            var cnt = AdvancedIOLocations.Count;
            //Check Every Advanced IO Port
            for (int i = 0; i < cnt; i++)
            {
                var current = AdvancedIOLocations.ElementAt(i);

                //Check if that Port has access to the Item
                //TODO: Rework that -> Is the Use of the Position really best?
                var boundStorageUnit = current.Value.boundStorageUnit;
                if ( (IsDSU_Link && boundStorageUnit?.GetPosition == ThingPos) || (IsColdLink &&  ParrentHolder == boundStorageUnit))
                {
                    //The Port has access to the Item
                    //Now check if we can reach that Port
                    if (reachability.CanReach(start, current.Key, PathEndMode.Touch, traverseParams)) return true;
                }

            }

            return false;
        }
    }
}
