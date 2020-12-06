using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ProjectRimFactory.SAL3.Things.Assemblers;
using RimWorld;
using Verse; // for OpCodes in Harmony Transpiler

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
    public static class Patch_GenRecipe_foodPoisoning
    {
        /// <summary>
        ///     Doing this should find the inner iterator class no matter how the compiler calls it.
        /// </summary>
        private static readonly Type hiddenClass = AccessTools.FirstInner(
            typeof(GenRecipe),
            type => type.HasAttribute<CompilerGeneratedAttribute>() &&
                    type.Name.Contains(nameof(GenRecipe.MakeRecipeProducts)));

        public static MethodBase TargetMethod()
        {
            // Decompiler showed the hidden inner class is "<MakeRecipeProducts>d__0"
            if (hiddenClass == null)
            {
                Log.Error("Couldn't find iterator class -- This should never be reached.");
                return null;
            }

            // and we want the iterator's MoveNext:
            MethodBase iteratorMethod = AccessTools.Method(hiddenClass, "MoveNext");
            if (iteratorMethod == null) Log.Error("Couldn't find MoveNext");
            return iteratorMethod;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codeHasStoredCompPoisonable = false;
            foreach (var instruction in instructions)
            {
                // =========== T3 cooker patch ==========
                // Roughly patches
                // if (compFoodPoisonable != null)
                // to
                // if (compFoodPoisonable != null && !UsingSpaceCooker(billGiver))
                if (instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder) instruction.operand).LocalIndex == 7)
                    // CompFoodPoisonable stored, the next condition will be the food poison check.
                    codeHasStoredCompPoisonable = true;
                if (instruction.opcode == OpCodes.Brfalse_S && codeHasStoredCompPoisonable)
                {
                    // For this branch, we emit the original instruction first. 
                    // If we don't have a CompFoodPoisonable, we still want to skip.

                    yield return instruction;
                    // Load billGiver onto the stack
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(hiddenClass, "billGiver"));
                    // Call our UsingSpaceCooker method
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_GenRecipe_foodPoisoning),
                        nameof(UsingSpaceCooker)));
                    // If that returned true, skip past the original condition
                    yield return new CodeInstruction(OpCodes.Brtrue_S, (Label) instruction.operand);
                    codeHasStoredCompPoisonable = false;

                    // Emitted original instruction first for this branch.
                    continue;
                }

                // =========== T1 cooker patch =========
                // Roughly patches
                // worker.GetRoom()
                // to Patch_GenRecipe_foodPoisoning.GetRoomOfPawnOrGiver(worker, RegionType.Set_Passable, billGiver)

                if (instruction.opcode == OpCodes.Call && instruction.operand as MethodInfo ==
                    AccessTools.Method(typeof(RegionAndRoomQuery), nameof(RegionAndRoomQuery.GetRoom)))
                {
                    // By the time we reach here, worker and the number 6 (signifying RegionType.Set_Passable)
                    // Load billGiver onto the stack.
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(hiddenClass, "billGiver"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_GenRecipe_foodPoisoning),
                        nameof(GetRoomOfPawnOrGiver)));

                    continue; // Don't emit the original instruction.
                }

                yield return instruction;
            }
        }

        public static bool UsingSpaceCooker(IBillGiver billGiver)
        {
            // NOTE: This could be made more efficient by having a Def already loaded
            //   either a static Def loaded from defdatabase directly, or loaded via
            //   [DefOf] notation.
            // However, string comparison in C# is already super fast, so it's
            //   probably all fine.
            if ((billGiver as Building)?.def.defName == "PRF_SelfCookerIII"
            ) // Log.Message("Using Spacer Cooker - skipping poison test");
                return true;
            // Log.Message("Not using space cooker for this recipe");
            return false;
        }

        /// <summary>
        ///     If the billGiver is our SimpleAssembler, get the room of the output tile.
        ///     Else, retain origin behavior.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="allowedRegionTypes"></param>
        /// <param name="billGiver"></param>
        /// <returns></returns>
        public static Room GetRoomOfPawnOrGiver(Pawn pawn, RegionType allowedRegionTypes, IBillGiver billGiver)
        {
            if (pawn.kindDef == PRFDefOf.PRFSlavePawn && billGiver is Building_SimpleAssembler assembler)
                return RegionAndRoomQuery.RoomAt(assembler.OutputComp.CurrentCell, billGiver.Map);
            return pawn.GetRoom(allowedRegionTypes);
        }
    }

    [HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn", typeof(Pawn), typeof(SkillDef))]
    internal class Patch_GenRecipe_GenerateQualityCreatedByPawn
    {
        private static bool Prefix(ref QualityCategory __result, Pawn pawn, SkillDef relevantSkill)
        {
            var isqd = PatchStorageUtil.Get<ISetQualityDirectly>(pawn.Map, pawn.Position);
            if (isqd != null)
            {
                __result = isqd.GetQuality(relevantSkill);
                return false;
            }

            return true;
        }
    }

    internal interface ISetQualityDirectly
    {
        QualityCategory GetQuality(SkillDef relevantSkill);
    }
}