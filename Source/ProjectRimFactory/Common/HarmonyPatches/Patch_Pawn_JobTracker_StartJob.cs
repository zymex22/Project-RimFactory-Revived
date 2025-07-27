using HarmonyLib;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common.HarmonyPatches
{

    /// <summary>
    /// Patch for the Building_AdvancedStorageUnitIOPort
    /// Pawns starting Jobs check the IO Port for Items
    /// This affects mostly Bills on Workbenches
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    class Patch_Pawn_JobTracker_StartJob
    {
        
        /// <summary>
        /// Returns the Target Position of a Job
        /// </summary>
        /// <param name="targetPos"></param>
        /// <param name="isHaulJobType"></param>
        /// <param name="newJob"></param>
        /// <param name="pawnPosition"></param>
        /// <returns></returns>
        private static bool TryGetTargetPos(out IntVec3 targetPos, bool isHaulJobType, Job newJob, IntVec3 pawnPosition)
        {
            if (isHaulJobType)
            {
                //Haul Type Job
                targetPos = newJob.targetB.Thing?.Position ?? newJob.targetB.Cell;
                if (targetPos == IntVec3.Invalid) targetPos = pawnPosition;
                if (newJob.targetA == null) return false;

            }
            else
            {
                //Bill Type Job
                targetPos = newJob.targetA.Thing?.Position ?? newJob.targetA.Cell;
                if (newJob.targetB == IntVec3.Invalid && (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)) return false;
            }
            return true;
        }
        
        /// <summary>
        /// Returns a list of LocalTargetInfos via <para>TargetItems</para> depending on the Job type
        /// </summary>
        /// <param name="targetItems"></param>
        /// <param name="isHaulJobType"></param>
        /// <param name="newJob"></param>
        private static void GetTargetItems(out List<LocalTargetInfo> targetItems, bool isHaulJobType, Job newJob)
        {
            if (isHaulJobType)
            {
                targetItems = [newJob.targetA];
                return;
            }

            if (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)
            {
                targetItems = [newJob.targetB];
            }
            else
            {
                targetItems = newJob.targetQueueB;
            }
        }


        public static bool Prefix(Job newJob, ref Pawn ___pawn, JobCondition lastJobEndCondition = JobCondition.None,
            ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true
        , ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false)
        {
            // No random moths eating my cloths
            if (___pawn?.Faction is not { IsPlayer: true }) return true;
            // PickUpAndHaul "Compatibility" (by not messing with it)
            if (newJob.def.defName == "HaulToInventory") return true;
            
            // Cache Variables for Performance
            var pawnMap = ___pawn.Map;
            var pawnPos = ___pawn.Position;
            var prfMapComponent = PatchStorageUtil.GetPRFMapComponent(pawnMap);

            
            // Check if this is a Haul Job
            bool usHaulJobType = newJob.targetA.Thing?.def?.category == ThingCategory.Item;
            
            // Get Target Position, Exit on fail
            if (!TryGetTargetPos(out var targetPos, usHaulJobType, newJob, pawnPos)) return true;

            var ports = AdvancedIO_PatchHelper.GetOrderedAdvancedIOPorts(pawnMap, pawnPos, targetPos);
            GetTargetItems(out var targetItems, usHaulJobType, newJob);
            foreach (var target in targetItems)
            {
                if (target.Thing == null)
                {
                    //Log.Error($"ProjectRimfactory - Patch_Pawn_JobTracker_StartJob - Null Thing as Target: {target} - pawn:{___pawn} - Job:{newJob}");
                    continue;
                }

                var distanceToTarget = AdvancedIO_PatchHelper.CalculatePath(pawnPos, target.Cell, targetPos);

                //Quick check if the Item could be in a DSU
                //Might have false Positives They are then filterd by AdvancedIO_PatchHelper.CanMoveItem
                //But should not have false Negatives
                if (!prfMapComponent.ShouldHideItemsAtPos(target.Cell)) continue;
                foreach (var port in ports)
                {
                    var portIsCloser = port.Key < distanceToTarget;
                    if (portIsCloser || (ConditionalPatchHelper.PatchReachabilityCanReach.Status 
                                         && pawnMap.reachability.CanReach(pawnPos, 
                                             target.Thing, PathEndMode.Touch, 
                                             TraverseParms.For(___pawn)) 
                                         && Patch_Reachability_CanReach.CanReachThing(target.Thing)))
                    {
                        if (AdvancedIO_PatchHelper.CanMoveItem(port.Value, target.Cell))
                        {
                            port.Value.AddItemToQueue(target.Thing);
                            port.Value.UpdateQueue();
                                
                            break;
                        }
                    }
                    else
                    {
                        //Since we use a orderd List we know
                        //if one ins further, the same is true for the rest
                        break;
                    }
                }
            }
            return true;
        }
    }
}
