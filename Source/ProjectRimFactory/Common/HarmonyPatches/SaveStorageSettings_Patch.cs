using HarmonyLib;
using ProjectRimFactory.SAL3.Things.Assemblers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    class SaveStorageSettings_Patch
    {
        public static Type Patch_Building_Gizmos;

        static IEnumerable<CodeInstruction> Transpiler_Billstack(IEnumerable<CodeInstruction> instructions)
        {
            bool Found_WindowStackCall = false;
            int ldfld_cnt = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo) == AccessTools.Method(typeof(Verse.Find), "get_WindowStack"))
                {
                    Found_WindowStackCall = true;
                    ldfld_cnt = 0;
                }

                if (Found_WindowStackCall && instruction.opcode == OpCodes.Castclass)
                {
                    continue; //skipp Instructions
                }
                if (Found_WindowStackCall && instruction.opcode == OpCodes.Ldfld)
                {
                    ldfld_cnt++;
                }

                if (ldfld_cnt == 4 && instruction.opcode == OpCodes.Ldfld)
                {
                    Found_WindowStackCall = false;
                    ldfld_cnt = 0;
                    var newist = new CodeInstruction(OpCodes.Call, HarmonyLib.AccessTools
                        .Method(typeof(SaveStorageSettings_Patch), nameof(SaveStorageSettings_Patch.GetBillstack), new[] { typeof(Building) }));
                    yield return newist;
                    continue;

                }
                yield return instruction;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler_IsWorkTable(IEnumerable<CodeInstruction> instructions)
        {
            bool forundCall = false;

            foreach (var instruction in instructions)
            {

                if (forundCall == false && instruction.opcode == OpCodes.Ldfld && (instruction.operand as FieldInfo) == AccessTools.Field(typeof(Verse.Thing), "def"))
                {
                    continue;
                }

                if (instruction.opcode == OpCodes.Callvirt &&
                    (instruction.operand as MethodInfo) == AccessTools.Method(typeof(Verse.ThingDef), "get_IsWorkTable"))
                {
                    forundCall = true;
                    yield return new CodeInstruction(OpCodes.Call, HarmonyLib.AccessTools
                        .Method(typeof(SaveStorageSettings_Patch), nameof(SaveStorageSettings_Patch.IsValidBuilding), new[] { typeof(Building) }));
                    continue;
                }

                yield return instruction;
            }
        }

        public static bool IsValidBuilding(Building building)
        {
            var isworktable = (building as Building_WorkTable) != null;
            var isRimfactory = ((building as ProjectRimFactory.AutoMachineTool.ITabBillTable) != null) || building is Building_DynamicBillGiver;

            return isworktable || isRimfactory;
        }

        public static BillStack GetBillstack(Building building)
        {
            Building_WorkTable building_WorkTable = building as Building_WorkTable;
            if (building_WorkTable != null) return building_WorkTable.billStack;
            ProjectRimFactory.AutoMachineTool.ITabBillTable tabBillTable = building as ProjectRimFactory.AutoMachineTool.ITabBillTable;
            if (tabBillTable != null) return tabBillTable.billStack;
            Building_DynamicBillGiver building_DynamicBillGiver = building as Building_DynamicBillGiver;
            if (building_DynamicBillGiver != null) return building_DynamicBillGiver.BillStack;

            //This should never happen
            Log.Error($"PRF Error GetBillstack returns null - {building}");
            return null;

        }


    }
}
