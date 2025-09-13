using RimWorld;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Common;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones
{
    public abstract class Building_WorkGiverDroneStation : Building_DroneStation
    {
        public Dictionary<WorkTypeDef, bool> WorkSettingsDict => WorkSettings;
        
        private Pawn_WorkSettings workSettings;
        private bool allowsEmergency;
        private bool configuredAllowsEmergency;
        
        public override Job TryGiveJob(Pawn pawn)
        {
            if (CachedSleepTimeList.Contains(GenLocalDate.HourOfDay(this))) return null;
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
            for (var i = 0; i < WorkSettings.Keys.Count; i++)
            {
                var workTypeDef = WorkSettings.Keys.ElementAt(i);
                pawn.workSettings.SetPriority(workTypeDef, WorkSettings[workTypeDef] ? 3 : 0);
                
                // Check once if we should look for Emergency Work
                if (!configuredAllowsEmergency && StaticEmergencyWorkTypes.EmergencyWorkTypes.Contains(workTypeDef)) 
                {
                    allowsEmergency = true;
                }
            }
            configuredAllowsEmergency = true;
            
            // The check with emergency = true; is required for Emergency Jobs such as Firefighting
            var giver = new JobGiver_Work
            {
                emergency = true
            };
            // Only Search for emergency if it can be successful
            var job = allowsEmergency ? giver.TryIssueJobPackage(pawn, default).Job : null;
            if (job != null) return job;
            // Search for Regular Jobs
            giver.emergency = false;
            return giver.TryIssueJobPackage(pawn, default).Job;
        }
    }
}