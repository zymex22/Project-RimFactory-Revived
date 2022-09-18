using System.Collections.Generic;
using System.Linq;
using Verse;
using ProjectRimFactory.Storage;

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
		public static IEnumerable<KeyValuePair<IntVec3, Building_AdvancedStorageUnitIOPort>> GetAdvancedIOPorts(Map map)
        {
			var Ports = PatchStorageUtil.GetPRFMapComponent(map).GetadvancedIOLocations.Where(l => (l.Value.boundStorageUnit?.Powered ?? false) && l.Value.CanGetNewItem);
			return Ports;
		}

		/// <summary>
		/// Orders IO Ports by Distance to an referencePos
		/// </summary>
		/// <param name="map"></param>
		/// <param name="referencePos"></param>
		/// <returns></returns>
		public static List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> GetOrderdAdvancedIOPorts(Map map, IntVec3 referencePos)
        {
			var dict_IOports = GetAdvancedIOPorts(map);
			List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> Ports = new List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>>();
			foreach (var pair in dict_IOports)
			{
				var distance = pair.Key.DistanceTo(referencePos);
				Ports.Add(new KeyValuePair<float, Building_AdvancedStorageUnitIOPort>(distance, pair.Value));
			}
			return Ports.OrderBy(i => i.Key).ToList();
		}


		public static List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> GetOrderdAdvancedIOPorts(Map map, IntVec3 pawnPos, IntVec3 targetPos)
		{
			var dict_IOports = GetAdvancedIOPorts(map);

			List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> Ports = new List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>>();
			foreach (var pair in dict_IOports)
			{
				var distance = CalculatePath(pawnPos, pair.Key, targetPos);
				Ports.Add(new KeyValuePair<float, Building_AdvancedStorageUnitIOPort>(distance, pair.Value));
			}
			
			return Ports.OrderBy(i => i.Key).ToList();
		}


		/// <summary>
		/// Returns a List of Ports where the in addition to the lower requirements they are additionally closer then a maxDistance 
		/// </summary>
		/// <param name="map"></param>
		/// <param name="referencePos"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
		public static IEnumerable<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> GetOrderdAdvancedIOPortsCloserThen(Map map, IntVec3 referencePos, float maxDistance)
		{
			return GetOrderdAdvancedIOPorts(map, referencePos).Where(i => i.Key < maxDistance);
		}

		/// <summary>
		/// Returns The Closest Port
		/// </summary>
		/// <param name="map"></param>
		/// <param name="referencePos"></param>
		/// <returns></returns>
		public static KeyValuePair<float, Building_AdvancedStorageUnitIOPort> GetClosestPort(Map map, IntVec3 referencePos)
        {
			return GetOrderdAdvancedIOPorts(map, referencePos).FirstOrDefault();
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
		public static KeyValuePair<float, Building_AdvancedStorageUnitIOPort> GetClosestPort(Map map, IntVec3 pawnPos, IntVec3 targetPos, Thing thing, float maxDistance)
		{
			return GetOrderdAdvancedIOPorts(map, pawnPos, targetPos).Where(p => p.Key < maxDistance && CanMoveItem(p.Value, thing)).FirstOrDefault();
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

		/// <summary>
		/// Calculates the Full Path Cost
		/// Checking for walls and alike
		/// The issue with this is that it is extramly expencive.
		/// 1 Call ~ 0.4ms
		/// I Hope there is a better way to make this kind of a check
		/// maybe a manual calculation without the extra stepps included?
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="thingPos"></param>
		/// <param name="targetPos"></param>
		/// <param name="map"></param>
		/// <returns></returns>
		public static float CalculatePath(Pawn pawn, IntVec3 thingPos, IntVec3 targetPos,Map map)
		{
			return map.pathFinder.FindPath(pawn.Position, thingPos, pawn).TotalCost + map.pathFinder.FindPath(thingPos, targetPos, pawn).TotalCost;
		}


	}
}
