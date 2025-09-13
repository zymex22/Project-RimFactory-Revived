using ProjectRimFactory.Common;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones.AI
{
    public class JobGiver_DroneMain : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var drone = (Pawn_Drone)pawn;
            if (drone.BaseStation is not Building_WorkGiverDroneStation baseStation) return null;
            if (!drone.BaseStation.Spawned || drone.BaseStation.Map != pawn.Map)
            {
                return new Job(PRFDefOf.PRFDrone_SelfTerminate);
            }
            
            // Just in case. should not really occur
            if (pawn.workSettings is null)
            {
                pawn.workSettings = new Pawn_WorkSettings(pawn);
                pawn.workSettings.EnableAndInitialize();
                pawn.workSettings.DisableAll();
            }
            
            // Drones should directly react the Changed Settings
            foreach (var def in baseStation.WorkSettingsDict.Keys)
            {
                pawn.workSettings.SetPriority(def, baseStation.WorkSettingsDict[def] ? 3 : 0);
            }
            
            var result = baseStation.TryGiveJob(drone);
            return result ?? new Job(PRFDefOf.PRFDrone_ReturnToStation, baseStation);
        }
    }
}
