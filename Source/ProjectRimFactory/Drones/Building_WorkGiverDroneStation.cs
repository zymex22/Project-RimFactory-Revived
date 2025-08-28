using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones
{
    public abstract class Building_WorkGiverDroneStation : Building_DroneStation
    {
        public Dictionary<WorkTypeDef, bool> WorkSettingsDict => WorkSettings;


        private Pawn_WorkSettings workSettings;


        //Try give Job to Spawned drone
        // TODO Check that
        public override Job TryGiveJob(Pawn pawn)
        {
            if (CachedSleepTimeList.Contains(GenLocalDate.HourOfDay(this).ToString())) return null;
            if (workSettings == null)
            {
                //This case takes an Average of 3.31ms
                pawn.workSettings = new Pawn_WorkSettings(pawn);
                pawn.workSettings.EnableAndInitialize();
                pawn.workSettings.DisableAll();
                workSettings = pawn.workSettings;
            }
            else
            {
                pawn.workSettings = workSettings;
            }

            //This loop is cheap
            //Set the workSettings based upon the settings
            foreach (var workTypeDef in WorkSettings.Keys)
            {
                pawn.workSettings.SetPriority(workTypeDef, WorkSettings[workTypeDef] ? 3 : 0);
            }
            
            var giver = new JobGiver_Work();
            giver.emergency = true;
            var job = giver.TryIssueJobPackage(pawn, default).Job;
            if (job != null) return job;
            giver.emergency = false;
            return giver.TryIssueJobPackage(pawn, default).Job;
        }
    }
}