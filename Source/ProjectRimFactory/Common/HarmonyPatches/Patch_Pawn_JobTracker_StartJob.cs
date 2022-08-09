using HarmonyLib;
using ProjectRimFactory.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common.HarmonyPatches
{

	/// <summary>
	/// Patch for the Building_AdvancedStorageUnitIOPort
	/// Pawns starting Jobs check the IO Port for Items
	/// This affects mostly Bills on Workbenches
	/// </summary>
	[HarmonyPatch(typeof(Verse.AI.Pawn_JobTracker), "StartJob")]
    class Patch_Pawn_JobTracker_StartJob
    {

		private static List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> GetPorts(Dictionary<IntVec3, Building_AdvancedStorageUnitIOPort> dict_IOports, IntVec3 targetPos)
        {
			List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> Ports = new List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>>();
			foreach (var pair in dict_IOports)
			{
				Ports.Add(new KeyValuePair<float, Building_AdvancedStorageUnitIOPort>(pair.Key.DistanceTo(targetPos), pair.Value));
			}
			Ports.OrderBy(i => i.Key);
			return Ports;

		}

		private static bool TryGetTargetPos(ref IntVec3 targetPos,bool isHaulJobType, Job newJob, IntVec3 pawnPosition)
        {
			if (isHaulJobType)
			{
				//Haul Type Job
				targetPos = newJob.targetB.Thing?.Position ?? newJob.targetB.Cell;
				if (targetPos == IntVec3.Invalid) targetPos = pawnPosition;
				if (newJob.targetA == null) return false;

			}
			else
			{
				//Bill Type Jon
				targetPos = newJob.targetA.Thing?.Position ?? newJob.targetA.Cell;
				if (newJob.targetB == IntVec3.Invalid && (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)) return false;
			}
			return true;
		}

		private static void GetTargetItems(ref List<LocalTargetInfo> TargetItems, bool isHaulJobType, Job newJob)
        {
			if (isHaulJobType)
			{
				TargetItems = new List<LocalTargetInfo>() { newJob.targetA };
			}
			else
			{
				if (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)
				{
					TargetItems = new List<LocalTargetInfo>() { newJob.targetB };
				}
				else
				{
					TargetItems = newJob.targetQueueB;
				}
			}
		}


		public static bool Prefix(Job newJob, ref Pawn ___pawn, JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true
		, ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false)
		{
			//No random moths eating my cloths
			if (___pawn?.Faction == null || !___pawn.Faction.IsPlayer) return true;
			var prfmapcomp = ___pawn.Map.GetComponent<PRFMapComponent>();
            Dictionary<IntVec3, Building_AdvancedStorageUnitIOPort> dict = prfmapcomp.GetadvancedIOLocations;
			if (dict == null || dict.Count() == 0) return true;
			//This is the Position where we need the Item to be at
			IntVec3 targetPos = IntVec3.Invalid;
			var usHaulJobType = newJob.targetA.Thing?.def?.category == ThingCategory.Item;
			if (!TryGetTargetPos(ref targetPos, usHaulJobType, newJob,___pawn.Position)) return true;


			List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> Ports = GetPorts(dict, targetPos);

			List<LocalTargetInfo> TargetItems = null;
			GetTargetItems(ref TargetItems, usHaulJobType, newJob);
			foreach (var target in TargetItems)
			{
				//Why did i put that check there? This seems odd
				if (prfmapcomp.ShouldHideItemsAtPos(target.Cell))
				{
					foreach (var port in Ports)
					{
						if (port.Key < target.Cell.DistanceTo(targetPos) && port.Value.boundStorageUnit?.Position == target.Cell)
						{
							port.Value.AddItemToQueue(target.Thing);
							break;
						}
					}
				}
			}
			return true;
		}
	}
}
