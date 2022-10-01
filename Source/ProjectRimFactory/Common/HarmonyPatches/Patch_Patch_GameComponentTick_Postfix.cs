using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /// <summary>
    /// Patches the Postfix patch contained in SimpleFridge targeting GameComponentTick
    /// This is needed to draw the correct amount of power for the Refrigerated DSU
    /// </summary>
    public static class Patch_Patch_GameComponentTick_Postfix
    {
        public static void Postfix(int ___tick)
        {
            if (___tick != 0) return;
            Dictionary<ThingWithComps, CompPowerTrader> fc = (Dictionary<ThingWithComps, CompPowerTrader>)ProjectRimFactory_ModComponent.ModSupport_SimpleFridge_fridgeCache.GetValue(null);
            foreach (KeyValuePair<ThingWithComps, CompPowerTrader> item in fc)
            {
                if (item.Key is Building_MassStorageUnitPowered dsu)
                {
                    var powertrader = item.Value;
                    FridgePowerPatchUtil.FridgePowerDrawPerUnit.SetOrAdd(dsu, powertrader.powerOutputInt);
                    powertrader.powerOutputInt -= dsu.ExtraPowerDraw;
                }
            }
        }
    }
}
