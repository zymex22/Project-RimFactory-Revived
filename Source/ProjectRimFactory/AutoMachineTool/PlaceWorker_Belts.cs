using System;
using UnityEngine;
using Verse;
using RimWorld;
namespace ProjectRimFactory.AutoMachineTool {
    public class PlaceWorker_Conveyor : PlaceWorker{
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, 
                                 Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null) {
            // Notes to self: checkingDef is the def player is trying to place
            // thingToIgnore is (obviously) a Thing that should not be considered for the placing.
            //   as an example, rotating an item in place - we want to ignore the item itself!
            // thing is if it's an actual Thing that's being moved?

            var def = checkingDef as ThingDef;
            if (def == null) return AcceptanceReport.WasAccepted; // No idea WTF it is??

            var defModExtConveyor = checkingDef.GetModExtension<ModExtension_Conveyor>();
            // underground:
            if (typeof(Building_BeltConveyorUGConnector).IsAssignableFrom(def.thingClass)
                || defModExtConveyor?.underground == true) {
                // Cannot place these over (other) underground or connectors
                foreach (var t in map.thingGrid.ThingsListAt(loc)) {
                    if (t == thingToIgnore || t == thing) continue;
                    if (t is Building_BeltConveyorUGConnector
                        || (t as Building_BeltConveyor)?.IsUnderground == true) {
                        return new AcceptanceReport("PRFBlockedBy".Translate(t.Label));
                    }
                }
            }

            return AcceptanceReport.WasAccepted;
        }
    }
}
