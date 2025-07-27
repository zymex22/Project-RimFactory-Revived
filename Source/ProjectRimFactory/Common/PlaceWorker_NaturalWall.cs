using System.Linq;
using Verse;

namespace ProjectRimFactory.Common
{

    public class PlaceWorker_NaturalWall : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            var allcells = GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size);
            if (
                allcells.All(t =>
                    t.GetThingList(map).Any(thing1 => thing1.def.IsNonResourceNaturalRock || thing1.def.IsSmoothed)
                    && !(t.GetThingList(map).Any(thing1 => thing1.def == (checkingDef as ThingDef)))
                )
            )
            {
                return AcceptanceReport.WasAccepted;
            }

            return new AcceptanceReport("PRF_PlaceWorker_NaturalWall_denied".Translate());
        }

        public override bool ForceAllowPlaceOver(BuildableDef other)
        {
            if (other.blueprintDef is { IsSmoothed: true })
            {

                return true;
            }

            if (other is ThingDef { IsNonResourceNaturalRock: true })
            {

                return true;
            }

            return base.ForceAllowPlaceOver(other);
        }
    }

}
