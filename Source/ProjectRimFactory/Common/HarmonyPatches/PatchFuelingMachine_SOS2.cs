using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using ProjectRimFactory.Industry;
using HarmonyLib;

namespace ProjectRimFactory.Common.HarmonyPatches
{

    [HarmonyPatch(typeof(Building_FuelingMachine), "checkIfThingIsSoS2Weapon")]
    class Patch_checkIfThingIsSoS2Weapon
    {
        static bool Prefix(Building_FuelingMachine __instance, ref Thing __result, Thing thing)
        {
            if (LoadedMods.Metha_sos2 == null)
            {
                return true;
            }
            CompChangeableProjectilePlural compChangeableProjectilePlural = (thing as Building_ShipTurret)?.gun?.TryGetComp<CompChangeableProjectilePlural>();
            if (compChangeableProjectilePlural != null)
            {
                __result = thing;
            }
            else
            {
                __result = null;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Building_FuelingMachine), "refuelSoSWeapon")]
    class Patch_refuelSoSWeapon
    {
        static bool Prefix(Building_FuelingMachine __instance, Thing thing)
        {
            if (LoadedMods.Metha_sos2 == null)
            {
                return true;
            }
            CompChangeableProjectilePlural compChangeableProjectilePlural = (thing as Building_ShipTurret)?.gun?.TryGetComp<CompChangeableProjectilePlural>();
            //Check if we need to load it
            if (compChangeableProjectilePlural.Loaded == false)
            {
                foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(__instance))
                {
                    List<Thing> l = __instance.Map.thingGrid.ThingsListAt(cell);
                    for (int i = l.Count - 1; i >= 0; i--)
                    { // count down because items may be destroyed
                        Thing item = l[i];
                        // Without this check, if there is something that is fueled by
                        //     minified Power Conduits (weird, but ...possible?), then
                        //     our FuelingMachine will happily rip conduits out of the
                        //     ground to fuel it.  I'm okay with this behavior.
                        //     Feature.  Not a bug.
                        // But if it ever causes a problem, uncomment this check:
                        // if (item.def.category != ThingCategory.Item) continue;
                        if (compChangeableProjectilePlural.allowedShellsSettings.filter.Allows(item))
                        {
                            //Load Item
                            compChangeableProjectilePlural.LoadShell(item.def, 1);
                            if (item.stackCount > 1)
                            {
                                item.SplitOff(1).Destroy();
                            }
                            else
                            {
                                item.Destroy();
                            }
                            
                            if (compChangeableProjectilePlural.Loaded) return false;
                        }
                    }
                }
            }

            return false;
        }
    }
}
