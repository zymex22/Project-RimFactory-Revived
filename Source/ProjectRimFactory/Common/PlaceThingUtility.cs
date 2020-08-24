using System;
using ProjectRimFactory.Common;
using Verse;
using RimWorld;

namespace ProjectRimFactory {
    public static class PlaceThingUtility {
        // Always use this if you are placing an item.
        /// <summary>
        /// Place a Thing (onto the ground or into a proper receptacle)
        /// Any time ANY PRF building tries to place an item, this is THE way to do it:
        ///   It checks if the space is clear, it checks for conveyor belts, in checks
        ///   for other storage mods (well, Deep Storage, anyway).  It does all the 
        ///   things!
        /// </summary>
        /// <returns><c>true</c>, if item was placed, <c>false</c> if it could not.</returns>
        /// <param name="placer">The PRF_Building doing the placing</param>
        /// <param name="t">Thing to place.</param>
        /// <param name="cell">Where to place.</param>
        /// <param name="map">Map.</param>
        /// <param name="forcePlace">If set to <c>true</c>, forces placing. For when
        ///   you absolutely positively have to put it down.</param>
        public static bool PRFTryPlaceThing(this IPRF_Building placer, Thing t, IntVec3 cell, Map map,
            bool forcePlace = false) {
            // Storage:
            SlotGroup slotGroup = cell.GetSlotGroup(map);
            if (slotGroup != null) {
                Log.Warning("SlotGroup: " + slotGroup.parent);
                if (slotGroup.parent is IPRF_Building) {
                    if (placer.PlaceThingNextBuilding((slotGroup.parent as IPRF_Building),
                        t, cell, map)) {
                        return true;
                    }
                    if (forcePlace) goto ForcePlace;
                    return false;
                }
                if (placer.PlaceThingInSlotGroup(t, slotGroup, cell, map)) {
                    return true;
                }
                if (forcePlace) goto ForcePlace;
                return false;
            }
            // Search through all items in cell: see if any will absorb
            //   our thing.  If we find a PRF_Building, stop looking and
            //   try to pass it on.
            bool cellIsImpassible = false;
            foreach (Thing otherThing in map.thingGrid.ThingsListAt(cell)) {
                if (otherThing.TryAbsorbStack(t, true)) {
                    placer.EffectOnPlaceThing(otherThing); // I think?
                    return true;
                }
                if (otherThing.def.passability == Traversability.Impassable)
                    cellIsImpassible = true;
                if (otherThing is IPRF_Building) {
                    if (placer.PlaceThingNextBuilding((otherThing as IPRF_Building),
                        t, cell, map)) {
                        placer.EffectOnPlaceThing(t);
                        return true;
                    }
                    if (forcePlace) goto ForcePlace;
                    return false;
                }
            }
            // There is no IPRF Building to take this from us
            if (cellIsImpassible) {
                if (forcePlace) goto ForcePlace;
                return false;
            }
            // The cell is not impassible! Try to place:
            // IsValidStorageFor should also work for multi-storage mods
            if (StoreUtility.IsValidStorageFor(cell, map, t)) {
                if (t.Spawned) t.DeSpawn();
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Direct);
                placer.EffectOnPlaceThing(t);
                if (placer.ForbidOnPlacing()) t.SetForbidden(true, false);
                return true;
            }
            if (!forcePlace) return false;
            ForcePlace:
            if (t.Spawned) t.DeSpawn();
            GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);
            placer.EffectOnPlaceThing(t);
            if (placer.ForbidOnPlacing())
                t.SetForbidden(true, false);
            return true;
        }
        private static bool PlaceThingNextBuilding(this IPRF_Building placer,
            IPRF_Building nextBuilding, Thing t,
            IntVec3 cell, Map map) {
            if(nextBuilding.AcceptsThing(t, placer)) {
                placer.EffectOnPlaceThing(t);
                return true;
            }
            return false;
        }
        private static bool PlaceThingInSlotGroup(this IPRF_Building placer, Thing t,
            SlotGroup slotGroup, IntVec3 cell, Map map) {
            // Head off a lot of potential calculation:
            // TODO: should buildings be able to ignore this?
            //    (e.g., a conveyor belt that just dumps everything when it hits the end)
            // TODO: Will need to use NoStorageBlockersIn() to be able to do that
            // TODO: Deal properly with underground conveyors (need options to allow
            //       player to decide how that works.
            if (!slotGroup.parent.Accepts(t)) return false;
            // PRF Buildings are Magic, and can move stuff anywhere into a slotGroup
            //   (or, you know, they pile stuff up until it falls, or use a machine
            //    arm to move things, etc)
            //TODO: make this use NoStorageBlockersIn() - faster AND lets
            //   us set conveyor belts to dumping random stuff into stockpiles
            //   if we want to be silly (or realistic)
            Log.Message("Checking IsValidStorageFor(" + cell + ", map, " + t + "): "
                + StoreUtility.IsValidStorageFor(cell, map, t));
            if (StoreUtility.IsValidStorageFor(cell, map, t)) {
                if (t.Spawned) t.DeSpawn();
                if (!GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Direct)) {
                    Log.Error("Could not place thing??");
                }
                placer.EffectOnPlaceThing(t);
                if (placer.ForbidOnPlacing()) t.SetForbidden(true, false);
                return true;
            }
            return true;
        }





    }
}
