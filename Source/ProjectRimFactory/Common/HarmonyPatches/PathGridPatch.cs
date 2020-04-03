using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Reflection;
using ProjectRimFactory.Storage;
using Verse.AI;
using System.Reflection.Emit;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(PathGrid), "CalculatedCostAt")]
    public static class PathGridPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> previousInstructions)
        {
            FieldInfo match = typeof(Thing).GetField("def");
            FieldInfo match2 = typeof(BuildableDef).GetField("pathCost");
            object previousOperand = null;
            foreach (CodeInstruction instruction in previousInstructions)
            {
                if (previousOperand == match && instruction.operand == match2)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, typeof(PathGrid).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance));
                    yield return new CodeInstruction(OpCodes.Call, typeof(PathGridPatch).GetMethod("ApparentPathCost"));
                }
                else
                {
                    yield return instruction;
                }
                previousOperand = instruction.operand;
            }
        }
        public static int ApparentPathCost(ThingDef def, IntVec3 c, Map map)
        {
            Building b = c.GetFirstBuilding(map);
            if (b is Building_MassStorageUnit)
            {
                return (b.def == def) ? b.def.pathCost : 0;
            }
            return def.pathCost;
        }
    }
}
