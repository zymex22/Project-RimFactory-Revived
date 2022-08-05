using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using ProjectRimFactory.Storage;

namespace ProjectRimFactory.Common.HarmonyPatches
{
	//This should probably be a Transpiler
	[HarmonyPatch(typeof(FoodUtility), "SpawnedFoodSearchInnerScan")]
    class Patch_FoodUtility_SpawnedFoodSearchInnerScan
    {

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				
				yield return instruction;

			}

		}

		private static float mindist = float.MaxValue;
		private static Building_AdvancedStorageUnitIOPort closestPort = null;

		private static void findClosestPort(Pawn pawn, IntVec3 root)
        {
			//Init vals
			mindist = float.MaxValue;
			closestPort = null;

			//Maybe Moths don't use that
			if (pawn.Faction == null || !pawn.Faction.IsPlayer) return;

			var dict = pawn?.Map?.GetComponent<PRFMapComponent>()?.GetadvancedIOLocations?.Where(l => l.Value.CanGetNewItem);
			if (dict == null || dict.Count() == 0) return;

			foreach (var port in dict)
			{
				var mydust = (root - port.Key).LengthManhattan;
				if (mydust < mindist)
				{
					mindist = mydust;
					closestPort = port.Value;

				}
			}
		}

		private static bool ioPortSelected = false;

		private static void isCanIOPortGetItem(ref float Distance,Thing thing)
        {
			ioPortSelected = false;
			if (mindist < Distance && closestPort != null && (closestPort.boundStorageUnit?.StoredItems?.Contains(thing) ?? false))
			{
				Distance = mindist;
				ioPortSelected = true;
			}
		}

		private static void moveItemIfNeeded(Thing thing)
        {
			if (ioPortSelected)
			{
				ioPortSelected = false;
                try
                {
					thing.Position = closestPort.Position;
				}
                catch (NullReferenceException)
                {
					Log.Message($"moveItemIfNeeded NullReferenceException - {thing} - {closestPort}");
                }
				
			}
		}

		public static bool Prefix(Pawn eater, IntVec3 root, List<Thing> searchSet, PathEndMode peMode, TraverseParms traverseParams, ref Thing __result, float maxDistance = 9999f, Predicate<Thing> validator = null)
		{
			if (searchSet == null)
			{
				__result = null;
				return false;
			}

			Pawn pawn = traverseParams.pawn ?? eater;

			Thing result = null;
			float FoodScore = 0f;
			float BestFoodScore = float.MinValue;


			findClosestPort(pawn, root);

			for (int i = 0; i < searchSet.Count; i++)
			{
				
				Thing thing = searchSet[i];
				float Distance = (root - thing.Position).LengthManhattan;

				isCanIOPortGetItem(ref Distance, thing);


				if (!(Distance > maxDistance))
				{
					FoodScore = FoodUtility.FoodOptimality(eater, thing, FoodUtility.GetFinalIngestibleDef(thing), Distance);
					if (!(FoodScore < BestFoodScore) && pawn.Map.reachability.CanReach(root, thing, peMode, traverseParams) && thing.Spawned && (validator == null || validator(thing)))
					{
						result = thing;
						BestFoodScore = FoodScore;
					}
				}
			}

			moveItemIfNeeded(result);

			__result = result;
			return false;
		}

	
	}
}
