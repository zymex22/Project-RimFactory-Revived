using System.Linq;
using RimWorld;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class PlaceWorker_AutoMachineTool : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
            Thing thingToIgnore = null, Thing thing = null)
        {
            var r = base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore);
            if (r.Accepted)
            {
                if ((loc + rot.FacingCell).GetThingList(map)
                    .Where(t => t.def.category == ThingCategory.Building)
                    .SelectMany(t => Option(t as Building_WorkTable))
                    .Where(b => b.InteractionCell == loc).Count() == 0)
                    return new AcceptanceReport("PRF.AutoMachineTool.PlaceNotAllowed".Translate());
                return r;
            }

            return r;
        }
    }
}