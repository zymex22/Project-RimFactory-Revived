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
using System.Reflection.Emit;
using System.Reflection;

namespace ProjectRimFactory.Common.HarmonyPatches
{

    [HarmonyPatch(typeof(GenClosest), "ClosestThingReachable")]
    class Patch_AdvancedIO_ClosestThingReachable
	{


		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			Type mapSubClass = typeof(GenClosest).GetNestedTypes(HarmonyLib.AccessTools.all)
			   .FirstOrDefault(t => t.FullName.Contains("c__DisplayClass2_0"));

			bool found_ClosestThing_Global_Call = false;

			foreach (var instruction in instructions)
			{
				//Search for:
				// IEnumerable<Thing> searchSet = customGlobalSearchSet ?? map.listerThings.ThingsMatching(thingReq);
				//After that add
				//AppendsearchSet(ref searchSet, map, thingReq);

				//Maybe improve the search
				if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString() == "System.Collections.Generic.IEnumerable`1[Verse.Thing] (7)")
                {
					//Keep that Instruction to save it as a local variable
					yield return instruction;
					//Load Serach Set
					yield return new CodeInstruction(OpCodes.Ldloca_S, instruction.operand);

					//get The map
					//display class thingy
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(mapSubClass, "map"));

					//Get the thingreq
					yield return new CodeInstruction(OpCodes.Ldarg_2);

					MethodInfo methodInfo_AppendsearchSet = AccessTools.Method(
						typeof(Patch_AdvancedIO_ClosestThingReachable), nameof(Patch_AdvancedIO_ClosestThingReachable.AppendsearchSet), new[] { typeof(IEnumerable<Thing>).MakeByRefType(), typeof(Map), typeof(ThingRequest) });
					
					//Make the call
					yield return new CodeInstruction(OpCodes.Call, methodInfo_AppendsearchSet);
					continue;
				}

				



