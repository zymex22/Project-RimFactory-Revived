using HarmonyLib;
using ProjectRimFactory.Storage;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches;

[HarmonyPatch(typeof(Thing), "TryAbsorbStack")]
public class Patch_Thing_TryAbsorbStack
{

    private static PRFMapComponent prfMapComponent;
    private static bool relevant;
    private static int baseCount;
    
    public static bool Prefix(Thing __instance,  Thing other)
    {
        prfMapComponent = PatchStorageUtil.GetPRFMapComponent(__instance.Map);
        relevant = prfMapComponent?.ShouldHideItemsAtPos(__instance.Position) ?? false;
        if (relevant)
        {
            baseCount = other.stackCount;
        }
        return true;
    }
    
    // ReSharper disable once UnusedParameter.Global
    public static void Postfix( Thing other, bool respectStackLimit , Thing __instance)
    {
        if (!relevant) return;
        var dsu = __instance.Position.GetFirst<Building_MassStorageUnit>(__instance.Map);
        dsu?.ItemCountsAdded(other.def, baseCount - other.stackCount);
    }
}