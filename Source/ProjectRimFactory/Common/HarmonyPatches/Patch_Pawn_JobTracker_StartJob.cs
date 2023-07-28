using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common.HarmonyPatches
{

    /// <summary>
    /// Patch for the Building_AdvancedStorageUnitIOPort
    /// Pawns starting Jobs check the IO Port for Items
    /// This affects mostly Bills on Workbenches
    /// </summary>
    [HarmonyPatch(typeof(Verse.AI.Pawn_JobTracker), "StartJob")]
    class Patch_Pawn_JobTracker_StartJob
    {

        private static bool TryGetTargetPos(ref IntVec3 targetPos, bool isHaulJobType, Job newJob, IntVec3 pawnPosition)
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
                //Bill Type Jon
                targetPos = newJob.targetA.Thing?.Position ?? newJob.targetA.Cell;
                if (newJob.targetB == IntVec3.Invalid && (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)) return false;
            }
            return true;
        }

        private static void GetTargetItems(ref List<LocalTargetInfo> TargetItems, bool isHaulJobType, Job newJob)
        {
            if (isHaulJobType)
            {
                TargetItems = new List<LocalTargetInfo>() { newJob.targetA };
            }
            else
            {
                if (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)
                {
                    TargetItems = new List<LocalTargetInfo>() { newJob.targetB };
                }
                else
                {
                    TargetItems = newJob.targetQueueB;
                }
            }
        }


        private static bool ShouldGetItem = false;
        private static Thing Port = null;
        private static LocalTargetInfo _target = null;

        public static bool Prefix(Job newJob, ref Pawn ___pawn, JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true
        , ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false)
        {

           // Log.Message($"{newJob} - {jobGiver} - {___pawn}");
            ShouldGetItem = false;
            if (newJob.def == PRFDefOf.PRF_GotoAdvanced) return true;
            //No random moths eating my cloths
            if (___pawn?.Faction == null || !___pawn.Faction.IsPlayer) return true;
            var prfmapcomp = PatchStorageUtil.GetPRFMapComponent(___pawn.Map);

            //PickUpAndHaul "Compatibility" (by not messing with it)
            if (newJob.def.defName == "HaulToInventory") return true;

            //This is the Position where we need the Item to be at
            IntVec3 targetPos = IntVec3.Invalid;
            var usHaulJobType = newJob.targetA.Thing?.def?.category == ThingCategory.Item;
            if (!TryGetTargetPos(ref targetPos, usHaulJobType, newJob, ___pawn.Position)) return true;

            List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> Ports = AdvancedIO_PatchHelper.GetOrderdAdvancedIOPorts(___pawn.Map, ___pawn.Position, targetPos);
            List<LocalTargetInfo> TargetItems = null;
            GetTargetItems(ref TargetItems, usHaulJobType, newJob);
            foreach (var target in TargetItems)
            {
                if (target.Thing == null)
                {
                    //Log.Error($"ProjectRimfactory - Patch_Pawn_JobTracker_StartJob - Null Thing as Target: {target} - pawn:{___pawn} - Job:{newJob}");
                    continue;
                }

                var DistanceToTarget = AdvancedIO_PatchHelper.CalculatePath(___pawn.Position, target.Cell, targetPos);

                //Quick check if the Item could be in a DSU
                //Might have false Positives They are then filterd by AdvancedIO_PatchHelper.CanMoveItem
                //But should not have false Negatives
                if (prfmapcomp.ShouldHideItemsAtPos(target.Cell))
                {
                    foreach (var port in Ports)
                    {
                        var PortIsCloser = port.Key < DistanceToTarget;
                        if (PortIsCloser || (ConditionalPatchHelper.Patch_Reachability_CanReach.Status && ___pawn.Map.reachability.CanReach(___pawn.Position, target.Thing, Verse.AI.PathEndMode.Touch, TraverseParms.For(___pawn)) && Patch_Reachability_CanReach.CanReachThing(target.Thing)))
                        {
                            if (AdvancedIO_PatchHelper.CanMoveItem(port.Value, target.Cell))
                            {
                                // port.Value.AddItemToQueue(target.Thing);
                                // port.Value.updateQueue();
                                _target = target;
                                Log.Message($"Should have moved {target} x{target.Thing.stackCount} ...");
                                ShouldGetItem = true;
                                Port = port.Value;
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
            }
            return true;
        }

        static bool temp = true;
        public static void Postfix(Job newJob, Pawn_JobTracker __instance, ref Pawn ___pawn, JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true
        , ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false)
        {

            if (ShouldGetItem && temp)
            {
                ShouldGetItem = false;
                //temp= false;
                Log.Message($"Job: {__instance.curJob} Driver: {__instance.curDriver}  Targets: {__instance.curJob.targetA}-{__instance.curJob.targetB}-{__instance.curJob.targetC}");




                __instance.jobQueue.EnqueueFirst(__instance.curJob);
                Job job = JobMaker.MakeJob(PRFDefOf.PRF_GotoAdvanced, Port, _target);
                job.count = _target.Thing.stackCount; // Mathf.Min(t.stackCount, thingOwner.GetCountCanAccept(t));

                __instance.StartJob(job);




                

            }


        }
    }
}
