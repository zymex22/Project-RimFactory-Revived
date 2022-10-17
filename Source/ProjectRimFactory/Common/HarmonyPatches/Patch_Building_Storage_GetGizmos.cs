using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /// <summary>
    /// This Transpiler prevents Vanilla 1.4 from generating Gizmos for each item contained in a Building_Storage
    /// 
    /// This prevents UI Clutter 
    /// and also improves performance compared to vanilla 1.4 without this Patch
    /// </summary>
    [HarmonyPatch]
    class Patch_Building_Storage_GetGizmos
    {
        //The target method is found using the custom logic defined here
        static MethodBase TargetMethod()
        {
            var predicateClass = typeof(Building_Storage).GetNestedTypes(HarmonyLib.AccessTools.all)
               .FirstOrDefault(t => t.FullName.Contains("d__43"));
            if (predicateClass == null)
            {
                Log.Error("PRF Harmony Error - predicateClass == null for Patch_Building_Storage_GetGizmos.TargetMethod()");
                return null;
            }

            var m = predicateClass.GetMethods(AccessTools.all)
                                 .FirstOrDefault(t => t.Name.Contains("MoveNext"));
            if (m == null)
            {
                Log.Error("PRF Harmony Error - m == null for Patch_Building_Storage_GetGizmos.TargetMethod()");
            }
            return m;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            bool Found_get_NumSelected = false;
            object endJumpMarker = null;
            bool addedJump = false;
            foreach (var instruction in instructions)
            {
                //Used for refrence of the pos withing the IL
                if (instruction.opcode == OpCodes.Callvirt
                    && instruction.operand.ToString().Contains("get_NumSelected()"))
                {
                    Found_get_NumSelected = true;
                }

                //Get the Jumpmarker for the End
                if (Found_get_NumSelected && endJumpMarker == null && instruction.opcode == OpCodes.Bne_Un)
                {
                    endJumpMarker = instruction.operand;
                }

                if (!addedJump && Found_get_NumSelected && instruction.opcode == OpCodes.Ldarg_0)
                {
                    //Check if this is a PRF Storage Building
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_Building_Storage_GetGizmos),
                        nameof(Patch_Building_Storage_GetGizmos.IsPRF_StorageBuilding)));
                    //Skip to the End if yes
                    yield return new CodeInstruction(OpCodes.Brtrue_S, endJumpMarker);
                    addedJump = true;
                }

                //Keep the rest
                yield return instruction;
            }

        }

        /// <summary>
        /// Checks if the building is derived from Building_MassStorageUnit
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsPRF_StorageBuilding(Building_Storage building)
        {
            return building is Building_MassStorageUnit;
        }

    }
}
