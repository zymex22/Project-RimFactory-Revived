using ProjectRimFactory.SAL3;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using ProjectRimFactory.SAL3.Tools;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /// <summary>
    /// Utility class used by Patch_CompRefrigerator_CompTickRare & Patch_Patch_GameComponentTick_Postfix
    /// This is needed to correctly update the Power Usage of the Refrigerated DSU
    /// see: #613
    /// </summary>
    public static class FridgePowerPatchUtil
    {
        public static Dictionary<Building_MassStorageUnitPowered, float> FridgePowerDrawPerUnit = new Dictionary<Building_MassStorageUnitPowered, float>();

        public static void UpdatePowerDraw(Building_MassStorageUnitPowered dsu, CompPowerTrader powertrader)
        {
            float powerDraw = 0;
            if (!FridgePowerDrawPerUnit.TryGetValue(dsu, out powerDraw)) powerDraw = -1 * (float)ReflectionUtility.CompPropertiesPowerBasePowerConsumption.GetValue(powertrader.Props);
            powertrader.powerOutputInt = powerDraw - dsu.ExtraPowerDraw;
        }

    }
}
