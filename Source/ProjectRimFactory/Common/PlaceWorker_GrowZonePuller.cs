using RimWorld;
using Verse;

namespace ProjectRimFactory.Common
{
    class PlaceWorker_GrowZonePuller : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
             var inputCell = loc + rot.Opposite.FacingCell;

            if (inputCell.InBounds(map) && inputCell.GetZone(map) is IPlantToGrowSettable)
            {
                return AcceptanceReport.WasAccepted;
            }

            return new AcceptanceReport("PRF_PlaceWorker_GrowZonePuller".Translate());
        }

    }
}
