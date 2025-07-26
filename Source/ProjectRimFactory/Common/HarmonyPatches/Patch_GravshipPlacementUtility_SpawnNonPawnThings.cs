using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Storage;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches;

/// <summary>
/// This Patch ensures that when Moving the Gravship, the items of a Building_MassStorageUnit will not get spilled
/// </summary>
[HarmonyLib.HarmonyPatch(typeof(GravshipPlacementUtility), "SpawnNonPawnThings")]
public class Patch_GravshipPlacementUtility_SpawnNonPawnThings
{

    private static List<Building_MassStorageUnit> storedBuildings = [];
     
    public static bool Prefix(Gravship gravship, Map map, List<Thing> gravshipThings, IntVec3 root)
    {
        storedBuildings = gravshipThings.OfType<Building_MassStorageUnit>().ToList();
        foreach (var thing in storedBuildings.Where(thing => thing.def.building.maxItemsInCell == 1))
        {
            // TODO: I wonder would there be any negative effect in always defaulting to int.MaxValue
            thing.def.building.maxItemsInCell = thing.ModExtensionCrate?.limit ?? int.MaxValue;
        }
            
        return true;
    }

    public static void Postfix(Gravship gravship, Map map, List<Thing> gravshipThings, IntVec3 root)
    {
        foreach (var thing in storedBuildings)
        {
            thing.def.building.maxItemsInCell = 1;
            thing.RefreshStorage(true);
        }
    }
    
}