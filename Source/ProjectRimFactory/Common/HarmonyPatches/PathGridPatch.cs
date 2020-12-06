using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ProjectRimFactory.Storage;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //[HarmonyPatch(typeof(PathGrid), "CalculatedCostAt")]
    public static class PathGridPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> previousInstructions)
        {
            var match = typeof(Thing).GetField("def");
            var match2 = typeof(BuildableDef).GetField("pathCost");
            FieldInfo previousOperand = null;
            foreach (var instruction in previousInstructions)
            {
                if (previousOperand == match && instruction.operand as FieldInfo == match2)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld,
                        typeof(PathGrid).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance));
                    yield return new CodeInstruction(OpCodes.Call, typeof(PathGridPatch).GetMethod("ApparentPathCost"));
                }
                else
                {
                    yield return instruction;
                }

                previousOperand = instruction.operand as FieldInfo;
            }
        }

        public static int ApparentPathCost(ThingDef def, IntVec3 c, Map map)
        {
            Building b = c.GetFirst<Building_MassStorageUnit>(map);
            if (b is Building_MassStorageUnit) return b.def == def ? b.def.pathCost : 0;
            return def.pathCost;
        }
    }
}