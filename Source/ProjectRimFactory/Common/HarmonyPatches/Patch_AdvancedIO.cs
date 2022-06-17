using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;
using Verse.AI;
using ProjectRimFactory.Storage;

namespace ProjectRimFactory.Common.HarmonyPatches
{


    //Should be a Transpiler... but for Testing lets use it as a prefix
    [HarmonyPatch(typeof(GenClosest), "ClosestThingReachable")]
    class Patch_AdvancedIO_ClosestThingReachable
	{
        public static bool Prefix(IntVec3 root, Map map, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, ref Thing __result
            , float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0
            , int searchRegionsMax = -1, bool forceAllowGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
        {
            var dict = map.GetComponent<PRFMapComponent>().GetadvancedIOLocations.Where(l => l.Value.CanGetNewItem);
			if (dict == null ||  dict.Count() == 0) return true;

			//Copy and paste for now (while not Transpiler)
			bool flag = searchRegionsMax < 0 || forceAllowGlobalSearch;
			if (!flag && customGlobalSearchSet != null)
			{
				Log.ErrorOnce("searchRegionsMax >= 0 && customGlobalSearchSet != null && !forceAllowGlobalSearch. customGlobalSearchSet will never be used.", 634984);
			}
			if (!flag && !thingReq.IsUndefined && !thingReq.CanBeFoundInRegion)
			{
				Log.ErrorOnce(string.Concat("ClosestThingReachable with thing request group ", thingReq.group, " and global search not allowed. This will never find anything because this group is never stored in regions. Either allow global search or don't call this method at all."), 518498981);
				__result = null;
				return false;
			}
			if (EarlyOutSearch(root, map, thingReq, customGlobalSearchSet, validator))
			{
				__result = null;
				return false;
			}
			Thing thing = null;

			//Add Check with Advanced_IO Here
			//-----------------------------------------------------------

			var possiblePorts = dict.Where(e => e.Value.boundStorageUnit?.StoredItems.Any(i => thingReq.Accepts(i)) ?? false).Select(e => e.Value);




			//-----------------------------------------------------------
			//normal Code Continue


			bool flag2 = false;
			if (!thingReq.IsUndefined && thingReq.CanBeFoundInRegion)
			{
				int num = ((searchRegionsMax > 0) ? searchRegionsMax : 30);
				thing = GenClosest.RegionwiseBFSWorker(root, map, thingReq, peMode, traverseParams, validator, null, searchRegionsMin, num, maxDistance, out var regionsSeen, traversableRegionTypes, ignoreEntirelyForbiddenRegions);
				flag2 = thing == null && regionsSeen < num;
			}
			if (thing == null && flag && !flag2)
			{
				if (traversableRegionTypes != RegionType.Set_Passable)
				{
					Log.ErrorOnce("ClosestThingReachable had to do a global search, but traversableRegionTypes is not set to passable only. It's not supported, because Reachability is based on passable regions only.", 14384767);
				}
				Predicate<Thing> validator2 = delegate (Thing t)
				{
					if (!map.reachability.CanReach(root, t, peMode, traverseParams))
					{
						return false;
					}
					return (validator == null || (validator(t) || t is  ProjectRimFactory.Storage.Building_AdvancedStorageUnitIOPort)) ? true : false;
				};
				IEnumerable<Thing> searchSet = customGlobalSearchSet ?? map.listerThings.ThingsMatching(thingReq);
				//
				foreach (var t in possiblePorts)
                {
					searchSet = searchSet.Append(t);
				}


				//
				thing = GenClosest.ClosestThing_Global(root, searchSet, maxDistance, validator2);
			}

			var myPort = thing as ProjectRimFactory.Storage.Building_AdvancedStorageUnitIOPort;
			if (myPort != null)
            {
				//Get the actual thing to the port
				var item = myPort.boundStorageUnit.StoredItems.FirstOrDefault(i => thingReq.Accepts(i));
				item.Position = myPort.Position;
				thing = item;
			}

				__result = thing;
			//Copy pasta end

			return false;
        }


		//Temp
		private static bool EarlyOutSearch(IntVec3 start, Map map, ThingRequest thingReq, IEnumerable<Thing> customGlobalSearchSet, Predicate<Thing> validator)
		{
			if (thingReq.group == ThingRequestGroup.Everything)
			{
				Log.Error("Cannot do ClosestThingReachable searching everything without restriction.");
				return true;
			}
			if (!start.InBounds(map))
			{
				Log.Error(string.Concat("Did FindClosestThing with start out of bounds (", start, "), thingReq=", thingReq));
				return true;
			}
			if (thingReq.group == ThingRequestGroup.Nothing)
			{
				return true;
			}
			if ((thingReq.IsUndefined || map.listerThings.ThingsMatching(thingReq).Count == 0) && customGlobalSearchSet.EnumerableNullOrEmpty())
			{
				return true;
			}
			return false;
		}

	}

