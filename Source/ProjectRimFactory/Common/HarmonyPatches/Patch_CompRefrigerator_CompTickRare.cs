using ProjectRimFactory.Storage;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /// <summary>
    /// Patches the CompRefrigerator.CompTickRare contained in RimFridge
    /// This is needed to draw the correct amount of power for the Refrigerated DSU
    /// </summary>
    public static class Patch_CompRefrigerator_CompTickRare
    {
        public static void Postfix(ThingComp __instance)
        {
            var parrent = __instance.parent;
            if (parrent is Building_MassStorageUnitPowered dsu)
            {
                var powertrader = parrent.GetComp<CompPowerTrader>();
                FridgePowerPatchUtil.FridgePowerDrawPerUnit.SetOrAdd(dsu, powertrader.powerOutputInt);
                powertrader.powerOutputInt -= dsu.ExtraPowerDraw;
            }
        }
    }
}
