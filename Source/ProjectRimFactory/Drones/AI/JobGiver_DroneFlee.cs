using ProjectRimFactory.Common;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProjectRimFactory.Drones.AI
{
    public class JobGiver_DroneFlee : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.playerSettings is { UsesConfigurableHostilityResponse: true })
            {
                return null;
            }

            if (ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn))
            {
                return null;
            }

            if (pawn.Map is null) //needed for 1.4
            {
                return null;
            }

            if (pawn.Faction == null)
            {
                var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.AlwaysFlee);
                for (var i = 0; i < list.Count; i++)
                {
                    if (pawn.Position.InHorDistOf(list[i].Position, 18f) && FleeUtility.ShouldFleeFrom(list[i], pawn, false, false))
                    {
                        return ReturnToStationJob((Pawn_Drone)pawn);
                    }
                }
                var job2 = FleeLargeFireJob(pawn);
                if (job2 != null)
                {
                    return job2;
                }
            }
            else if (pawn.GetLord() == null)
            {
                var potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
                for (var j = 0; j < potentialTargetsFor.Count; j++)
                {
                    var thing = potentialTargetsFor[j].Thing;
                    if (pawn.Position.InHorDistOf(thing.Position, 18f) && FleeUtility.ShouldFleeFrom(thing, pawn, false, true))
                    {
                        return ReturnToStationJob((Pawn_Drone)pawn);
                    }
                }
            }

            return null;
        }

        private Job FleeLargeFireJob(Pawn pawn)
        {
            var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Fire);
            if (list.Count < 60)
            {
                return null;
            }

            var tp = TraverseParms.For(pawn);
            Fire closestFire = null;
            var closestDistSq = -1f;
            var firesCount = 0;
            RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region to) => to.Allows(tp, false), delegate (Region x)
            {
                var list2 = x.ListerThings.ThingsInGroup(ThingRequestGroup.Fire);
                for (var i = 0; i < list2.Count; i++)
                {
                    var num = (float)pawn.Position.DistanceToSquared(list2[i].Position);
                    if (num <= 400f)
                    {
                        if (closestFire == null || num < closestDistSq)
                        {
                            closestDistSq = num;
                            closestFire = (Fire)list2[i];
                        }
                        firesCount++;
                    }
                }
                return closestDistSq <= 100f && firesCount >= 60;
            }, 18, RegionType.Set_Passable);
            if (closestDistSq <= 100f && firesCount >= 60)
            {
                var job = ReturnToStationJob((Pawn_Drone)pawn);
                if (job != null)
                {
                    return job;
                }
            }
            return null;
        }

        private static Job ReturnToStationJob(Pawn_Drone drone)
        {
            if (drone.BaseStation != null && drone.Map == drone.BaseStation.Map)
            {
                return new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.BaseStation);
            }
            return null;
        }
    }
}
