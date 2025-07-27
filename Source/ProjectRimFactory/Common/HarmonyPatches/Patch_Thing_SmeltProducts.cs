using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches;

/// <summary>
/// Change Thing.SmeltProducts so that if
/// ProjectRimFactory.Common.HarmonyPatches.Patch_Thing_SmeltProducts.RecyclerProducingItems
/// is Set, the Check "costListAdj[j].thingDef.smeltable" will be Ignored
///
/// Since Vanilla uses the actual Value instead of the Getter we Can't simply patch the Getter,
/// Instead we need to Transpile Thing.SmeltProducts
/// </summary>
[HarmonyPatch]
public class Patch_Thing_SmeltProducts
{
    private static readonly Type HiddenClass = AccessTools.FirstInner(
        typeof(Thing),
        type => type.HasAttribute<CompilerGeneratedAttribute>() && type.Name.Contains(nameof(Thing.SmeltProducts)));
   
    public static MethodBase TargetMethod()
    {
        // Decompiler showed the hidden inner class is "<SmeltProducts>d__223"
        if (HiddenClass == null)
        {
            Log.Error("Couldn't find iterator class -- This should never be reached.");
            return null;
        }
        // and we want the iterator's MoveNext:
        MethodBase iteratorMethod = AccessTools.Method(HiddenClass, "MoveNext");
        if (iteratorMethod == null) Log.Error("Couldn't find MoveNext");
        return iteratorMethod;
    }

    private static bool smeltableFound;
    
    // ReSharper disable once UnusedMember.Local
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (!smeltableFound)
            {
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand.ToString().Contains("smeltable"))
                {
                    smeltableFound = true;
                    
                    // now replace it with a call to Patch_Thing_SmeltProducts.Smeltable
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_Thing_SmeltProducts),
                        nameof(Smeltable)));
                    continue;
                    
                }
                yield return instruction;
                continue;
            }
            
            // Don't touch the rest
            yield return instruction;
        }
        
        
    }
    
    public static bool RecyclerProducingItems = false;
    
    /// <summary>
    /// Skips the def.smeltable Check if RecyclerProducingItems
    /// </summary>
    /// <param name="def"></param>
    /// <returns></returns>
    public static bool Smeltable(ThingDef def)
    { 
        return RecyclerProducingItems || def.smeltable;
    }



}