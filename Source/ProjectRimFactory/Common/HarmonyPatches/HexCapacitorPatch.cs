using HarmonyLib;
using ProjectRimFactory.Archo.Things;
using ProjectRimFactory.Industry;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{

    [HarmonyPatch(typeof(Alert_NeedBatteries), "NeedBatteries")]
    class Alert_NeedBatteries_NeedBatteries_Patch
    {
        static void Postfix(Alert_NeedBatteries __instance, Map map, ref bool __result)
        {
            if (__result == true)
            {
                if (map.listerBuildings.ColonistsHaveBuilding((Thing building) => building is Building_HexCapacitor))
                {
                    __result = false;
                }
                else if (map.listerBuildings.ColonistsHaveBuilding((Thing building) => building is Building_CustomBattery))
                {
                    __result = false;
                }
            }


        }

    }
}
