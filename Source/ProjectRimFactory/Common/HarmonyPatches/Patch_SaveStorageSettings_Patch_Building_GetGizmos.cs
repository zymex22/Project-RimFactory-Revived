using HarmonyLib;
using ProjectRimFactory.SAL3.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /// <summary>
    /// Mod Support Patch for SaveStorageSettings
    /// </summary>
    class Patch_SaveStorageSettings_Patch_Building_GetGizmos
    {
        public static Type Patch_Building_Gizmos;

        /// <summary>
        /// makes the Gizmos Compatible with our Machines
        /// 
        /// This Patch is Used for the Following Methods:
        /// Find.WindowStack.Add(new SaveCraftingDialog(type, ((Building_WorkTable)CS$<>8__locals1.__instance).billStack));
        /// Find.WindowStack.Add(new LoadCraftingDialog(type, ((Building_WorkTable)CS$<>8__locals1.__instance).billStack, LoadCraftingDialog.LoadType.Append));
        /// Find.WindowStack.Add(new LoadCraftingDialog(type, ((Building_WorkTable)CS$<>8__locals1.__instance).billStack, LoadCraftingDialog.LoadType.Replace));
        /// 
        /// for each of them we want change the billStack Parameter
        /// Instead of Casting the building as a Building_WorkTable and retrieving the billStack that way
        /// we use our method GetBillstack to get the Billstack for all different cases
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        // ReSharper disable once ArrangeTypeMemberModifiers
        // ReSharper disable once UnusedMember.Local
        static IEnumerable<CodeInstruction> Transpiler_Billstack(IEnumerable<CodeInstruction> instructions)
        {
            bool foundWindowStackCall = false;
            int ldfldCnt = 0;
            foreach (var instruction in instructions)
            {
                //find the Start Point
                if (instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo) == AccessTools.Method(typeof(Find), "get_WindowStack"))
                {
                    foundWindowStackCall = true;
                    ldfldCnt = 0;
                }

                //We don't want the cast to Building_WorkTable
                if (foundWindowStackCall && instruction.opcode == OpCodes.Castclass)
                {
                    continue; //skipp Instructions
                }

                //Count the ldfld instructions to find where we want to place our method
                if (foundWindowStackCall && instruction.opcode == OpCodes.Ldfld)
                {
                    ldfldCnt++;
                }

                //Add our Method in the correct location
                if (ldfldCnt == 4 && instruction.opcode == OpCodes.Ldfld)
                {
                    foundWindowStackCall = false;
                    ldfldCnt = 0;
                    var newist = new CodeInstruction(OpCodes.Call, AccessTools
                        .Method(typeof(Patch_SaveStorageSettings_Patch_Building_GetGizmos), nameof(GetBillStack), new[] { typeof(Building) }));
                    yield return newist;
                    continue;

                }
                yield return instruction;
            }
        }

        /// <summary>
        /// This Patch makes the Gizmos Visible for our Buildings
        /// 
        /// The Original Method uses the IsWorkTable getter to check if it should add the Worktable
        /// We want to instead use our Method IsValidBuilding, so that we can also support our buildings
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        // ReSharper disable once ArrangeTypeMemberModifiers
        // ReSharper disable once UnusedMember.Local
        static IEnumerable<CodeInstruction> Transpiler_IsWorkTable(IEnumerable<CodeInstruction> instructions)
        {
            bool forundCall = false;

            foreach (var instruction in instructions)
            {
                //For our Method we need the Building as the first and only Parameter
                //Therfore we want to remove the ldfld that would get the thingdef instead
                if (forundCall == false && instruction.opcode == OpCodes.Ldfld && (instruction.operand as FieldInfo) == AccessTools.Field(typeof(Thing), "def"))
                {
                    continue;
                }

                //Check for the get_IsWorkTable call and replace it with our method
                if (instruction.opcode == OpCodes.Callvirt &&
                    (instruction.operand as MethodInfo) == AccessTools.Method(typeof(ThingDef), "get_IsWorkTable"))
                {
                    forundCall = true;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools
                        .Method(typeof(Patch_SaveStorageSettings_Patch_Building_GetGizmos), nameof(IsValidBuilding),
                            [typeof(Building)]));
                    continue;
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Checks if a Building should get the Save Storage Gizmos 
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsValidBuilding(Building building)
        {
            return building.def.IsWorkTable || (building as IBillTab) != null;
        }

        /// <summary>
        /// Returns the BillStack for a multitude of different Building Types 
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static BillStack GetBillStack(Building building)
        {
            if (building is Building_WorkTable buildingWorkTable) return buildingWorkTable.billStack;
            if (building is IBillTab buildingIBillTab) return buildingIBillTab.BillStack;

            //This should never happen
            Log.Error($"PRF Error GetBillstack returns null - {building}");
            return null;

        }


    }
}
