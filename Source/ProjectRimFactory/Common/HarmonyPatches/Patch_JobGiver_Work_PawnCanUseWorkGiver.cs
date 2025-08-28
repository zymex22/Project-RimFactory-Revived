using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using ProjectRimFactory.Drones;
using RimWorld;

namespace ProjectRimFactory.Common.HarmonyPatches;


[HarmonyPatch (typeof(JobGiver_Work), "PawnCanUseWorkGiver")]
public class Patch_JobGiver_Work_PawnCanUseWorkGiver
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool foundMissingRequiredCapacity = false;
        bool foundColonySubhuman = false;
        int foundColonySubhumanCnt = 0;
        int counter = 0;
        object retPointer = null;
        object initialSkipPointer = null;
        foreach (var instruction in instructions)
        {
            if (initialSkipPointer is null && instruction.opcode == OpCodes.Brtrue_S)
            {
                initialSkipPointer = instruction.operand;
            }

            if (!foundColonySubhuman && instruction.opcode == OpCodes.Callvirt &&
                instruction.operand.ToString() == "Boolean get_IsColonySubhuman()")
            {
                foundColonySubhuman = true;
            }

            if (foundColonySubhuman) foundColonySubhumanCnt++;

            if (foundColonySubhumanCnt == 3)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Isinst, typeof(Pawn_Drone));
                yield return new CodeInstruction(OpCodes.Ldnull);
                yield return new CodeInstruction(OpCodes.Cgt_Un);
                
                yield return new CodeInstruction(OpCodes.Brtrue_S, initialSkipPointer);
            }
            
            if (!foundMissingRequiredCapacity && instruction.opcode == OpCodes.Callvirt &&
                instruction.operand.ToString() == "Verse.PawnCapacityDef MissingRequiredCapacity(Verse.Pawn)")
            {
                foundMissingRequiredCapacity = true;
            }
            if (foundMissingRequiredCapacity) counter++;

            if (counter == 8) // brfalse.s IL_0091
            {
                retPointer = instruction.operand;
            }

            if (counter == 9) // ldarg.2
            {
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Isinst, typeof(Pawn_Drone));
                yield return new CodeInstruction(OpCodes.Ldnull);
                yield return new CodeInstruction(OpCodes.Cgt_Un);
                
                yield return new CodeInstruction(OpCodes.Brfalse_S, retPointer);
                
                
            }
            
            yield return instruction;
        }
    }
}