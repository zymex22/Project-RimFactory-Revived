using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common
{

    public class PlaceWorker_NaturalWall : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {

            //TODO Consider Building Size and not just the center
            List<Thing> thingList = loc.GetThingList(map);

            if (thingList
                .Where(t => t.def.IsNonResourceNaturalRock || t.def.IsSmoothed)
                .Any())
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
