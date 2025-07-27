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
            var parent = __instance.parent;
            if (parent is Building_MassStorageUnitPowered dsu)
            {
                var powerTrader = parent.GetComp<CompPowerTrader>();
                FridgePowerPatchUtil.FridgePowerDrawPerUnit.SetOrAdd(dsu, powerTrader.powerOutputInt);
                powerTrader.powerOutputInt -= dsu.ExtraPowerDraw;
            }
        }
    }
}
