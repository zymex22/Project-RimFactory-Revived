using RimWorld;
using System;
using System.Linq;
using Verse;
namespace ProjectRimFactory.CultivatorTools
{
    public static class Utilities
    {
        /// <summary>
        /// Returns current Rot4 as a compass direction.
        /// </summary>
        public static string AsCompassDirection(this Rot4 rot)
        {
            switch (rot.AsByte)
            {
                case 0:
                    return "North".Translate();
                case 1:
                    return "East".Translate();
                case 2:
                    return "South".Translate();
                case 3:
                    return "West".Translate();
                default:
                    throw new ArgumentException("Invalid rotation or rotation factor " + rot + ". Valid rotations are 0, 1, 2 and 3");
            }
        }

        public static IPlantToGrowSettable GetIPlantToGrowSettable(IntVec3 c, Map map)
        {
            var zone = c.GetZone(map);
            var building = c.GetThingList(map).Where(t => t.def.category == ThingCategory.Building).Where(t => t is IPlantToGrowSettable).Select(t => (IPlantToGrowSettable)t).FirstOrDefault();
            if (building is IPlantToGrowSettable b) return b;
            if (zone is IPlantToGrowSettable z) return z;
            return null;
        }

        public static bool CanPlantRightNow(this IPlantToGrowSettable planter)
        {
            return (!planter.CanAcceptSowNow()) ? false :
                (planter is Zone_Growing z) ? z.allowSow :
                (planter is Thing t) ? !t.IsForbidden(Faction.OfPlayer) :
                true;
        }
    }
}
