using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using Verse.AI;
using ProjectRimFactory.Common;


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
        public virtual Dictionary<WorkTypeDef, bool> WorkSettings_dict
        {
            get
            {
                return WorkSettings;
            }
        }


        Pawn_WorkSettings workSettings = null;

        //TODO Finding a good way to Cache pawn.workSettings may increase performence
        public override Job TryGiveJob()
        {


            Job result = null;
            if (!(cachedSleepTimeList.Contains(GenLocalDate.HourOfDay(this).ToString())))
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
                foreach (WorkTypeDef def in WorkSettings.Keys)
                {
                    if (WorkSettings[def])
                    {
                        pawn.workSettings.SetPriority(def, 3);
                    }
                    else
                    {
                        pawn.workSettings.SetPriority(def, 0);
                    }
                }

                //Each call to TryIssueJobPackageDrone takes an Average of 1ms
                result = TryIssueJobPackageDrone(pawn, true).Job;
                if (result == null)
                {
                    result = TryIssueJobPackageDrone(pawn, false).Job;
                }

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
            //You can't prioritize Drones to work a specific Cell
            //if (emergency && pawn.mindState.priorityWork.IsPrioritized)
            //{
            //	List<WorkGiverDef> workGiversByPriority = pawn.mindState.priorityWork.WorkGiver.workType.workGiversByPriority;
            //	for (int i = 0; i < workGiversByPriority.Count; i++)
            //	{
            //		WorkGiver worker = workGiversByPriority[i].Worker;
            //		if (WorkGiversRelatedDrone(pawn.mindState.priorityWork.WorkGiver, worker.def))
            //		{
            //			Job job = GiverTryGiveJobPrioritized(pawn, worker, pawn.mindState.priorityWork.Cell);
            //			if (job != null)
            //			{
            //				job.playerForced = true;
            //				Log.Message(" GiverTryGiveJobPrioritizedDrone(pawn, worker, pawn.mindState.priorityWork.Cell);");
            //				return new ThinkResult(job, null, workGiversByPriority[i].tagToGive);
            //			}
            //		}
            //	}
            //	pawn.mindState.priorityWork.Clear();
            //}
            List<WorkGiver> list = ((!emergency) ? pawn.workSettings.WorkGiversInOrderNormal : pawn.workSettings.WorkGiversInOrderEmergency);
            int num = -999;
            TargetInfo bestTargetOfLastPriority = TargetInfo.Invalid;
            WorkGiver_Scanner scannerWhoProvidedTarget = null;
            WorkGiver_Scanner scanner;
            IntVec3 pawnPosition;
            float closestDistSquared;
            float bestPriority;
            bool prioritized;
            bool allowUnreachable;
            int cellcnt = 0;
            int cellcntnew = 0;
            Danger maxPathDanger;
            for (int j = 0; j < list.Count; j++)
            {
                WorkGiver workGiver = list[j];

                //Abort if seach if there is a Valid target and the next giver laks priority
                if (workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid)
                {
                    break;
                }

                //This should be always true for drones
                //if (!PawnCanUseWorkGiver(pawn, workGiver))
                //{
                //	//Thats it
                //	//Log.Message("Pawn cant work for " + workGiver);
                //	continue;
                //}
                try
                {
                    //Set job2 to null
                    //This check is not applicable for drones --> it shall always be null
                    //Job job2 = workGiver.NonScanJob(pawn);
                    //if (job2 != null)
                    //{
                    //	Log.Message("job2 workGiver.NonScanJob(pawn);");
                    //	return new ThinkResult(job2, null, list[j].def.tagToGive);
                    //}
                    Job job2 = null;
                    scanner = workGiver as WorkGiver_Scanner;
                    if (scanner != null)
                    {
                        if (scanner.def.scanThings)
                        {
                            Predicate<Thing> validator = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
                            IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);
                            Thing thing;
                            if (scanner.Prioritized)
                            {
                                IEnumerable<Thing> enumerable2 = enumerable;
                                if (enumerable2 == null)
                                {
                                    enumerable2 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                }
                                thing = ((!scanner.AllowUnreachable) ? GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, enumerable2, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, (Thing x) => scanner.GetPriority(pawn, x)) : GenClosest.ClosestThing_Global(pawn.Position, enumerable2, 99999f, validator, (Thing x) => scanner.GetPriority(pawn, x)));
                            }
                            else if (scanner.AllowUnreachable)
                            {
                                IEnumerable<Thing> enumerable3 = enumerable;
                                if (enumerable3 == null)
                                {
                                    enumerable3 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                }
                                thing = GenClosest.ClosestThing_Global(pawn.Position, enumerable3, 99999f, validator);
                            }
                            else
                            {
                                thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
                            }
                            if (thing != null)
                            {
                                bestTargetOfLastPriority = thing;
                                scannerWhoProvidedTarget = scanner;
                            }
                        }
                        if (scanner.def.scanCells)
                        {
                            pawnPosition = pawn.Position;
                            closestDistSquared = 99999f;
                            bestPriority = float.MinValue;
                            prioritized = scanner.Prioritized;
                            allowUnreachable = scanner.AllowUnreachable;
                            maxPathDanger = scanner.MaxPathDanger(pawn);
                            IEnumerable<IntVec3> enumerable4 = WorkScannerHelper.TryGetWorkCells(scanner, pawn); //scanner.PotentialWorkCellsGlobal(pawn);
                            IList<IntVec3> list2;

                            cellcnt = 0;
                            cellcntnew = 0;
                            if ((list2 = enumerable4 as IList<IntVec3>) != null)
                            {
                                //That seems to never happen
                                for (int k = 0; k < list2.Count; k++)
                                {
                                    ProcessCell(list2[k]);
                                }
                            }
                            else
                            {
                                List<IntVec3> intVecs = enumerable4?.ToList();
                                foreach (IntVec3 item in enumerable4)
                                {
                                    ProcessCell(item);

                                }

                            }

                        }
                    }
                    void ProcessCell(IntVec3 c)
                    {
                        bool flag = false;
                        float num2 = (c - pawnPosition).LengthHorizontalSquared;
                        float num3 = 0f;
                        if (prioritized)
                        {
                            if (!c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
                            {
                                if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                                {
                                    return;
                                }
                                num3 = scanner.GetPriority(pawn, c);
                                if (num3 > bestPriority || (num3 == bestPriority && num2 < closestDistSquared))
                                {
                                    flag = true;
                                }
                            }
                        }
                        else if (num2 < closestDistSquared && !c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
                        {
                            if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                            {
                                return;
                            }
                            flag = true;
                        }
                        if (flag)
                        {
                            bestTargetOfLastPriority = new TargetInfo(c, pawn.Map);
                            scannerWhoProvidedTarget = scanner;
                            closestDistSquared = num2;
                            bestPriority = num3;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat(pawn, " threw exception in WorkGiver ", workGiver.def.defName, ": ", ex.ToString()));
                }
                finally
                {

                }


                if (bestTargetOfLastPriority.IsValid)
                {
                    Job job3 = ((!bestTargetOfLastPriority.HasThing) ? scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell) : scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing));
                    if (job3 != null)
                    {
                        job3.workGiverDef = scannerWhoProvidedTarget.def;
                        WorkScannerHelper.NotifyUsedCell(scannerWhoProvidedTarget, bestTargetOfLastPriority.Cell,pawn);
                        return new ThinkResult(job3, null, list[j].def.tagToGive);
                    }
                    Log.ErrorOnce(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
                }
                num = workGiver.def.priorityInType;
            }
            //Thats it
            //Log.Message("NoJob");
            WorkScannerHelper.NotifyNoJob(list,pawn);
            return ThinkResult.NoJob;
        }
