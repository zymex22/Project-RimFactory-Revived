using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using HarmonyLib;
using Verse;
using ProjectRimFactory.Storage;
using System.Reflection.Emit;
using System.Reflection;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //This patch allows Dsu's to Act as a trade beachon
    [HarmonyPatch(typeof(TradeUtility), "AllLaunchableThingsForTrade")]
    class Patch_DSU_OrbitalTrade
    {
        static void Postfix(Map map, ref IEnumerable<Thing> __result)
        {
            HashSet<Thing> yieldedThings = new HashSet<Thing>();
            yieldedThings.AddRange<Thing>(__result);
            foreach (Building_MassStorageUnitPowered dsu in Building_MassStorageUnitPowered.AllPowered(map))
            {
                yieldedThings.AddRange<Thing>(dsu.StoredItems);
            }
            __result = yieldedThings;

        }
    }

    //This Patch Allows the player to start an orvital Trade without a Trade beacon but with a DSU.
    //Without this patch a player would need a dummy beacon to use Patch_DSU_OrbitalTrade
    [HarmonyPatch]
     public static class Patch_PassingShip_DSUisTradebeacon
    {
        public static Type predicateClass;
        static MethodBase TargetMethod()//The target method is found using the custom logic defined here
        {
            predicateClass = typeof(PassingShip).GetNestedTypes(HarmonyLib.AccessTools.all)
               .FirstOrDefault(t => t.FullName.Contains("c__DisplayClass24_0"));
            if (predicateClass == null)
            {
                Log.Error("PRF Harmony Error - predicateClass == null for Patch_PassingShip_DSUisTradebeacon.TargetMethod()");
                return null;
            }

            var m = predicateClass.GetMethods(AccessTools.all)
                                 .FirstOrDefault(t => t.Name.Contains("b__1"));
            if (m == null)
            {
                Log.Error("PRF Harmony Error - m == null for Patch_PassingShip_DSUisTradebeacon.TargetMethod()");
            }
            return m;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            Type hiddenClass = typeof(PassingShip).GetNestedTypes(HarmonyLib.AccessTools.all)
               .FirstOrDefault(t => t.FullName.Contains("c__DisplayClass24_0"));

            bool foundLocaterString = false;
            foreach (var instruction in instructions)
            {

                //Patch shall change:
                //if (!Building_OrbitalTradeBeacon.AllPowered(<>4__this.Map).Any())
                //
                //To:
                //if (!Building_OrbitalTradeBeacon.AllPowered(<>4__this.Map).Any() && !Building_MassStorageUnitPowered.AllPowered(<>4__this.Map).Any() )


                //Find the refrence Point
                if (instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo) == AccessTools.Method(typeof(Building_OrbitalTradeBeacon), nameof(Building_OrbitalTradeBeacon.AllPowered)))
                {
                    foundLocaterString = true;
                }

                //Find the Check
                if (instruction.opcode == OpCodes.Brtrue_S && foundLocaterString)
                {
                    //Keep the Inctruction
                    yield return instruction;
                    //this.Map
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(hiddenClass, "<>4__this"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PassingShip), "Map"));
                    //Call --> Building_MassStorageUnitPowered.AnyPowerd with the above as an argument
                    yield return new CodeInstruction(OpCodes.Call, HarmonyLib.AccessTools
                        .Method(typeof(Building_MassStorageUnitPowered) ,nameof(Building_MassStorageUnitPowered.AnyPowerd), new[] { typeof(Map)} ));
                    yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand);
                    continue;

                }
                //Keep the other instructions
                yield return instruction;

            }


        }
    }




}
