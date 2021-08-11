using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;


namespace ProjectRimFactory.Drones.AI
{
    public class JobGiver_DroneMain : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {


            Pawn_Drone drone = (Pawn_Drone)pawn;
            
            if (drone.station != null)
            {
                if (drone.station.Spawned && drone.station.Map == pawn.Map)
                {
                    
                    Job result = null;
                    if (drone.station is Building_WorkGiverDroneStation b)
                    {

                        if (!(drone.station.cachedSleepTimeList.Contains(GenLocalDate.HourOfDay(drone).ToString())))
                        {

                            

                            if (pawn.workSettings == null)
                            {
                                //Log.Message("pawn.workSettings == null");
                                pawn.workSettings = new Pawn_WorkSettings(pawn);
                                pawn.workSettings.EnableAndInitialize();
                                pawn.workSettings.DisableAll();

                                foreach (WorkTypeDef def in b.WorkSettings_dict.Keys)
                                {
                                    if (b.WorkSettings_dict[def])
                                    {
                                        pawn.workSettings.SetPriority(def, 3);
                                    }
                                    else
                                    {
                                        pawn.workSettings.SetPriority(def, 0);
                                    }
                                }
                            }



                            // So the station finds the best job for the pawn
                            //result = b.TryIssueJobPackageDrone(drone, true).Job;
                            //if (result == null)
                            //{
                            //Stopwatch sw = Stopwatch.StartNew();
                            result = b.TryIssueJobPackageDrone(drone, false).Job;
                            /*sw.Stop();
                            if (sw.ElapsedMilliseconds > 5)
                            {
                                Log.Message("took: " + sw.Elapsed.TotalMilliseconds + " ms for " + result);
                            }*/
                            // }


                        }

                    }
                    else
                    {
                        result = drone.station.TryGiveJob();
                    }
                    if (result == null)
                    {
                        result = new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.station);
                    }
                    return result;
                }
                return new Job(PRFDefOf.PRFDrone_SelfTerminate);
            }
            return null;
        }
    }
}
