using System.Linq;
using HarmonyLib;
using ProjectRimFactory.Storage;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches;

[HarmonyPatch(typeof(Thing), "TryAbsorbStack")]
public class Patch_Thing_TryAbsorbStack
{

    private static PRFMapComponent prfMapComponent;
    private static bool relevant = false;
    private static int baseCount = 0;
    
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
    
    public static void Postfix( Thing other, bool respectStackLimit , Thing __instance)
    {
        if (!relevant) return;
        var dsu = __instance.Position.GetFirst<Building_MassStorageUnit>(__instance.Map);
        dsu?.ItemCountsAdded(other.def, baseCount - other.stackCount);
    }
}