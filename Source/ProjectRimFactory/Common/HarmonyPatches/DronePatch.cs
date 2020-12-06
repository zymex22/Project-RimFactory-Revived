using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ProjectRimFactory.Drones;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    internal class Patch_EndCurrentJob_DroneJobs
    {
        private static bool Prefix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob = true,
            bool canReturnToPool = true)
        {
            //Only run the Prefix if its a Drone and the Expected Error Condition did occur
            if (Traverse.Create(__instance).Field<Pawn>("pawn").Value.kindDef == PRFDefOf.PRFDroneKind &&
                (condition == JobCondition.ErroredPather || condition == JobCondition.Errored))
            {
                //Display Warning & Affected Cell info
                Log.Warning("Pathing Failed Drone Returning to Station - (This is a Rimwold Pathing Bug)");
                Log.Message("Target is " + __instance.curJob.GetTarget(TargetIndex.A).Cell);

                //Run default Code (may need to update that in the Future (if RW Updates This Method))
                var jobDef = __instance.curJob != null ? __instance.curJob.def : null;
                Traverse.Create(__instance).Method("CleanupCurrentJob", condition, true, true, canReturnToPool)
                    .GetValue();
                //Send the Drone Home
                var drone = Traverse.Create(__instance).Field<Pawn_Drone>("pawn").Value;
                __instance.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.station));


                return false;
            }

            return true;
        }
    }


    //This Patch Prevents Drones from Uninstalling or Deconstructing their own Station
    [HarmonyPatch(typeof(WorkGiver_RemoveBuilding), "PotentialWorkThingsGlobal")]
    internal class Patch_PotentialWorkThingsGlobal_DronesRenoveOwnBase
    {
        private static void Postfix(Pawn pawn, ref IEnumerable<Thing> __result)
        {
            if (pawn.kindDef == PRFDefOf.PRFDroneKind)
            {
                var drone = (Pawn_Drone) pawn;
                var DroneStationPos = drone.station.Position;

                //Remove work on the station itself
                __result = __result.Where(u => u.Position != DroneStationPos).ToList();
            }
        }
    }
}