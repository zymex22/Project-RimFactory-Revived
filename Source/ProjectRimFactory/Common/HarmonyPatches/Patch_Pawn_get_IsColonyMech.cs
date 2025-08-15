using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using ProjectRimFactory.Drones;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches;

[HarmonyPatch (typeof(Pawn), "get_IsColonyMech")]
// ReSharper disable once InconsistentNaming
// Required for Drone Skills to function property
// get_IsColonyMech checks if a Pawn is Mechanoid and controlled by the Play but NOT if it is a Biotech Mech
// BUT it is used as IF that where the case. requiring this patch 
public class Patch_Pawn_get_IsColonyMech
{

    private static bool foundReturnFalsePointer;
    private static object returnFalsePointer;
    private static int referencePointCnt;
    private static bool foundReferencePoint;

    // ReSharper disable once UnusedMember.Local
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        
        foreach (var instruction in instructions)
        {
            // Get the Return False Pointer
            if (!foundReturnFalsePointer && instruction.opcode == OpCodes.Brfalse_S)
            {
                foundReturnFalsePointer = true;
                returnFalsePointer = instruction.operand;
            }

            // Find the get_MentalStateDef() Check (we want to be inserted right after that)
            if (!foundReferencePoint && foundReturnFalsePointer &&
                instruction.operand?.ToString() == "Verse.MentalStateDef get_MentalStateDef()")
            {
                foundReferencePoint = true;
            }

            if (foundReferencePoint) referencePointCnt++;
            
            if (foundReferencePoint && referencePointCnt == 3)
            {
                // now we want to insert a check if the pawn is Pawn_Drone
                // For maximum performance fully inlined IL
                // 5 additional instructions
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Isinst, typeof(Pawn_Drone));
                yield return new CodeInstruction(OpCodes.Ldnull);
                yield return new CodeInstruction(OpCodes.Cgt_Un);
                
                yield return new CodeInstruction(OpCodes.Brtrue_S, returnFalsePointer);
                
            }
            
            
            
            yield return instruction;
        }
    }
}