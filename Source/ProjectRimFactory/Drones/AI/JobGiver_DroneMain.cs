using System.Linq;
using ProjectRimFactory.Common;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones.AI
{
    public class JobGiver_DroneMain : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var drone = (Pawn_Drone) pawn;
            if (drone.station != null)
            {
                if (drone.station.Spawned && drone.station.Map == pawn.Map)
                {
                    Job result = null;
                    if (drone.station is Building_WorkGiverDroneStation b)
                    {
                        if (!drone.station.cachedSleepTimeList.Contains(GenLocalDate.HourOfDay(drone).ToString()))
                        {
                            pawn.workSettings = new Pawn_WorkSettings(pawn);
                            pawn.workSettings.EnableAndInitialize();
                            pawn.workSettings.DisableAll();
                            foreach (var def in b.WorkTypes) pawn.workSettings.SetPriority(def, 3);
                            foreach (var def in b.WorkSettings_dict.Keys)
                                if (b.WorkSettings_dict[def])
                                    pawn.workSettings.SetPriority(def, 3);
                                else
                                    pawn.workSettings.SetPriority(def, 0);


                            // So the station finds the best job for the pawn
                            result = b.TryIssueJobPackageDrone(drone, true).Job;
                            if (result == null) result = b.TryIssueJobPackageDrone(drone, false).Job;
                        }
                    }
                    else
                    {
                        result = drone.station.TryGiveJob();
                    }

                    if (result == null) result = new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.station);
                    return result;
                }

                return new Job(PRFDefOf.PRFDrone_SelfTerminate);
            }

            return null;
        }
    }
}