using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection.Emit; // for OpCodes in Harmony Transpiler

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /* Patch Verse.GenRecipe.cs's MakeRecipeProducts to remove chance of food
     *   poisoning for T3 Assemblers:
     * We change
     *   CompFoodPoisonable compFoodPoisonable = thing.TryGetComp<CompFoodPoisonable>();
     *   if (compFoodPoisonable != null) {......}
     * to
     *   CompFoodPoisonable compFoodPoisonable = thing.TryGetComp<CompFoodPoisonable>();
     *   if (compFoodPoisonable != null && !UsingSpaceCooker(this.billGiver)) {......}
     * #DeepMagic
     */
    /* Technical notes:
     * The toils are returned inside a hidden "inner" iterator class,
     * and are returned inside the MoveNext() method of that class.
     * So to patch the method, we first have to find it, then we
     * use Transpiler to modify the code.
     */
    [HarmonyPatch]
    public static class Patch_GenRecipe_foodPoisoning {
        public static MethodBase TargetMethod() {
            // Decompiler showed the hidden inner class is "<MakeRecipeProducts>d__0"
            Type hiddenClass = HarmonyLib.AccessTools.Inner(typeof(Verse.GenRecipe), "<MakeRecipeProducts>d__0");
            if (hiddenClass==null) {
                Log.Error("Couldn't find d__0 - check decompiler to find proper inner class");
                return null;
            }
            // and we want the iterator's MoveNext:
            MethodBase iteratorMethod=HarmonyLib.AccessTools.Method(hiddenClass, "MoveNext");
            if (iteratorMethod==null) Log.Error("Couldn't find MoveNext");
            return iteratorMethod;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var code = new List<CodeInstruction>(instructions);
            bool codeHasStoredCompPoisonable=false;
            int i=0; // 2 for loops, one i
            for (; i<code.Count; i++) {
                yield return code[i];
                // Stloc_S 7 is where the CompFoodPoisonable is stored
                if (code[i].opcode==OpCodes.Stloc_S && ((LocalBuilder)code[i].operand).LocalIndex == 7) {
                    //Log.Message("Transpiler found CompPoisonable");
                    codeHasStoredCompPoisonable=true;
                }
                // and here is the jump away if it's null:
                if (code[i].opcode==OpCodes.Brfalse_S && codeHasStoredCompPoisonable) {
                    // Next up WOULD be doing the food poisoning check, but we add one more test:
                    // Get the billGiver:
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    //                                              vvv this is that hidden class again (from above)
                    yield return new CodeInstruction(OpCodes.Ldfld, HarmonyLib.AccessTools
                                  .Field(HarmonyLib.AccessTools.Inner(typeof(Verse.GenRecipe), "<MakeRecipeProducts>d__0"),
                                                                                                 "billGiver"));
                    // Okay, billGiver is on the stack; now call UsingSpaceCooker:
                    yield return new CodeInstruction(OpCodes.Call, HarmonyLib.AccessTools
                                                     .Method(typeof(Patch_GenRecipe_foodPoisoning), "UsingSpaceCooker"));
                    // that puts either true of false on the stack.  If it's true, jump past the food poisoning test:
                    yield return new CodeInstruction(OpCodes.Brtrue_S, (Label)code[i].operand);
                    //Log.Message("Successfully Transpiled Verse.GenRecipe");
                    i++; // advance past BrFalse.s to next instruction
                    break;  // only do this once - probably unnecessary, but...
                }
            }
            if (i==code.Count) Log.Warning("PRF: Removing Food Poisoning failed.");
            for (;i<code.Count; i++) {
                yield return code[i];
            }
        }
        public static bool UsingSpaceCooker(IBillGiver billGiver) {
            // NOTE: This could be made more efficient by having a Def already loaded
            //   either a static Def loaded from defdatabase directly, or loaded via
            //   [DefOf] notation.
            // However, string comparison in C# is already super fast, so it's
            //   probably all fine.
            if ((billGiver as Building)?.def.defName=="PRF_SpacerCooker") {
                Log.Message("Using Spacer Cooker - skipping poison test");
                return true;
            }
            Log.Message("Not using space cooker for this recipe");
            return false;
        }
    }
}
