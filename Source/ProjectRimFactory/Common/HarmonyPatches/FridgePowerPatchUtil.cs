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
        public static readonly Dictionary<Building_MassStorageUnitPowered, float> FridgePowerDrawPerUnit = new();

        public static void UpdatePowerDraw(Building_MassStorageUnitPowered dsu, CompPowerTrader powerTrader)
        {
            if (!FridgePowerDrawPerUnit.TryGetValue(dsu, out var powerDraw))
            {
                powerDraw = -1 * (float)ReflectionUtility.CompPropertiesPowerBasePowerConsumption.GetValue(powerTrader.Props);
            }

            powerTrader.powerOutputInt = powerDraw - dsu.ExtraPowerDraw;
        }

    }
}
