using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones
{
    public abstract class Building_WorkGiverDroneStation : Building_DroneStation
    {
        public virtual IEnumerable<WorkTypeDef> WorkTypes
        {
            get
            {
                return extension.workTypes;
            }
        }

        public override Job TryGiveJob()
        {
            Job result = null;
            Pawn pawn = MakeDrone();
            GenSpawn.Spawn(pawn, Position, Map);
            pawn.workSettings = new Pawn_WorkSettings(pawn);
            pawn.workSettings.EnableAndInitialize();
            pawn.workSettings.DisableAll();
            
            foreach (WorkTypeDef def in WorkTypes)
            {
                pawn.workSettings.SetPriority(def, 3);
            }
            result = TryIssueJobPackageDrone(pawn, true).Job;
            if (result == null)
            {
                result = TryIssueJobPackageDrone(pawn, false).Job;
            }
            pawn.Destroy();
            Notify_DroneGained();
            return result;
        }

        // Method from RimWorld.JobGiver_Work.TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        // I modified the line if (!workGiver.ShouldSkip(pawn))
#pragma warning disable
        public ThinkResult TryIssueJobPackageDrone(Pawn pawn, bool emergency)
        {
            List<WorkGiver> list = emergency ? pawn.workSettings.WorkGiversInOrderEmergency : pawn.workSettings.WorkGiversInOrderNormal;
            int num = -999;
            TargetInfo targetInfo = TargetInfo.Invalid;
            WorkGiver_Scanner workGiver_Scanner = null;
            for (int j = 0; j < list.Count; j++)
            {
                WorkGiver workGiver = list[j];
                if (workGiver.def.priorityInType != num && targetInfo.IsValid)
                {
                    break;
                }
                if (!workGiver.ShouldSkip(pawn))
                {
                    try
                    {
                        Job job2 = workGiver.NonScanJob(pawn);
                        if (job2 != null)
                        {
                            return new ThinkResult(job2, null, new JobTag?(list[j].def.tagToGive), false);
                        }
                        WorkGiver_Scanner scanner = workGiver as WorkGiver_Scanner;
                        if (scanner != null)
                        {
                            if (scanner.def.scanThings)
                            {
                                Predicate<Thing> predicate = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t, false);
                                IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);
                                Thing thing;
                                if (scanner.Prioritized)
                                {
                                    IEnumerable<Thing> enumerable2 = enumerable;
                                    if (enumerable2 == null)
                                    {
                                        enumerable2 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    if (scanner.AllowUnreachable)
                                    {
                                        IntVec3 position = pawn.Position;
                                        IEnumerable<Thing> searchSet = enumerable2;
                                        Predicate<Thing> validator = predicate;
                                        thing = GenClosest.ClosestThing_Global(position, searchSet, 99999f, validator, (Thing x) => scanner.GetPriority(pawn, x));
                                    }
                                    else
                                    {
                                        IntVec3 position = pawn.Position;
                                        Map map = pawn.Map;
                                        IEnumerable<Thing> searchSet = enumerable2;
                                        PathEndMode pathEndMode = scanner.PathEndMode;
                                        TraverseParms traverseParams = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn), TraverseMode.ByPawn, false);
                                        Predicate<Thing> validator = predicate;
                                        thing = GenClosest.ClosestThing_Global_Reachable(position, map, searchSet, pathEndMode, traverseParams, 9999f, validator, (Thing x) => scanner.GetPriority(pawn, x));
                                    }
                                }
                                else if (scanner.AllowUnreachable)
                                {
                                    IEnumerable<Thing> enumerable3 = enumerable;
                                    if (enumerable3 == null)
                                    {
                                        enumerable3 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    IntVec3 position = pawn.Position;
                                    IEnumerable<Thing> searchSet = enumerable3;
                                    Predicate<Thing> validator = predicate;
                                    thing = GenClosest.ClosestThing_Global(position, searchSet, 99999f, validator, null);
                                }
                                else
                                {
                                    IntVec3 position = pawn.Position;
                                    Map map = pawn.Map;
                                    ThingRequest potentialWorkThingRequest = scanner.PotentialWorkThingRequest;
                                    PathEndMode pathEndMode = scanner.PathEndMode;
                                    TraverseParms traverseParams = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn), TraverseMode.ByPawn, false);
                                    Predicate<Thing> validator = predicate;
                                    bool forceGlobalSearch = enumerable != null;
                                    thing = GenClosest.ClosestThingReachable(position, map, potentialWorkThingRequest, pathEndMode, traverseParams, 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, forceGlobalSearch, RegionType.Set_Passable, false);
                                }
                                if (thing != null)
                                {
                                    targetInfo = thing;
                                    workGiver_Scanner = scanner;
                                }
                            }
                            if (scanner.def.scanCells)
                            {
                                IntVec3 position2 = pawn.Position;
                                float num2 = 99999f;
                                float num3 = float.MinValue;
                                bool prioritized = scanner.Prioritized;
                                bool allowUnreachable = scanner.AllowUnreachable;
                                Danger maxDanger = scanner.MaxPathDanger(pawn);
                                foreach (IntVec3 intVec in scanner.PotentialWorkCellsGlobal(pawn))
                                {
                                    bool flag = false;
                                    float num4 = (float)(intVec - position2).LengthHorizontalSquared;
                                    float num5 = 0f;
                                    if (prioritized)
                                    {
                                        if (scanner.HasJobOnCell(pawn, intVec))
                                        {
                                            if (!allowUnreachable && !pawn.CanReach(intVec, scanner.PathEndMode, maxDanger, false, TraverseMode.ByPawn))
                                            {
                                                continue;
                                            }
                                            num5 = scanner.GetPriority(pawn, intVec);
                                            if (num5 > num3 || (num5 == num3 && num4 < num2))
                                            {
                                                flag = true;
                                            }
                                        }
                                    }
                                    else if (num4 < num2 && scanner.HasJobOnCell(pawn, intVec))
                                    {
                                        if (!allowUnreachable && !pawn.CanReach(intVec, scanner.PathEndMode, maxDanger, false, TraverseMode.ByPawn))
                                        {
                                            continue;
                                        }
                                        flag = true;
                                    }
                                    if (flag)
                                    {
                                        targetInfo = new TargetInfo(intVec, pawn.Map, false);
                                        workGiver_Scanner = scanner;
                                        num2 = num4;
                                        num3 = num5;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Concat(new object[]
                        {
                            pawn,
                            " threw exception in WorkGiver ",
                            workGiver.def.defName,
                            ": ",
                            ex.ToString()
                        }));
                    }
                    finally
                    {
                    }
                    if (targetInfo.IsValid)
                    {
                        Job job3;
                        if (targetInfo.HasThing)
                        {
                            job3 = workGiver_Scanner.JobOnThing(pawn, targetInfo.Thing, false);
                        }
                        else
                        {
                            job3 = workGiver_Scanner.JobOnCell(pawn, targetInfo.Cell);
                        }
                        if (job3 != null)
                        {
                            return new ThinkResult(job3, null, new JobTag?(list[j].def.tagToGive), false);
                        }
                        Log.ErrorOnce(string.Concat(new object[]
                        {
                            workGiver_Scanner,
                            " provided target ",
                            targetInfo,
                            " but yielded no actual job for pawn ",
                            pawn,
                            ". The CanGiveJob and JobOnX methods may not be synchronized."
                        }), 6112651);
                    }
                    num = workGiver.def.priorityInType;
                }
            }
            return ThinkResult.NoJob;
        }
#pragma warning restore
    }
}
