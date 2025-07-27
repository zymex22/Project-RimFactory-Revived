using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(FoodUtility), "SpawnedFoodSearchInnerScan")]
    class Patch_FoodUtility_SpawnedFoodSearchInnerScan
    {
        private static object thingarg;
        private static bool afterflaotMin;
        
        // ReSharper disable once UnusedMember.Local
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            thingarg = null;
            afterflaotMin = false;

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
                        nameof(FindClosestPort), new[] { typeof(Pawn), typeof(IntVec3) }));


                    continue;
                }

                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString() == "Verse.Thing (7)" && thingarg == null) thingarg = instruction.operand;


                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString() == "System.Single (8)")
                {
                    yield return instruction;

                    yield return new CodeInstruction(OpCodes.Ldloca_S, instruction.operand);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, thingarg);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScan),
                        nameof(IsIOPortBetter), new[] { typeof(float).MakeByRefType(), typeof(Thing), typeof(Pawn), typeof(IntVec3) }));
                    continue;
                }


                if (instruction.opcode == OpCodes.Ret && thingarg != null)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScan),
                        nameof(MoveItemIfNeeded), new[] { typeof(Thing) }));
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                }



                yield return instruction;

            }

        }

        private static float mindist = float.MaxValue;
        private static Building_AdvancedStorageUnitIOPort closestPort;

        public static void FindClosestPort(Pawn pawn, IntVec3 _)
        {
            //Init vals
            mindist = float.MaxValue;
            closestPort = null;

            if (pawn.Faction is not { IsPlayer: true }) return;

            //TODO: Not Optimal for the search. might need update
            var closest = AdvancedIO_PatchHelper.GetClosestPort(pawn.Map, pawn.Position);
            mindist = closest.Key;
            closestPort = closest.Value;
        }

        private static bool ioPortSelected;
        private static Thing ioPortSelectedFor;


        /// <summary>
        /// Checks if the IO Port is a better or the only Option
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="thing"></param>
        /// <param name="pawn"></param>
        /// <param name="start"></param>
        public static void IsIOPortBetter(ref float distance, Thing thing, Pawn pawn, IntVec3 start)
        {
            ioPortSelected = false;

            //If the Port is Closer than it is a better choice
            //#691 If the Port is the only Option it must be used
            if ( mindist < distance || (ConditionalPatchHelper.PatchReachabilityCanReach.Status 
                                        && pawn.Map.reachability.CanReach(start,thing,Verse.AI.PathEndMode.Touch, TraverseParms.For(pawn)) 
                                        && Patch_Reachability_CanReach.CanReachThing(thing) ))
            {
                //Check if the Port can be used
                //TODO: Check TODO in Line 88
                if (closestPort != null && AdvancedIO_PatchHelper.CanMoveItem(closestPort, thing))
                {
                    distance = mindist;
                    ioPortSelected = true;
                    ioPortSelectedFor = thing;
                }
            }
        }

        public static void MoveItemIfNeeded(Thing thing)
        {
            //When using replimat it might replace thing  
            if (thing != ioPortSelectedFor || !ioPortSelected || thing == null)  return;
            
            ioPortSelected = false;
            closestPort.PlaceThingNow(thing);
        }
    }
}
