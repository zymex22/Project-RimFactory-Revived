using RimWorld;
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
        
        public override Job TryGiveJob(Pawn pawn)
        {
            if (CachedSleepTimeList.Contains(GenLocalDate.HourOfDay(this).ToString())) return null;
            if (workSettings == null)
            {
                pawn.workSettings = new Pawn_WorkSettings(pawn);
                pawn.workSettings.EnableAndInitialize();
                pawn.workSettings.DisableAll();
                workSettings = pawn.workSettings;
            }
            else
            {
                pawn.workSettings = workSettings;
            }
            
            //Set the workSettings based upon the settings
            foreach (var workTypeDef in WorkSettings.Keys)
            {
                pawn.workSettings.SetPriority(workTypeDef, WorkSettings[workTypeDef] ? 3 : 0);
            }
            
            // The check with emergency = true; is required for Emergency Jobs such as Firefighting
            // TODO I wonder if we can throttle those calls & how much of an impact that would make
            var giver = new JobGiver_Work
            {
                emergency = true
            };
            var job = giver.TryIssueJobPackage(pawn, default).Job;
            if (job != null) return job;
            // Search for Regular Jobs
            giver.emergency = false;
            return giver.TryIssueJobPackage(pawn, default).Job;
        }
    }
}