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

        static bool Prefix(Pawn_JobTracker __instance,Pawn ___pawn, JobCondition condition, bool startNewJob = true, bool canReturnToPool = true)
        {

            //Only run the Prefix if its a Drone and the Expected Error Condition did occur
            if (___pawn.kindDef == PRFDefOf.PRFDroneKind && (condition == JobCondition.ErroredPather || condition == JobCondition.Errored))
            {
                //Check for a error during PRFDrone_ReturnToStation
                if (__instance.curJob.def == PRFDefOf.PRFDrone_ReturnToStation)
                {
                    IntVec3 pos = ___pawn.Position;
                    Map map = ___pawn.Map;
                    //no path home -> Power down
                    ___pawn.Destroy();
                    
                    //Spawn a drone Module where the drone was
                    GenSpawn.Spawn(PRFDefOf.PRF_DroneModule, pos, map);

                    Log.Warning($"PRF - Drone could not return home from {pos} - Powering down");
                    return false;
                }

                //Display Warning & Affected Cell info
                Log.Warning($"Vanilla Pathing Failed - Drone Returning to Station - {___pawn.Position}->{__instance.curJob.GetTarget(TargetIndex.A).Cell}");

                //Run default Code (may need to update that in the Future (if RW Updates This Method))
                JobDef jobDef = (__instance.curJob != null) ? __instance.curJob.def : null;
                Traverse.Create(__instance).Method("CleanupCurrentJob", condition, true, true, canReturnToPool).GetValue();
                //Send the Drone Home
                Pawn_Drone drone = (Pawn_Drone)___pawn;
                __instance.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.BaseStation));


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
                IntVec3 DroneStationPos = drone.BaseStation.Position;

                //Remove work on the station itself
                __result = __result.Where(u => u.Position != DroneStationPos).ToList();
            }
        }
    }

}
