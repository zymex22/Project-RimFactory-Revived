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
            {
                return new Job(PRFDefOf.PRFDrone_SelfTerminate);
            }

            if (drone.BaseStation is Building_WorkGiverDroneStation b)
            {
                if (drone.BaseStation.CachedSleepTimeList.Contains(GenLocalDate.HourOfDay(drone).ToString()))
                {
                    return new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.BaseStation);
                }

                pawn.workSettings = new Pawn_WorkSettings(pawn);
                pawn.workSettings.EnableAndInitialize();
                pawn.workSettings.DisableAll();

                foreach (var def in b.WorkSettingsDict.Keys)
                {
                    pawn.workSettings.SetPriority(def, b.WorkSettingsDict[def] ? 3 : 0);
                }
            }

            var result = drone.BaseStation.TryGiveJob(drone);

            return result ?? new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.BaseStation);
        }
    }
}
