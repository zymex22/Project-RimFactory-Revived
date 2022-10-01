using HarmonyLib;
using ProjectRimFactory.Drones;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    class Patch_EndCurrentJob_DroneJobs
    {

        static bool Prefix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob = true, bool canReturnToPool = true)
        {

            //Only run the Prefix if its a Drone and the Expected Error Condition did occur
            if (Traverse.Create(__instance).Field<Pawn>("pawn").Value.kindDef == PRFDefOf.PRFDroneKind && (condition == JobCondition.ErroredPather || condition == JobCondition.Errored))
            {
                //Display Warning & Affected Cell info
                Log.Warning("Pathing Failed Drone Returning to Station - (This is a Rimwold Pathing Bug)");
                Log.Message("Target is " + __instance.curJob.GetTarget(TargetIndex.A).Cell);

                //Run default Code (may need to update that in the Future (if RW Updates This Method))
                JobDef jobDef = (__instance.curJob != null) ? __instance.curJob.def : null;
                Traverse.Create(__instance).Method("CleanupCurrentJob", condition, true, true, canReturnToPool).GetValue();
                //Send the Drone Home
                Pawn_Drone drone = Traverse.Create(__instance).Field<Pawn_Drone>("pawn").Value;
                __instance.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.station));


                return false;
            }
            return true;


        }
    }


    //This Patch Prevents Drones from Uninstalling or Deconstructing their own Station
    [HarmonyPatch(typeof(WorkGiver_RemoveBuilding), "PotentialWorkThingsGlobal")]
    class Patch_PotentialWorkThingsGlobal_DronesRenoveOwnBase
    {

        static void Postfix(Pawn pawn, ref IEnumerable<Thing> __result)
        {
            if (pawn.kindDef == PRFDefOf.PRFDroneKind)
            {
                Pawn_Drone drone = (Pawn_Drone)pawn;
                IntVec3 DroneStationPos = drone.station.Position;

                //Remove work on the station itself
                __result = __result.Where(u => u.Position != DroneStationPos).ToList();
            }
        }
    }

}
