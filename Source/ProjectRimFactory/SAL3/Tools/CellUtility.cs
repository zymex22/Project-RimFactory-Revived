using RimWorld;
using Verse;

namespace ProjectRimFactory.SAL3.Tools
{
    public static class CellUtility
    {
        public static bool HasSlotGroupParent(this IntVec3 cell, Map map)
        {
            return cell.GetZone(map) is Zone_Stockpile || cell.GetThingList(map).Any(t => t is ISlotGroupParent);
        }
    }
}
