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
    // Removed at LWM's suggestion - see
    //   https://github.com/zymex22/Project-RimFactory-Revived/commit/b1fafb73aaa45c7dbaa151fc0fc09f2b2df144b8
    // LWM's thoughts: this would give a tiny bit of value for a small amount of performance cost
    //    so maybe not worth it
    // If not disabled, it throws errors, so it would need to be fixed to be used
#if false
    [HarmonyPatch(typeof(PathGrid), "CalculatedCostAt")]
    public static class PathGridPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> previousInstructions)
        {
            FieldInfo match = typeof(Thing).GetField("def");
            FieldInfo match2 = typeof(BuildableDef).GetField("pathCost");
            FieldInfo previousOperand = null;
            foreach (CodeInstruction instruction in previousInstructions)
            {
                if (previousOperand == match && (instruction.operand as FieldInfo) == match2)
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
                previousOperand = (instruction.operand as FieldInfo);
            }
        }
        public static int ApparentPathCost(ThingDef def, IntVec3 c, Map map)
        {
            Building b = c.GetFirst<Building_MassStorageUnit>(map);
            if (b is Building_MassStorageUnit)
            {
                return (b.def == def) ? b.def.pathCost : 0;
            }
            return def.pathCost;
        }
    }
#endif
}
