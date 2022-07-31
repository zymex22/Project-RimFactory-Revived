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
		public static bool Prefix(Job newJob, ref Pawn ___pawn, JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true
		, ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false)
		{

			var prfmapcomp = ___pawn.Map.GetComponent<PRFMapComponent>();
			var dict = prfmapcomp.GetadvancedIOLocations;
			if (dict == null || dict.Count() == 0) return true;

			var targetPos = newJob.targetA.Thing?.Position ?? newJob.targetA.Cell;

			List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> Ports = new List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>>();
			foreach (var pair in dict)
			{
				Ports.Add(new KeyValuePair<float, Building_AdvancedStorageUnitIOPort>(pair.Key.DistanceTo(targetPos), pair.Value));
			}
			Ports.OrderBy(i => i.Key);

			if (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0) return true;

			foreach (var target in newJob.targetQueueB)
			{
				if (prfmapcomp.ShouldHideItemsAtPos(target.Cell))
				{
					foreach (var port in Ports)
					{
						if (port.Key < target.Cell.DistanceTo(targetPos) && port.Value.boundStorageUnit.Position == target.Cell)
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