	//Should be a Transpiler... but for Testing lets use it as a prefix
	[HarmonyPatch(typeof(FoodUtility), "SpawnedFoodSearchInnerScan")]
	public class Patch_AdvancedIO_SpawnedFoodSearchInnerScan
	{
		public static bool Prefix(Pawn eater, IntVec3 root, List<Thing> searchSet, PathEndMode peMode, TraverseParms traverseParams,ref Thing __result ,float maxDistance = 9999f, Predicate<Thing> validator = null)
        {
			Pawn pawn = traverseParams.pawn ?? eater;

			var prfmapcomp = pawn.Map.GetComponent<PRFMapComponent>();
			var dict = prfmapcomp.GetadvancedIOLocations.Where(l => l.Value.CanGetNewItem);
			if (dict == null || dict.Count() == 0) return true;

			var mindist = float.MaxValue;
			ProjectRimFactory.Storage.Building_AdvancedStorageUnitIOPort closestPort = null;
			foreach (var port in dict)
            {
				var mydust = (root - port.Key).LengthManhattan;
				if (mydust < mindist)
                {
					mindist = mydust;
					closestPort = port.Value;

				}
			}

			ThingRequest thingRequest = ((!((eater.RaceProps.foodType & (FoodTypeFlags.Plant | FoodTypeFlags.Tree)) != 0 && false)) ? ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree) : ThingRequest.ForGroup(ThingRequestGroup.FoodSource));


			if (searchSet == null)
			{
				__result = null;
				return false;
			}

			bool flagresult = false;
			Thing result = null;
			float FoodScore = 0f;
			float BestFoodScore = float.MinValue;
			for (int i = 0; i < searchSet.Count; i++)
			{
				bool flag = false;
				Thing thing = searchSet[i];
				float Distance = (root - thing.Position).LengthManhattan;
				if (mindist < Distance && closestPort.boundStorageUnit.StoredItems.Contains(thing))
                {
					Distance = mindist;
					flag = true;
				}

				if (!(Distance > maxDistance))
				{
					FoodScore = FoodUtility.FoodOptimality(eater, thing, FoodUtility.GetFinalIngestibleDef(thing), Distance);
					if (!(FoodScore < BestFoodScore) && pawn.Map.reachability.CanReach(root, thing, peMode, traverseParams) && thing.Spawned && (validator == null || validator(thing)))
					{
						flagresult = flag;
						result = thing;
						BestFoodScore = FoodScore;
					}
				}
			}
			if (flagresult)
            {
				result.Position = closestPort.Position;

			}
			__result = result;
			return false;
        }

	}


    [HarmonyPatch(typeof(Verse.AI.Pawn_JobTracker), "StartJob")]

    public class Patch_AdvancedIO_StartJob
	{
        public static bool Prefix(Job newJob, ref Pawn ___pawn , JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true
			, ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false)
		{

			var prfmapcomp = ___pawn.Map.GetComponent<PRFMapComponent>();
			var dict = prfmapcomp.GetadvancedIOLocations;
			if (dict == null || dict.Count() == 0) return true;

			var targetPos = newJob.targetA.Thing?.Position ?? newJob.targetA.Cell;

			//Maybe later make it work with multible port connecting to multibel dsu s
			var closesetPort = dict.OrderBy(i => i.Key.DistanceTo(targetPos)).FirstOrDefault();
			var closesetPortDistance = closesetPort.Key.DistanceTo(targetPos);
			if (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0) return true;

			foreach (var target in newJob.targetQueueB)
            {
				if (closesetPortDistance < target.Cell.DistanceTo(targetPos))
                {
					if (prfmapcomp.ShouldHideItemsAtPos(target.Cell))
                    {
						closesetPort.Value.AddItemToQueue(target.Thing);
					}
                }
            }

			return true;
		}
	}

}
