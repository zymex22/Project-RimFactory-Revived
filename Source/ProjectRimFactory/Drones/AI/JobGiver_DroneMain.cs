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
            if (drone.BaseStation == null) return null;
            if (!drone.BaseStation.Spawned || drone.BaseStation.Map != pawn.Map)
                return new Job(PRFDefOf.PRFDrone_SelfTerminate);
            Job result;
            if (drone.BaseStation is Building_WorkGiverDroneStation b)
            {
                if (drone.BaseStation.CachedSleepTimeList.Contains(GenLocalDate.HourOfDay(drone).ToString()))
                    return new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.BaseStation);
                pawn.workSettings = new Pawn_WorkSettings(pawn);
                pawn.workSettings.EnableAndInitialize();
                pawn.workSettings.DisableAll();

                foreach (var def in b.WorkSettingsDict.Keys)
                {
                    if (b.WorkSettingsDict[def])
                    {
                        pawn.workSettings.SetPriority(def, 3);
                    }
                    else
                    {
                        pawn.workSettings.SetPriority(def, 0);
                    }
                }


                // So the station finds the best job for the pawn
                result = drone.BaseStation.TryGiveJob(drone);

            }
            else
            {
                result = drone.BaseStation.TryGiveJob(drone);
            }

            return result ?? new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.BaseStation);
        }
    }
}
