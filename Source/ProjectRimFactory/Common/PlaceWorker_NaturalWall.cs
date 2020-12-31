using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common
{

    public class PlaceWorker_NaturalWall : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            IEnumerable<IntVec3> allcells = GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size);
            if (
                allcells.All(t => 
                    t.GetThingList(map).Where(t => (t.def.IsNonResourceNaturalRock && !ModLister.HasActiveModWithName("Replace Stuff")) || t.def.IsSmoothed).Any()
                    && !(t.GetThingList(map).Where(t => t.def == (checkingDef as ThingDef)).Any())

                )
                
            )
            {
                return AcceptanceReport.WasAccepted;
            }
            else
            {
                if (ModLister.HasActiveModWithName("Replace Stuff"))
                {
                    return new AcceptanceReport("PRF_PlaceWorker_NaturalWall_denied_ReplaceStuff".Translate());
                }
                return new AcceptanceReport("PRF_PlaceWorker_NaturalWall_denied".Translate());
            }
        }

        public override bool ForceAllowPlaceOver(BuildableDef other)
        {
            if (other.blueprintDef != null && other.blueprintDef.IsSmoothed)
            {

                return true;
            }
            var def = other as ThingDef;
            if (def != null && def.IsNonResourceNaturalRock && !ModLister.HasActiveModWithName("Replace Stuff"))
            {

                return true;
            }

            return base.ForceAllowPlaceOver(other);
        }
    }

}
