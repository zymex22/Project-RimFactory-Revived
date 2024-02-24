using System.Linq;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class PlaceWorker_WallEmbedded : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (loc.GetThingList(map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t.def.building != null)
                // This keeps natural (unworked/un-smoothed) rocks from also qualifying:
                .Where(t => !t.def.building.isNaturalRock)
                .Where(t => t.def.passability == Traversability.Impassable)
                // Walls or (Smoothed) Rocks:  (smoothed rocks are !.isNaturalRock)
                .Any(t => (t.def.graphicData.linkFlags & LinkFlags.Wall) > 0    //walls
                       || (t.def.graphicData.linkFlags & LinkFlags.Rock) > 0))  //smoothed rocks
            {
                return AcceptanceReport.WasAccepted;
            }
            else
            {
                return new AcceptanceReport("PRF.AutoMachineTool.MustInWall".Translate());
            }
        }
    }
}