#pragma warning restore
    }


    static class WorkScannerHelper
    {

        private static IntVec3 sortCell = new IntVec3(0,0,0);

        private static Dictionary<Tuple<WorkGiver_Scanner,Area>, List<IntVec3>> lastValues = new Dictionary<Tuple<WorkGiver_Scanner, Area>, List<IntVec3>>();

        private static Dictionary<Tuple<WorkGiver_Scanner,Area>, int> foundJobCnt = new Dictionary<Tuple<WorkGiver_Scanner, Area>, int>();

        private static int refreshDelay = 2500; //1h 

        private static int recalcDistance = 50; 


        private static int lastRefreshTick = 0;


        //TODO
        public static void NotifyNoJob(List<WorkGiver> scanners,Pawn pawn)
        {
            if (scanners == null) return;

            try
            {
                Area area = pawn.playerSettings?.AreaRestriction;
                foreach (WorkGiver giver in scanners)
                {
                    WorkGiver_Scanner scan =  giver as WorkGiver_Scanner;
                    if (scan == null) continue;
                    foundJobCnt[new Tuple<WorkGiver_Scanner, Area>(scan, area)] = 0;
                }
                
            }
            catch
            {
                Log.Message("NotifyNoJob Catch");
            }
            
        }

        public static IEnumerable<IntVec3> TryGetWorkCells(WorkGiver_Scanner scanner,Pawn pawn)
        {
            Tuple<WorkGiver_Scanner, Area> key = new Tuple<WorkGiver_Scanner, Area>(scanner, pawn.playerSettings?.AreaRestriction);
            int ctick = Find.TickManager.TicksGame;
            if ((ctick - lastRefreshTick ) >= refreshDelay )
            {
                lastRefreshTick = Find.TickManager.TicksGame;
                lastValues.Clear();
            }

            List<IntVec3> data;
            if (lastValues.TryGetValue(key, out data) == false)
            {
                  data = scanner.PotentialWorkCellsGlobal(pawn).ToList();
                  lastValues.Add(key, data);
                if (!foundJobCnt.ContainsKey(key))
                {
                    foundJobCnt.Add(key, 0);
                }
                else
                {
                    foundJobCnt[key] = 0;
                }
                

            }

            if (foundJobCnt[key] > 5)
            {
                data = data.GetRange(0, Math.Min(data.Count, 441));
            }

            return data;
        }

        public static void NotifyUsedCell(WorkGiver_Scanner scanner, IntVec3 cell, Pawn pawn)
        {
            Tuple<WorkGiver_Scanner, Area> key = new Tuple<WorkGiver_Scanner, Area>(scanner, pawn.playerSettings?.AreaRestriction);
            if (!foundJobCnt.ContainsKey(key))
            {
                foundJobCnt.Add(key, 1);
            }
            else
            {
                foundJobCnt[key]++;
            }

            
            if (cell == null || !cell.IsValid) return;

            if ((cell - sortCell).LengthHorizontalSquared > recalcDistance)
            {
                sortCell = cell;

                SortList(scanner, cell,pawn);
            }
           

        }

        // maybe do this async later
        private static void SortList(WorkGiver_Scanner scanner, IntVec3 cell, Pawn pawn)
        {
            try
            {
                //AsParallel() dos nothing
                lastValues[new Tuple<WorkGiver_Scanner, Area>(scanner, pawn.playerSettings?.AreaRestriction)].OrderBy(c => (c - cell).LengthHorizontalSquared);
               // Log.Message(lastValues.Keys.Count() + " ");
            }
            catch
            {

            }
            


        }


    }




}
