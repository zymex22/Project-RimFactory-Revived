using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones
{
    public abstract class Building_WorkGiverDroneStation : Building_DroneStation
    {
        private Pawn_WorkSettings workSettings;

        public virtual IEnumerable<WorkTypeDef> WorkTypes => extension.workTypes;

        public virtual Dictionary<WorkTypeDef, bool> WorkSettings_dict => WorkSettings;

        //TODO Finding a good way to Cache pawn.workSettings may increase performence
        public override Job TryGiveJob()
        {
            Job result = null;
            if (!cachedSleepTimeList.Contains(GenLocalDate.HourOfDay(this).ToString()))
            {
                //Only plausibel enhancment would be to cache the pawn
                //MakeDrone Average time of 1ms 
                Pawn pawn = MakeDrone();

                //Spawn is cheap
                GenSpawn.Spawn(pawn, Position, Map);

                if (workSettings == null)
                {
                    //This case takes an Average of 3.31ms
                    pawn.workSettings = new Pawn_WorkSettings(pawn);
                    pawn.workSettings.EnableAndInitialize();
                    pawn.workSettings.DisableAll();
                    workSettings = pawn.workSettings;
                }
                else
                {
                    pawn.workSettings = workSettings;
                }

                //This loop is cheap
                //Set the workSettings based upon the settings
                foreach (var def in WorkSettings.Keys)
                    if (WorkSettings[def])
                        pawn.workSettings.SetPriority(def, 3);
                    else
                        pawn.workSettings.SetPriority(def, 0);

                //Each call to TryIssueJobPackageDrone takes an Average of 1ms
                result = TryIssueJobPackageDrone(pawn, true).Job;
                if (result == null) result = TryIssueJobPackageDrone(pawn, false).Job;

                pawn.Destroy();
                Notify_DroneGained();
            }

            return result;
        }

        // Method from RimWorld.JobGiver_Work.TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        // I modified the line if (!workGiver.ShouldSkip(pawn))
#pragma warning disable
        public ThinkResult TryIssueJobPackageDrone(Pawn pawn, bool emergency)
        {
            var list = emergency
                ? pawn.workSettings.WorkGiversInOrderEmergency
                : pawn.workSettings.WorkGiversInOrderNormal;
            var num = -999;
            var targetInfo = TargetInfo.Invalid;
            WorkGiver_Scanner workGiver_Scanner = null;
            for (var j = 0; j < list.Count; j++)
            {
                var workGiver = list[j];
                if (workGiver.def.priorityInType != num && targetInfo.IsValid) break;
                if (!workGiver.ShouldSkip(pawn))
                {
                    try
                    {
                        var job2 = workGiver.NonScanJob(pawn);
                        if (job2 != null) return new ThinkResult(job2, null, list[j].def.tagToGive);
                        var scanner = workGiver as WorkGiver_Scanner;
                        if (scanner != null)
                        {
                            if (scanner.def.scanThings)
                            {
                                Predicate<Thing> predicate = t =>
                                    !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
                                var enumerable = scanner.PotentialWorkThingsGlobal(pawn);
                                Thing thing;
                                if (scanner.Prioritized)
                                {
                                    var enumerable2 = enumerable;
                                    if (enumerable2 == null)
                                        enumerable2 =
                                            pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    if (scanner.AllowUnreachable)
                                    {
                                        var position = pawn.Position;
                                        var searchSet = enumerable2;
                                        var validator = predicate;
                                        thing = GenClosest.ClosestThing_Global(position, searchSet, 99999f, validator,
                                            x => scanner.GetPriority(pawn, x));
                                    }
                                    else
                                    {
                                        var position = pawn.Position;
                                        var map = pawn.Map;
                                        var searchSet = enumerable2;
                                        var pathEndMode = scanner.PathEndMode;
                                        var traverseParams = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn));
                                        var validator = predicate;
                                        thing = GenClosest.ClosestThing_Global_Reachable(position, map, searchSet,
                                            pathEndMode, traverseParams, 9999f, validator,
                                            x => scanner.GetPriority(pawn, x));
                                    }
                                }
                                else if (scanner.AllowUnreachable)
                                {
                                    var enumerable3 = enumerable;
                                    if (enumerable3 == null)
                                        enumerable3 =
                                            pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    var position = pawn.Position;
                                    var searchSet = enumerable3;
                                    var validator = predicate;
                                    thing = GenClosest.ClosestThing_Global(position, searchSet, 99999f, validator);
                                }
                                else
                                {
                                    var position = pawn.Position;
                                    var map = pawn.Map;
                                    var potentialWorkThingRequest = scanner.PotentialWorkThingRequest;
                                    var pathEndMode = scanner.PathEndMode;
                                    var traverseParams = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn));
                                    var validator = predicate;
                                    var forceGlobalSearch = enumerable != null;
                                    thing = GenClosest.ClosestThingReachable(position, map, potentialWorkThingRequest,
                                        pathEndMode, traverseParams, 9999f, validator, enumerable, 0,
                                        scanner.MaxRegionsToScanBeforeGlobalSearch, forceGlobalSearch);
                                    if (scanner is WorkGiver_ConstructDeliverResourcesToBlueprints)
                                        //Preforme further checks too see if this is a reinstall attempt of its own station
                                        if (thing is Blueprint_Install)
                                        {
                                            var bpthing = (Blueprint_Install) thing;
                                            var pd = (Pawn_Drone) pawn;
                                            if (bpthing.MiniToInstallOrBuildingToReinstall == pd.station)
                                                //This is a reinstall attempt - Prevent by setting thing to null
                                                thing = null;
                                        }
                                }

                                if (thing != null)
                                {
                                    targetInfo = thing;
                                    workGiver_Scanner = scanner;
                                }
                            }

                            if (scanner.def.scanCells)
                            {
                                var position2 = pawn.Position;
                                var num2 = 99999f;
                                var num3 = float.MinValue;
                                var prioritized = scanner.Prioritized;
                                var allowUnreachable = scanner.AllowUnreachable;
                                var maxDanger = scanner.MaxPathDanger(pawn);
                                foreach (var intVec in scanner.PotentialWorkCellsGlobal(pawn))
                                {
                                    var flag = false;
                                    float num4 = (intVec - position2).LengthHorizontalSquared;
                                    var num5 = 0f;
                                    if (prioritized)
                                    {
                                        if (scanner.HasJobOnCell(pawn, intVec))
                                        {
                                            if (!allowUnreachable &&
                                                !pawn.CanReach(intVec, scanner.PathEndMode, maxDanger)) continue;
                                            num5 = scanner.GetPriority(pawn, intVec);
                                            if (num5 > num3 || num5 == num3 && num4 < num2) flag = true;
                                        }
                                    }
                                    else if (num4 < num2 && scanner.HasJobOnCell(pawn, intVec))
                                    {
                                        if (!allowUnreachable && !pawn.CanReach(intVec, scanner.PathEndMode, maxDanger))
                                            continue;
                                        flag = true;
                                    }

                                    if (flag)
                                    {
                                        targetInfo = new TargetInfo(intVec, pawn.Map);
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
                        Log.Error(string.Concat(pawn, " threw exception in WorkGiver ", workGiver.def.defName, ": ",
                            ex.ToString()));
                    }

                    if (targetInfo.IsValid)
                    {
                        Job job3;
                        if (targetInfo.HasThing)
                            job3 = workGiver_Scanner.JobOnThing(pawn, targetInfo.Thing);
                        else
                            job3 = workGiver_Scanner.JobOnCell(pawn, targetInfo.Cell);
                        if (job3 != null) return new ThinkResult(job3, null, list[j].def.tagToGive);
                        Log.ErrorOnce(
                            string.Concat(workGiver_Scanner, " provided target ", targetInfo,
                                " but yielded no actual job for pawn ", pawn,
                                ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
                    }

                    num = workGiver.def.priorityInType;
                }
            }

            return ThinkResult.NoJob;
        }
#pragma warning restore
    }
}