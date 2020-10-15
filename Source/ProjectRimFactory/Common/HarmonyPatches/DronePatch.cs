using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using Verse.AI;
using ProjectRimFactory.Drones;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    class Patch_EndCurrentJob_DroneJobs
    {

        static bool Prefix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob = true, bool canReturnToPool = true)
        {

            //Only run the Prefix if its a Drone and the Expected Error Condition did occur
            if (Traverse.Create(__instance).Field<Pawn>("pawn").Value.kindDef == PRFDefOf.PRFDroneKind && (condition == JobCondition.ErroredPather || condition == JobCondition.Errored) )
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


}
