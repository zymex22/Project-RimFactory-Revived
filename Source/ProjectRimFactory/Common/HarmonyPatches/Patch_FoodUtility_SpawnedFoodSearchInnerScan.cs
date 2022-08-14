using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using ProjectRimFactory.Storage;
using System.Reflection.Emit;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(FoodUtility), "SpawnedFoodSearchInnerScan")]
	class Patch_FoodUtility_SpawnedFoodSearchInnerScan
	{
		static object Thingarg = null;
		static bool afterflaotMin = false;
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand.ToString() == "-3.402823E+38")
				{
					yield return instruction;
					afterflaotMin = true;
					continue;
				}
				if (afterflaotMin && instruction.opcode == OpCodes.Stloc_S)
				{
					afterflaotMin = false;
					yield return instruction;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
						typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScan),
						nameof(Patch_FoodUtility_SpawnedFoodSearchInnerScan.findClosestPort), new[] { typeof(Pawn), typeof(IntVec3) }));


					continue;
				}

				if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString() == "Verse.Thing (7)" && Thingarg == null) Thingarg = instruction.operand;


				//Issue here
				if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString() == "System.Single (8)")
				{
					yield return instruction;

					yield return new CodeInstruction(OpCodes.Ldloca_S, instruction.operand);
					yield return new CodeInstruction(OpCodes.Ldloc_S, Thingarg);

					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
						typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScan),
						nameof(Patch_FoodUtility_SpawnedFoodSearchInnerScan.isCanIOPortGetItem), new[] { typeof(float).MakeByRefType(), typeof(Thing) }));
					continue;
				}


				if (instruction.opcode == OpCodes.Ret && Thingarg != null)
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
						typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScan),
						nameof(Patch_FoodUtility_SpawnedFoodSearchInnerScan.moveItemIfNeeded), new[] { typeof(Thing) }));
					yield return new CodeInstruction(OpCodes.Ldloc_3);
				}



				yield return instruction;

			}

		}

		private static float mindist = float.MaxValue;
		private static Building_AdvancedStorageUnitIOPort closestPort = null;

		public static void findClosestPort(Pawn pawn, IntVec3 root)
		{
			//Init vals
			mindist = float.MaxValue;
			closestPort = null;

			if (pawn.Faction == null || !pawn.Faction.IsPlayer) return;

			//Not Optimal for the search. might need update
			var closest = AdvancedIO_PatchHelper.GetClosestPort(pawn.Map, pawn.Position);
			mindist = closest.Key;
			closestPort = closest.Value;
		}

		private static bool ioPortSelected = false;

		public static void isCanIOPortGetItem(ref float Distance, Thing thing)
		{
			ioPortSelected = false;
			if (mindist < Distance && closestPort != null && ((AdvancedIO_PatchHelper.CanMoveItem(closestPort,thing))))
			{
				Distance = mindist;
				ioPortSelected = true;
			}
		}

		public static void moveItemIfNeeded(Thing thing)
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
	}
}
