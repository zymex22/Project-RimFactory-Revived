using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
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
        // ReSharper disable once UnusedMember.Local
        static MethodBase TargetMethod()
        {
            // Resolve the compiler-generated iterator by its source method instead of a
            // hard-coded "<GetGizmos>d__54" name, which changes whenever RimWorld is recompiled.
            var m = AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(Building_Storage), nameof(Building_Storage.GetGizmos)));
            if (m == null)
            {
                Log.Error("PRF Harmony Error - could not find Building_Storage.GetGizmos MoveNext for Patch_Building_Storage_GetGizmos.TargetMethod()");
            }
            return m;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // The iterator stores the Building_Storage instance in its "<>4__this" field.
            var thisField = AccessTools.Field(original.DeclaringType, "<>4__this");
            if (thisField == null)
            {
                Log.Error("PRF Harmony Error - <>4__this not found for Patch_Building_Storage_GetGizmos.Transpiler()");
                foreach (var instruction in instructions)
                {
                    yield return instruction;
                }
                yield break;
            }

            bool foundGetNumSelected = false;
            object endJumpMarker = null;
            bool addedJump = false;
            foreach (var instruction in instructions)
            {
                //Used for refrence of the pos withing the IL
                if ((instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call)
                    && instruction.operand is MethodInfo method && method.Name == "get_NumSelected")
                {
                    foundGetNumSelected = true;
                }

                //Get the Jumpmarker for the End
                if (foundGetNumSelected && endJumpMarker == null
                    && (instruction.opcode == OpCodes.Bne_Un || instruction.opcode == OpCodes.Bne_Un_S))
                {
                    endJumpMarker = instruction.operand;
                }

                if (!addedJump && endJumpMarker != null && instruction.opcode == OpCodes.Ldarg_0)
                {
                    //Check if this is a PRF Storage Building. The previous version pushed an extra
                    //ldarg.0 here, leaving the stack unbalanced; Harmony 3 rejects that IL.
                    //Labels on the original instruction move to the inserted check so jumps still land on it.
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                    yield return new CodeInstruction(OpCodes.Ldfld, thisField);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_Building_Storage_GetGizmos),
                        nameof(IsPRF_StorageBuilding)));
                    //Skip to the End if yes
                    yield return new CodeInstruction(OpCodes.Brtrue, endJumpMarker);
                    addedJump = true;
                }

                //Keep the rest
                yield return instruction;
            }

            if (!addedJump)
            {
                Log.Warning("Project Rimfactory - Patch_Building_Storage_GetGizmos found no injection point; storage item gizmos will not be hidden.");
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
