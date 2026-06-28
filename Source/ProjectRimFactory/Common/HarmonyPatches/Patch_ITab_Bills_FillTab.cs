using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ProjectRimFactory.SAL3.Things.Assemblers;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches;

[HarmonyPatch]
public class Patch_ITab_Bills_FillTab
{

    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return  typeof(ITab_Bills).GetMethods(AccessTools.all).First(m => m.Name == "<FillTab>g__OptionsMaker|10_0");
        yield return AccessTools.Method(typeof(ITab_Bills), "FillTab");
    }
    
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool foundGetTabel = false;
        bool foundDef =false;
        CodeInstruction defInstruction = null;
        
        foreach (var instruction in instructions)
        {
           // Log.Message($"{instruction.opcode} {instruction.operand}");
            if (instruction.opcode == OpCodes.Call &&  instruction.operand.ToString() == "RimWorld.Building_WorkTable get_SelTable()")
            {
                foundGetTabel = true;
                yield return instruction;
                continue;
            }

            if (foundGetTabel && instruction.opcode == OpCodes.Ldfld &&
                instruction.operand.ToString() == "Verse.ThingDef def")
            {
                // We Have SelTable().def
                foundDef =  true;
                defInstruction  = instruction;
                continue; // We will add it back if this is a different case
            }

            // Only if SelTable() is directly followed by .def
            if (foundGetTabel && !foundDef)
            {
                foundGetTabel = false;
            }
            
            if (foundDef && foundGetTabel && instruction.opcode == OpCodes.Callvirt 
                && instruction.operand.ToString() == "System.Collections.Generic.List`1[Verse.RecipeDef] get_AllRecipes()"){
                instruction.operand = AccessTools.Method(typeof(Patch_ITab_Bills_FillTab), "GetAllRecipes", [typeof(Thing)]);
                foundDef = false;
                foundGetTabel  = false;
                Log.Message($"Updated: {instruction.opcode} {instruction.operand}");
                yield return instruction;
                continue;
            } 
            if (foundDef && foundGetTabel)
            {
                foundDef = false;
                foundGetTabel  = false;
                yield return defInstruction;
            }
            
            yield return instruction;
        }
    }
    
    
    public static List<RecipeDef> GetAllRecipes(Thing thing)
    {
        if (thing is Building_DynamicBillGiver prfBuilding)
        {
            return prfBuilding.Recipes;
        }
        return thing.def.AllRecipes;
    }
    


}