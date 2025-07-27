using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //This patch allows Dsu's to Act as a trade beachon
    [HarmonyPatch(typeof(TradeUtility), "AllLaunchableThingsForTrade")]
    class Patch_TradeUtility_AllLaunchableThingsForTrade
    {
        // ReSharper disable once UnusedMember.Local
        static void Postfix(Map map, ref IEnumerable<Thing> __result)
        {
            HashSet<Thing> yieldedThings = [];
            yieldedThings.AddRange(__result);
            foreach (var dsu in TradePatchHelper.AllPowered(map))
            {
                yieldedThings.AddRange(dsu.StoredItems);
            }
            __result = yieldedThings;

        }
    }

    //This Patch Allows the player to start an orvital Trade without a Trade beacon but with a DSU.
    //Without this patch a player would need a dummy beacon to use Patch_DSU_OrbitalTrade
    [HarmonyPatch]
    // ReSharper disable once InconsistentNaming
    public static class Patch_PassingShip_c__DisplayClass24_0
    {
        private static Type predicateClass;
        // ReSharper disable once UnusedMember.Local
        static MethodBase TargetMethod()//The target method is found using the custom logic defined here
        {
            predicateClass = typeof(PassingShip).GetNestedTypes(AccessTools.all)
               .FirstOrDefault(t => t!.FullName!.Contains("c__DisplayClass23_0"));
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

        // ReSharper disable once UnusedMember.Local
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            Type hiddenClass = typeof(PassingShip).GetNestedTypes(AccessTools.all)
               .FirstOrDefault(t => t!.FullName!.Contains("c__DisplayClass23_0"));

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
                    foundLocaterString = false;
                    //Keep the Inctruction
                    yield return instruction;
                    //this.Map
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(hiddenClass, "<>4__this"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PassingShip), "Map"));
                    //Call --> Building_MassStorageUnitPowered.AnyPowerd with the above as an argument
                    yield return new CodeInstruction(OpCodes.Call, AccessTools
                        .Method(typeof(TradePatchHelper), nameof(TradePatchHelper.AnyPowered), [typeof(Map)]));
                    yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand);
                    continue;

                }
                //Keep the other instructions
                yield return instruction;

            }


        }
    }

    public static class TradePatchHelper
    {
        public static bool AnyPowered(Map map)
        {
            return AllPowered(map, true).Any();
        }

        public static IEnumerable<ILinkableStorageParent> AllPowered(Map map, bool any = false)
        {
            foreach (ILinkableStorageParent item in map.listerBuildings.AllBuildingsColonistOfClass<Building_MassStorageUnitPowered>())
            {
                if (item.Powered)
                {
                    yield return item;
                    if (any) break;
                }
            }
            var cs = PatchStorageUtil.GetPRFMapComponent(map).ColdStorageBuildings
                .Select(ILinkableStorageParent (b) => b);
            foreach (var item in cs)
            {
                yield return item;
            }
        }
    }




}