				//Search for:
				//thing = ClosestThing_Global(root, searchSet, maxDistance, validator2);
				//After that, just before the reurn add:
				//thing = GetThing(thing,thingReq);
				if (found_ClosestThing_Global_Call)
                {
					found_ClosestThing_Global_Call = false;
					//We must now be on:
					//IL_015e: stloc.2
					if (instruction.opcode != OpCodes.Stloc_2)
                    {
						Log.Error($"Project Rimfactory unexpected IL in Patch_AdvancedIO_ClosestThingReachable Expected OpCodes.Stloc_2 but got {instruction}");
                    }
					yield return instruction;

					//Get the Thing
					yield return new CodeInstruction(OpCodes.Ldloc_2);

					//Get the thingreq
					yield return new CodeInstruction(OpCodes.Ldarg_2);

					//Make the Call
					MethodInfo methodInfo_GetThing = AccessTools.Method(
						typeof(Patch_AdvancedIO_ClosestThingReachable), nameof(Patch_AdvancedIO_ClosestThingReachable.GetThing), new[] { typeof(Thing), typeof(ThingRequest) });

					//Make the call
					yield return new CodeInstruction(OpCodes.Call, methodInfo_GetThing);

					yield return new CodeInstruction(OpCodes.Stloc_2);

					continue;
				}
				if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("ClosestThing_Global"))
                {
					found_ClosestThing_Global_Call = true;
					//found the Line:
					//thing = ClosestThing_Global(root, searchSet, maxDistance, validator2);
				}



				yield return instruction;
			}

		}

		public static Dictionary<Building_AdvancedStorageUnitIOPort, Thing> ItemsReachableByIOMap = new Dictionary<Building_AdvancedStorageUnitIOPort, Thing>();

		public static bool AdvancedIOValidator(Predicate<Thing> validator, Thing t)
        {
			if (validator == null) return true;
			if (t is Building_AdvancedStorageUnitIOPort myPort)
			{
				return (validator(ItemsReachableByIOMap[myPort]));
			}

			return (validator(t));
		}

		public static void AppendsearchSet(ref IEnumerable<Thing> searchSet,Map map, ThingRequest thingReq)
        {
			if (map is null) return;

			var dict = map.GetComponent<PRFMapComponent>().GetadvancedIOLocations.Where(l => l.Value.CanGetNewItem);
			if (dict is null || dict.Count() == 0) return;
			ItemsReachableByIOMap.Clear();
			//Remove IO Ports from the search set. Only specific ones can be used.
			var serachlist = searchSet.ToList();
			serachlist.RemoveAll(i => i is Building_AdvancedStorageUnitIOPort);

			
			
			foreach (var e in dict)
            {
				var bound = e.Value.boundStorageUnit;
				if (bound is null) continue;
				Thing thing = bound.StoredItems.Where(i => thingReq.Accepts(i)).FirstOrDefault();
				if (thing is not null)
                {
					ItemsReachableByIOMap.Add(e.Value, thing);
					serachlist.Add(e.Value);
				}

			}
			searchSet = serachlist;
		}

		public static Thing GetThing(Thing thing, ThingRequest thingReq)
        {
			var myPort = thing as ProjectRimFactory.Storage.Building_AdvancedStorageUnitIOPort;
			if (myPort is null)
            {
				return thing;
            }
			else
			{
				var item = ItemsReachableByIOMap[myPort];
				//Get the actual thing to the port
				//var item = myPort.boundStorageUnit.StoredItems.FirstOrDefault(i => thingReq.Accepts(i));
				if (item == null)
				{
					Log.Error($"PRF Advanced IO assumd {myPort} had an item matching a thingReq but now can't clocate it");
					return null;
				}
				item.Position = myPort.Position;
				return item;
			}
		}

	}

	/// <summary>
	/// Patch for the "Predicate<Thing> validator2" contained in GenClosest.ClosestThingReachable
	/// As it is stord in a DisplayCalass a seperate Transpiler Patch is required
	/// This Patch belogs to Patch_AdvancedIO_ClosestThingReachable
	/// </summary>
	[HarmonyPatch]
	public class Patch_GenClosest_ClosestThingReachable_Validator
	{
		static MethodBase TargetMethod()//The target method is found using the custom logic defined here
		{

			Type predicateClass = typeof(GenClosest).GetNestedTypes(HarmonyLib.AccessTools.all)
			   .FirstOrDefault(t => t.FullName.Contains("c__DisplayClass2_0"));

			if (predicateClass == null)
			{
				Log.Error("PRF Harmony Error - predicateClass == null");
				return null;
			}

			var m = predicateClass.GetMethods(AccessTools.all)
								 .FirstOrDefault(t => t.Name.Contains("b__0"));
			if (m == null)
			{
				Log.Error("PRF Harmony Error - m == null");
			}
			return m;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			bool foundFirtRet = false;
			int counter = 0;
			foreach (var instruction in instructions)
			{
				counter++;
				//Validator
				//Replace
				//return (validator == null || validator(t)) ? true : false;
				//With
				//return AdvancedIOValidator(validator,t);
				//Found the first "return False"
				if (instruction.opcode == OpCodes.Ret)
                {
					foundFirtRet = true;
					counter = 0;

				}

				if(foundFirtRet && counter == 3)
                {
					//After Loading the Validator

					//Load t
					yield return new CodeInstruction(OpCodes.Ldarg_1);

					//Make the Call
					MethodInfo methodInfo_AdvancedIOValidator = AccessTools.Method(
						typeof(Patch_AdvancedIO_ClosestThingReachable), nameof(Patch_AdvancedIO_ClosestThingReachable.AdvancedIOValidator), new[] { typeof(Predicate<Thing>), typeof(Thing) });

					//Make the call
					yield return new CodeInstruction(OpCodes.Call, methodInfo_AdvancedIOValidator);

					yield return new CodeInstruction(OpCodes.Ret);

					//Throw the rest out
					break;
                }

				//Keep the other instructions
				yield return instruction;

			}

		}

	}
}
