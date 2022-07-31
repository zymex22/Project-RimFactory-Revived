using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

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

		public static bool Prefix(Pawn eater, IntVec3 root, List<Thing> searchSet, PathEndMode peMode, TraverseParms traverseParams, ref Thing __result, float maxDistance = 9999f, Predicate<Thing> validator = null)
		{
			if (searchSet == null)
			{
				__result = null;
				return false;
			}

			Pawn pawn = traverseParams.pawn ?? eater;

			bool flagresult = false;
			Thing result = null;
			float FoodScore = 0f;
			float BestFoodScore = float.MinValue;

			/******************/

			//Add Variables
			var mindist = float.MaxValue;
			ProjectRimFactory.Storage.Building_AdvancedStorageUnitIOPort closestPort = null;

			//Init Variables

			var dict = pawn.Map.GetComponent<PRFMapComponent>().GetadvancedIOLocations.Where(l => l.Value.CanGetNewItem);
			if (dict == null || dict.Count() == 0) return true;

			foreach (var port in dict)
			{
				var mydust = (root - port.Key).LengthManhattan;
				if (mydust < mindist)
				{
					mindist = mydust;
					closestPort = port.Value;

				}
			}

			/******************/
			for (int i = 0; i < searchSet.Count; i++)
			{
				
				Thing thing = searchSet[i];
				float Distance = (root - thing.Position).LengthManhattan;
				/******************/
				bool flag = false;
				if (mindist < Distance && closestPort.boundStorageUnit.StoredItems.Contains(thing))
				{
					Distance = mindist;
					flag = true;
				}
				/******************/

				if (!(Distance > maxDistance))
				{
					FoodScore = FoodUtility.FoodOptimality(eater, thing, FoodUtility.GetFinalIngestibleDef(thing), Distance);
					if (!(FoodScore < BestFoodScore) && pawn.Map.reachability.CanReach(root, thing, peMode, traverseParams) && thing.Spawned && (validator == null || validator(thing)))
					{
						/******************/
						flagresult = flag;
						/******************/
						result = thing;
						BestFoodScore = FoodScore;
					}
				}
			}
			/******************/
			if (flagresult)
			{
				result.Position = closestPort.Position;

			}
			/******************/
			__result = result;
			return false;
		}


	}
}
