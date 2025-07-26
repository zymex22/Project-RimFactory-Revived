using RimWorld;
using Verse;
namespace ProjectRimFactory.AutoMachineTool
{
    // ReSharper disable once UnusedType.Global
    public class PlaceWorker_Conveyor : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc,
                                 Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            // Notes to self: checkingDef is the def player is trying to place
            // thingToIgnore is (obviously) a Thing that should not be considered for the placing.
            //   as an example, rotating an item in place - we want to ignore the item itself!
            // thing is if it's an actual Thing that's being moved?

            if (checkingDef is not ThingDef def) return AcceptanceReport.WasAccepted; // probably terrain, not our problem

            var defModExtConveyor = checkingDef.GetModExtension<ModExtension_Conveyor>();
            // underground:
            if (typeof(Building_BeltConveyorUGConnector).IsAssignableFrom(def.thingClass)
                || defModExtConveyor?.underground == true)
            {
                // Cannot place these over (other) underground or connectors
                foreach (var thing1 in map.thingGrid.ThingsListAt(loc))
                {
                    if (thing1 == thingToIgnore || thing1 == thing) continue;
                    if (thing1 is Building_BeltConveyorUGConnector // other UGConnectors
                        || (thing1 as Building_BeltConveyor)?.IsUnderground == true // UG stuff
                                                                               // Blueprints:
                        || (thing1 is Blueprint blue && blue.def.entityDefToBuild is ThingDef td &&
                           (typeof(Building_BeltConveyorUGConnector).IsAssignableFrom(td.thingClass) || // other UG
                                                                                                        // and other UG stuff:
                            td.modExtensions?.Any(dme => dme is ModExtension_Conveyor mec && mec.underground) == true)))
                    {
                        return new AcceptanceReport("PRFBlockedBy".Translate(thing1.Label));
                    }
                }
            }
            // in walls
            if (checkingDef.placeWorkers?.Contains(typeof(PlaceWorker_WallEmbedded)) != true) return AcceptanceReport.WasAccepted;
            foreach (var t in map.thingGrid.ThingsListAt(loc))
            {
                if (t == thingToIgnore || t == thing) continue;
                if (t is IBeltConveyorLinkable and Thing && t.def.placeWorkers?.Contains(typeof(PlaceWorker_WallEmbedded)) == true)
                {
                    return new AcceptanceReport("PRFBlockedBy".Translate(t.Label));
                }
                if (t is Blueprint blue && blue.def.entityDefToBuild is ThingDef td)
                {
                    if (typeof(Building_BeltConveyor).IsAssignableFrom(td.thingClass) &&
                        td.placeWorkers?.Contains(typeof(PlaceWorker_WallEmbedded)) == true)
                    {
                        return new AcceptanceReport("PRFBlockedBy".Translate(t.Label));
                    }
                }
            }
            return AcceptanceReport.WasAccepted;
        }
    }
}
