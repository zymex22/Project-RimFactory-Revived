using ProjectRimFactory.Storage;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    public static class AdvancedIO_PatchHelper
    {

        /// <summary>
        /// Gets all Ports that could be used
        /// They are Powerd, connected and the connected DSU is also powerd
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<IntVec3, Building_AdvancedStorageUnitIOPort>> GetAdvancedIOPorts(Map map)
        {
            var ports = PatchStorageUtil.GetPRFMapComponent(map).
                GetAdvancedIOLocations.Where(l => 
                    (l.Value.boundStorageUnit?.Powered ?? false) && l.Value.CanGetNewItem);
            return ports;
        }

        /// <summary>
        /// Orders IO Ports by Distance to an referencePos
        /// </summary>
        /// <param name="map"></param>
        /// <param name="referencePos"></param>
        /// <returns></returns>
        private static List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> GetOrderedAdvancedIOPorts(Map map, IntVec3 referencePos)
        {
            var dictIOPorts = GetAdvancedIOPorts(map);
            var ports = new List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>>();
            foreach (var pair in dictIOPorts)
            {
                var distance = pair.Key.DistanceTo(referencePos);
                ports.Add(new KeyValuePair<float, Building_AdvancedStorageUnitIOPort>(distance, pair.Value));
            }
            return ports.OrderBy(i => i.Key).ToList();
        }


        public static List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> GetOrderedAdvancedIOPorts(Map map,
            IntVec3 pawnPos, IntVec3 targetPos)
        {
            var dictIOPorts = GetAdvancedIOPorts(map);

            var ports = new List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>>();
            foreach (var pair in dictIOPorts)
            {
                var distance = CalculatePath(pawnPos, pair.Key, targetPos);
                ports.Add(new KeyValuePair<float, Building_AdvancedStorageUnitIOPort>(distance, pair.Value));
            }

            return ports.OrderBy(i => i.Key).ToList();
        }

        /// <summary>
        /// Returns The Closest Port
        /// </summary>
        /// <param name="map"></param>
        /// <param name="referencePos"></param>
        /// <returns></returns>
        public static KeyValuePair<float, Building_AdvancedStorageUnitIOPort> GetClosestPort(Map map, IntVec3 referencePos)
        {
            return GetOrderedAdvancedIOPorts(map, referencePos).FirstOrDefault();
        }

        /// <summary>
        /// Returns the Closest Port that can transport a specific thing
        /// While being closer then a defined maxDistance
        /// </summary>
        /// <param name="map"></param>
        /// <param name="pawnPos"></param>
        /// <param name="targetPos"></param>
        /// <param name="thing"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public static KeyValuePair<float, Building_AdvancedStorageUnitIOPort> GetClosestPort(Map map, IntVec3 pawnPos,
            IntVec3 targetPos, Thing thing, float maxDistance)
        {
            return GetOrderedAdvancedIOPorts(map, pawnPos, targetPos)
                .FirstOrDefault(p => p.Key < maxDistance && CanMoveItem(p.Value, thing));
        }


        /// <summary>
        /// Checks if a Port can Move a specific Item
        /// </summary>
        /// <param name="port"></param>
        /// <param name="thing"></param>
        /// <returns></returns>
        public static bool CanMoveItem(Building_AdvancedStorageUnitIOPort port, Thing thing)
        {
            return port.boundStorageUnit?.StoredItems?.Contains(thing) ?? false;
        }

        /// <summary>
        /// Checks if a Port can Move a specific Item
        /// </summary>
        /// <param name="port"></param>
        /// <param name="thingPos"></param>
        /// <returns></returns>
        public static bool CanMoveItem(Building_AdvancedStorageUnitIOPort port, IntVec3 thingPos)
        {
            return port.boundStorageUnit?.HoldsPos(thingPos) ?? false;
        }


        /// <summary>
        /// Calculates the Full Path Cost
        /// But it can't see walls / Tarrain
        /// This is cheap
        /// 1 Call ~ 0.2us
        /// </summary>
        /// <param name="pawnPos"></param>
        /// <param name="thingPos"></param>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        public static float CalculatePath(IntVec3 pawnPos, IntVec3 thingPos, IntVec3 targetPos)
        {
            return pawnPos.DistanceTo(thingPos) + thingPos.DistanceTo(targetPos);
        }
    }
}
