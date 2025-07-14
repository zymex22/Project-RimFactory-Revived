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
            Pawn_Drone drone = (Pawn_Drone)pawn;
            if (drone.BaseStation != null)
            {
                if (drone.BaseStation.Spawned && drone.BaseStation.Map == pawn.Map)
                {
                    Job result = null;
                    if (drone.BaseStation is Building_WorkGiverDroneStation b)
                    {

                        if (!(drone.BaseStation.CachedSleepTimeList.Contains(GenLocalDate.HourOfDay(drone).ToString())))
                        {
                            pawn.workSettings = new Pawn_WorkSettings(pawn);
                            pawn.workSettings.EnableAndInitialize();
                            pawn.workSettings.DisableAll();

                            foreach (WorkTypeDef def in b.WorkSettingsDict.Keys)
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
                            result = b.TryIssueJobPackageDrone(drone, default, true).Job;
                            if (result == null)
                            {
                                result = b.TryIssueJobPackageDrone(drone, default,false).Job;
                            }

                        }

                    }
                    else
                    {
                        result = drone.BaseStation.TryGiveJob(drone);
                    }
                    if (result == null)
                    {
                        result = new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.BaseStation);
                    }
                    return result;
                }
                return new Job(PRFDefOf.PRFDrone_SelfTerminate);
            }
            return null;
        }
    }
}
