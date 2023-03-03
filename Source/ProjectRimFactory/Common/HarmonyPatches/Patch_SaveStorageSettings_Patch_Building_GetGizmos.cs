using HarmonyLib;
using ProjectRimFactory.SAL3.Things.Assemblers;
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
        static IEnumerable<CodeInstruction> Transpiler_Billstack(IEnumerable<CodeInstruction> instructions)
        {
            bool Found_WindowStackCall = false;
            int ldfld_cnt = 0;
            foreach (var instruction in instructions)
            {
                //find the Start Point
                if (instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo) == AccessTools.Method(typeof(Verse.Find), "get_WindowStack"))
                {
                    Found_WindowStackCall = true;
                    ldfld_cnt = 0;
                }

                //We don't want the cast to Building_WorkTable
                if (Found_WindowStackCall && instruction.opcode == OpCodes.Castclass)
                {
                    continue; //skipp Instructions
                }

                //Count the ldfld instructions to find where we want to place our method
                if (Found_WindowStackCall && instruction.opcode == OpCodes.Ldfld)
                {
                    ldfld_cnt++;
                }

                //Add our Method in the correct location
                if (ldfld_cnt == 4 && instruction.opcode == OpCodes.Ldfld)
                {
                    Found_WindowStackCall = false;
                    ldfld_cnt = 0;
                    var newist = new CodeInstruction(OpCodes.Call, HarmonyLib.AccessTools
                        .Method(typeof(Patch_SaveStorageSettings_Patch_Building_GetGizmos), nameof(Patch_SaveStorageSettings_Patch_Building_GetGizmos.GetBillstack), new[] { typeof(Building) }));
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
        static IEnumerable<CodeInstruction> Transpiler_IsWorkTable(IEnumerable<CodeInstruction> instructions)
        {
            bool forundCall = false;

            foreach (var instruction in instructions)
            {
                //For our Method we need the Building as the first and only Parameter
                //Therfore we want to remove the ldfld that would get the the thingdef instead
                if (forundCall == false && instruction.opcode == OpCodes.Ldfld && (instruction.operand as FieldInfo) == AccessTools.Field(typeof(Verse.Thing), "def"))
                {
                    continue;
                }

                //Check for the get_IsWorkTable call and replace it with our method
                if (instruction.opcode == OpCodes.Callvirt &&
                    (instruction.operand as MethodInfo) == AccessTools.Method(typeof(Verse.ThingDef), "get_IsWorkTable"))
                {
                    forundCall = true;
                    yield return new CodeInstruction(OpCodes.Call, HarmonyLib.AccessTools
                        .Method(typeof(Patch_SaveStorageSettings_Patch_Building_GetGizmos), nameof(Patch_SaveStorageSettings_Patch_Building_GetGizmos.IsValidBuilding), new[] { typeof(Building) }));
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
            return (building as IBillTab) != null;
        }

        /// <summary>
        /// Returns the BillStack for a multitude of different Building Types 
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static BillStack GetBillstack(Building building)
        {
            Building_WorkTable building_WorkTable = building as Building_WorkTable;
            if (building_WorkTable != null) return building_WorkTable.billStack;

            IBillTab building_IBillTab = building as IBillTab;
            if (building_IBillTab != null) return building_IBillTab.BillStack;

            //This should never happen
            Log.Error($"PRF Error GetBillstack returns null - {building}");
            return null;

        }


    }
}
