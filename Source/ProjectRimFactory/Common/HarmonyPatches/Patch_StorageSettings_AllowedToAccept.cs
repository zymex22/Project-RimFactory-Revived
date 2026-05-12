using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

namespace ProjectRimFactory.Common.HarmonyPatches;

/// <summary>
/// Required for the IO Port Limit Function
/// See: #699 #678 for reference
///
/// </summary>
[HarmonyPatch(typeof(StorageSettings), "AllowedToAccept", typeof(Thing))]
class Patch_StorageSettings_AllowedToAccept
{
        

    private static bool patchDone = false;
    private static bool foundStart = false;
    private static object jumpMarker;
        
    /// <summary>
    /// Equivalent to the Following Prefix
    /// static bool Prefix(IStoreSettingsParent ___owner, Thing t, out bool __result)
    ///{
    ///    __result = false;
    ///    if (___owner is not IForbidPawnInputItem storage) return true;
    ///    if (PatchStorageUtil.SkippAcceptsPatch || !storage.ForbidPawnInput || storage.Position == t.Position)
    ///    {
    ///        return true;
    ///    }
    ///    return false;
    ///}
    /// </summary>
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
            
        foreach (var instruction in instructions)
        {
                
            if (!foundStart && !patchDone)
            {
                if (instruction.opcode == OpCodes.Brfalse_S)
                {
                    jumpMarker = instruction.operand;
                }
                if (instruction.opcode == OpCodes.Ldc_I4_1)
                {
                    foundStart = true;
                }
            }
                
            if (foundStart && !patchDone)
            {
                // Insert Patch Here
                // We are Just before the Return True
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageSettings), "owner"));
                yield return new CodeInstruction(OpCodes.Isinst, typeof(IForbidPawnInputItem));
                yield return new CodeInstruction(OpCodes.Brfalse_S, jumpMarker); // if not IForbidPawnInputItem return
                    
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PatchStorageUtil), "SkippAcceptsPatch"));
                yield return new CodeInstruction(OpCodes.Brtrue_S, jumpMarker); // Skip if SkippAcceptsPatch
                   
                    
                // I Hope that's right
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageSettings), "owner"));
                yield return new CodeInstruction(OpCodes.Isinst, typeof(IForbidPawnInputItem));
                yield return new CodeInstruction(OpCodes.Callvirt, 
                    AccessTools.PropertyGetter(typeof(IForbidPawnInputItem), "ForbidPawnInput"));
                yield return new CodeInstruction(OpCodes.Brfalse_S, jumpMarker); // if not ForbidPawnInput return
                    
                // now the Position check
                // I Hope that's right
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageSettings), "owner"));
                yield return new CodeInstruction(OpCodes.Isinst, typeof(IForbidPawnInputItem));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(IHaulDestination), "get_Position"));
                    
                    
                //yield return new CodeInstruction(OpCodes.Ldarg_1); // This should be the thing
                //yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Position"));
                    
                yield return new CodeInstruction(OpCodes.Ldarg_1); // This should be the thing
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Position"));
                    
                    
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(IntVec3), "op_Equality", new Type[] { typeof(IntVec3), typeof(IntVec3) }));
                yield return new CodeInstruction(OpCodes.Brtrue_S, jumpMarker); // Skip if pos is the same

                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return new CodeInstruction(OpCodes.Ret);
                    
                patchDone = true;
            }
                
            yield return instruction;
        }
    }

}