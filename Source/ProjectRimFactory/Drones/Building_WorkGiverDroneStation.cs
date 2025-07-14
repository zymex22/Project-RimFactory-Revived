using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones
{
    public abstract class Building_WorkGiverDroneStation : Building_DroneStation
    {
        public virtual IEnumerable<WorkTypeDef> WorkTypes => extension.workTypes;

        public virtual Dictionary<WorkTypeDef, bool> WorkSettingsDict => WorkSettings;


        Pawn_WorkSettings workSettings = null;


        //Try give Job to Spawned drone
        // TODO Check that
        public override Job TryGiveJob(Pawn pawn)
        {
            if (CachedSleepTimeList.Contains(GenLocalDate.HourOfDay(this).ToString())) return null;
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
            foreach (var workTypeDef in WorkSettings.Keys)
            {
                pawn.workSettings.SetPriority(workTypeDef, WorkSettings[workTypeDef] ? 3 : 0);
            }

            //Each call to TryIssueJobPackageDrone takes an Average of 1ms
            return TryIssueJobPackageDrone(pawn, default, true).Job ?? TryIssueJobPackageDrone(pawn, default,false).Job;
        }
        
        // Method from RimWorld.JobGiver_Work.TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        // I modified the line if (!workGiver.ShouldSkip(pawn))
#pragma warning disable
        public ThinkResult TryIssueJobPackageDrone(Pawn pawn, JobIssueParams jobParams, bool emergency)
		{
			if (pawn.RaceProps.Humanlike && pawn.health.hediffSet.InLabor())
			{
				return ThinkResult.NoJob;
			}
			/*
			if (emergency && pawn.mindState.priorityWork.IsPrioritized)
			{
				...
			}
			*/
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
			Danger maxPathDanger;
			for (int j = 0; j < list.Count; j++)
			{
				WorkGiver workGiver = list[j];
				if (workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid)
				{
					break;
				}
				/*!PawnCanUseWorkGiver(pawn, workGiver) replace with the following subset*/
				if (workGiver.def.workType != null && pawn.WorkTypeIsDisabled(workGiver.def.workType))
				{
					continue;
				}
				try
				{
					Job job2 = workGiver.NonScanJob(pawn);
					if (job2 != null)
					{
						if (pawn.jobs.debugLog)
						{
							pawn.jobs.DebugLogEvent($"JobGiver_Work produced non-scan Job {job2.ToStringSafe()} from {workGiver}");
						}
						return new ThinkResult(job2, null, list[j].def.tagToGive);
					}
					scanner = workGiver as WorkGiver_Scanner;
					if (scanner != null)
					{
						if (scanner.def.scanThings)
						{
							IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);
							bool flag = pawn.carryTracker?.CarriedThing != null && scanner.PotentialWorkThingRequest.Accepts(pawn.carryTracker.CarriedThing) && Validator(pawn.carryTracker.CarriedThing);
							Thing thing;
							if (scanner.Prioritized)
							{
								IEnumerable<Thing> searchSet = enumerable ?? pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
								thing = ((!scanner.AllowUnreachable) ? GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, searchSet, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, Validator, (Thing x) => scanner.GetPriority(pawn, x)) : GenClosest.ClosestThing_Global(pawn.Position, searchSet, 99999f, Validator, (Thing x) => scanner.GetPriority(pawn, x)));
								if (flag)
								{
									if (thing != null)
									{
										float num2 = scanner.GetPriority(pawn, pawn.carryTracker.CarriedThing);
										float num3 = scanner.GetPriority(pawn, thing);
										if (num2 >= num3)
										{
											thing = pawn.carryTracker.CarriedThing;
										}
									}
									else
									{
										thing = pawn.carryTracker.CarriedThing;
									}
								}
							}
							else if (flag)
							{
								thing = pawn.carryTracker.CarriedThing;
							}
							else if (scanner.AllowUnreachable)
							{
								IEnumerable<Thing> searchSet2 = enumerable ?? pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
								thing = GenClosest.ClosestThing_Global(pawn.Position, searchSet2, 99999f, Validator);
							}
							else
							{
								thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, Validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
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
							IEnumerable<IntVec3> enumerable2 = scanner.PotentialWorkCellsGlobal(pawn);
							IList<IntVec3> list2 = enumerable2 as IList<IntVec3>;
							if (list2 != null)
							{
								for (int k = 0; k < list2.Count; k++)
								{
									ProcessCell(list2[k]);
								}
							}
							else
							{
								foreach (IntVec3 item in enumerable2)
								{
									ProcessCell(item);
								}
							}
						}
					}
					void ProcessCell(IntVec3 c)
					{
						bool flag2 = false;
						float num4 = (c - pawnPosition).LengthHorizontalSquared;
						float num5 = 0f;
						if (prioritized)
						{
							if (!c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
							{
								if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
								{
									return;
								}
								num5 = scanner.GetPriority(pawn, c);
								if (num5 > bestPriority || (num5 == bestPriority && num4 < closestDistSquared))
								{
									flag2 = true;
								}
							}
						}
						else if (num4 < closestDistSquared && !c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
						{
							if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
							{
								return;
							}
							flag2 = true;
						}
						if (flag2)
						{
							bestTargetOfLastPriority = new TargetInfo(c, pawn.Map);
							scannerWhoProvidedTarget = scanner;
							closestDistSquared = num4;
							bestPriority = num5;
						}
					}
					bool Validator(Thing t)
					{
						if (!t.IsForbidden(pawn))
						{
							return scanner.HasJobOnThing(pawn, t);
						}
						return false;
					}
				}
				catch (Exception ex)
				{
					Log.Error(pawn?.ToString() + " threw exception in WorkGiver " + workGiver.def.defName + ": " + ex.ToString());
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
						if (pawn.jobs.debugLog)
						{
							pawn.jobs.DebugLogEvent($"JobGiver_Work produced scan Job {job3.ToStringSafe()} from {scannerWhoProvidedTarget}");
						}
						return new ThinkResult(job3, null, list[j].def.tagToGive);
					}
					string[] obj = new string[6]
					{
						scannerWhoProvidedTarget?.ToString(),
						" provided target ",
						null,
						null,
						null,
						null
					};
					TargetInfo targetInfo = bestTargetOfLastPriority;
					obj[2] = targetInfo.ToString();
					obj[3] = " but yielded no actual job for pawn ";
					obj[4] = pawn?.ToString();
					obj[5] = ". The CanGiveJob and JobOnX methods may not be synchronized.";
					Log.ErrorOnce(string.Concat(obj), 6112651);
				}
				num = workGiver.def.priorityInType;
			}
			return ThinkResult.NoJob;
		}
#pragma warning restore
    }
}
