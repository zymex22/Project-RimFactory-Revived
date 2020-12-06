using ProjectRimFactory.Common;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProjectRimFactory.Drones.AI
{
    public class JobGiver_DroneFlee : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Job result;
            if (pawn.playerSettings != null && pawn.playerSettings.UsesConfigurableHostilityResponse)
            {
                result = null;
            }
            else if (ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn))
            {
                result = null;
            }
            else
            {
                if (pawn.Faction == null)
                {
                    var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.AlwaysFlee);
                    for (var i = 0; i < list.Count; i++)
                        if (pawn.Position.InHorDistOf(list[i].Position, 18f) &&
                            SelfDefenseUtility.ShouldFleeFrom(list[i], pawn, false, false))
                            return ReturnToStationJob((Pawn_Drone) pawn);
                    var job2 = FleeLargeFireJob(pawn);
                    if (job2 != null) return job2;
                }
                else if (pawn.GetLord() == null)
                {
                    var potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
                    for (var j = 0; j < potentialTargetsFor.Count; j++)
                    {
                        var thing = potentialTargetsFor[j].Thing;
                        if (pawn.Position.InHorDistOf(thing.Position, 18f) &&
                            SelfDefenseUtility.ShouldFleeFrom(thing, pawn, false, true))
                            return ReturnToStationJob((Pawn_Drone) pawn);
                    }
                }

                result = null;
            }

            return result;
        }

        private Job FleeLargeFireJob(Pawn pawn)
        {
            var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Fire);
            Job result;
            if (list.Count < 60)
            {
                result = null;
            }
            else
            {
                var tp = TraverseParms.For(pawn);
                Fire closestFire = null;
                var closestDistSq = -1f;
                var firesCount = 0;
                RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (from, to) => to.Allows(tp, false),
                    delegate(Region x)
                    {
                        var list2 = x.ListerThings.ThingsInGroup(ThingRequestGroup.Fire);
                        for (var i = 0; i < list2.Count; i++)
                        {
                            float num = pawn.Position.DistanceToSquared(list2[i].Position);
                            if (num <= 400f)
                            {
                                if (closestFire == null || num < closestDistSq)
                                {
                                    closestDistSq = num;
                                    closestFire = (Fire) list2[i];
                                }

                                firesCount++;
                            }
                        }

                        return closestDistSq <= 100f && firesCount >= 60;
                    }, 18);
                if (closestDistSq <= 100f && firesCount >= 60)
                {
                    var job = ReturnToStationJob((Pawn_Drone) pawn);
                    if (job != null) return job;
                }

                result = null;
            }

            return result;
        }

        public Job ReturnToStationJob(Pawn_Drone drone)
        {
            if (drone.station != null && drone.Map == drone.station.Map)
                return new Job(PRFDefOf.PRFDrone_ReturnToStation, drone.station);
            return null;
        }
    }
}