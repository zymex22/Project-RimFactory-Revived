using RimWorld;
using Verse;

namespace ProjectRimFactory.Common
{
    class PlaceWorker_GrowZonePuller : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            IntVec3 inputCell;
            inputCell = loc + rot.Opposite.FacingCell;

            if (inputCell.InBounds(map) && inputCell.GetZone(map) is Zone_Growing)
            {
                return AcceptanceReport.WasAccepted;
            }
            else
            {
                return new AcceptanceReport("PRF_PlaceWorker_GrowZonePuller".Translate());
            }
        }

    }
}
