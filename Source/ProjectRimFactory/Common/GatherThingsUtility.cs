using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.AutoMachineTool;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common
{
    public static class GatherThingsUtility
    {
        /// <summary>
        /// A list of cells that might be appropriate for a PRF building to gather input items from.
        ///   If the PRF building has a CompPowerWorkSetting comp, it uses that, otherwise
        ///   it defaults to all adjacent cells around the building.
        /// </summary>
        /// <param name="building">A building. Probably a PRF building. Probably one that makes things.</param>
        public static IEnumerable<IntVec3> InputCells(this Building building)
        {
            return building.GetComp<CompPowerWorkSetting>()?.GetRangeCells() ?? GenAdj.CellsAdjacent8Way(building);
        }

        /// <summary>
        /// WARNING MAY CONTAIN DUPLICATES
        /// A list of items a PRF building might want to use as input resources, either on the ground,
        ///   in storage, or on coveyor belts.
        /// </summary>
        /// <returns>The items in cell <paramref name="c"/> for use.</returns>
        /// <param name="c">Cell</param>
        /// <param name="map">Map</param>
        /// <param name="allowStorageZones"></param>
        /// <param name="allowUGEntranceConnector"></param>
        /// <param name="allowUGBelts"></param>
        public static IEnumerable<Thing> AllThingsInCellForUse(this IntVec3 c, Map map, bool allowStorageZones = true,
            bool allowUGEntranceConnector = false, bool allowUGBelts = false)
        {

            if (!c.InBounds(map)) yield break;
            var thingList = map.thingGrid.ThingsListAt(c);
            //Risk for duplicate entrys if a cell contins both a Item & a IThingHolder that holds said item
            for (int i = thingList.Count - 1; i >= 0; i--)
            {
                Thing t = thingList[i];
                if (t is Building && t is IThingHolder holder && !(t is Frame))
                {
                    if (holder.GetDirectlyHeldThings() is not ThingOwner<Thing> owner) continue;
                    if (t is Building_BeltConveyor belt && belt.IsUnderground && !allowUGBelts)
                    {
                        // It the target is an Underground Belt & wen don't Explicitly allow that then don't place anything
                        continue;
                    }

                    if (t is Building_BeltConveyorUGConnector { ToUnderground: true })
                    {
                        if (!allowUGEntranceConnector) {
                            continue; // Don't allow Entrance unless specifically allowed
                        }
                    }

                    // Exits should always be allowed
                    for (var j = owner.InnerListForReading.Count - 1; j >= 0; j--)
                    {
                        yield return owner.InnerListForReading[j];
                    }
                }
                else if (t.def.category == ThingCategory.Item)
                {
                    yield return t;
                }
                //This should support all other storage Buildings
                else if (t is Building_Storage storage)
                {
                    foreach (var thing in storage.GetSlotGroup().HeldThings)
                    {
                        yield return thing;
                    }
                }
            }
            //Pull from Storage Zones
            if (allowStorageZones && c.GetZone(map) is Zone_Stockpile sz)
            {
                foreach (var thing in sz.AllContainedThings.Where(t => t.def.category == ThingCategory.Item))
                {
                    yield return thing;
                }
            }
        }
    }
}
