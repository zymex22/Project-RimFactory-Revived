using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    // ReSharper disable once UnusedType.Global
    public class PlaceWorker_AutoMachineTool : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            var r = base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore, thing);
            if (r.Accepted && !(new Building_AutoMachineTool()).GetTarget(loc, rot, map))
            { 
                return new AcceptanceReport("PRF.AutoMachineTool.PlaceNotAllowed".Translate());
            }

            return r;
        }
    }
}