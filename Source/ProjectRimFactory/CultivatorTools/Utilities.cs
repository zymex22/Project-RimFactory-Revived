using Verse;
using RimWorld;
using System.Linq;
using System;
using UnityEngine;
namespace ProjectRimFactory.CultivatorTools
{
    public class CultivatorDefModExtension : DefModExtension
    {
        public int TickFrequencyDivisor = 200;
        public int squareAreaRadius;
    }
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

        /// <summary>
        /// Finds if SeedsPlease is active by seeing if the seeds texture exists
        /// </summary>
        public static bool SeedsPleaseActive => 
            ContentFinder<Texture2D>.Get("Things/Item/Seeds/Seeds/Seeds_b", false) != null;

        public static IPlantToGrowSettable GetIPlantToGrowSettable(IntVec3 c, Map map)
        {
            var zone = c.GetZone(map);
            var building = c.GetFirstBuilding(map);
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
