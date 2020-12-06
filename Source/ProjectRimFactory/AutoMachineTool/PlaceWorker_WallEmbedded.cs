using System.Linq;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class PlaceWorker_WallEmbedded : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
            Thing thingToIgnore = null, Thing thing = null)
        {
            if (loc.GetThingList(map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t.def.building != null)
                .Where(t => !t.def.building.isNaturalRock)
                .Where(t => t.def.passability == Traversability.Impassable)
                .Any(t => (t.def.graphicData.linkFlags & LinkFlags.Wall) == LinkFlags.Wall))
                return AcceptanceReport.WasAccepted;
            return new AcceptanceReport("PRF.AutoMachineTool.MustInWall".Translate());
        }
    }
}