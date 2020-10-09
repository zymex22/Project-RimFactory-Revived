using System;
using System.Reflection;
using System.Reflection.Emit; // for dynamic method
using System.Collections.Generic;
using System.Linq;
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
                Debug.Warning(Debug.Flag.PlaceThing, "Placing " + t + " in slotGroup: " + slotGroup.parent + " at " + cell);
                if (slotGroup.parent is IPRF_Building) {
                    if (placer.PlaceThingNextBuilding((slotGroup.parent as IPRF_Building),
                        t, cell, map)) {
                        Debug.Message(Debug.Flag.PlaceThing, "  which is owned by PRF " + slotGroup.parent);
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
            Debug.Warning(Debug.Flag.PlaceThing, "Place request: " + placer == null ? "NoPlacer" : placer.ToString() + " is trying to place " + t + " at " + cell);
            // Search through all items in cell: see if any will absorb
            //   our thing.  If we find a PRF_Building, stop looking and
            //   try to pass it on.
            bool cellIsImpassible = false;
            foreach (Thing otherThing in map.thingGrid.ThingsListAt(cell)) {
                if (otherThing.TryAbsorbStack(t, true)) {
                    Debug.Message(Debug.Flag.PlaceThing, "  absorbed by " + otherThing);
                    placer.EffectOnPlaceThing(otherThing); // I think?
                    return true;
                }
                if (otherThing.def.passability == Traversability.Impassable)
                    cellIsImpassible = true;
                if (otherThing is IPRF_Building) {
                    if (placer.PlaceThingNextBuilding((otherThing as IPRF_Building),
                        t, cell, map)) {
                        Debug.Message(Debug.Flag.PlaceThing, placer +
                          " gave " + t + " to " + otherThing);
                        placer.EffectOnPlaceThing(t);
                        return true;
                    }
                    // Continue loop - may be more than 1 PRF_Building here
                }
            }
            // There is no IPRF Building to take this from us
            if (cellIsImpassible) {
                Debug.Message(Debug.Flag.PlaceThing, "  cell is impassable.");
                if (forcePlace) goto ForcePlace;
                return false;
            }
            // The cell is not impassible! Try to place:
            if (CallNoStorageBlockersIn(cell, map, t)) {
                Debug.Message(Debug.Flag.PlaceThing, "  placing directly.");
                if (t.Spawned) t.DeSpawn();
                if (!GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Direct)) {
                    Log.Error("Could not place thing " + t + " at " + cell);
                }
                placer.EffectOnPlaceThing(t);
                if (placer.ForbidOnPlacing(t)) t.SetForbidden(true, false);
                return true;
            }
            if (!forcePlace) return false;
            ForcePlace:
            Debug.Warning(Debug.Flag.PlaceThing, "  Placement is forced!");
            if (t.Spawned) t.DeSpawn();
            GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);
            placer.EffectOnPlaceThing(t);
            if (placer.ForbidOnPlacing(t))
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
            if (placer?.OutputToEntireStockpile == true) return PlaceThingAnywhereInSlotGroup(placer, t, slotGroup, cell);
            // Head off a lot of potential calculation:
            // TODO: Deal properly with underground conveyors (need options to allow
            //       player to decide how that works.
            if (placer.ObeysStorageFilters && !slotGroup.parent.Accepts(t)) return false;
            if (CallNoStorageBlockersIn(cell, map, t)) {
                Debug.Message(Debug.Flag.PlaceThing, "Found NoStorageBlockersIn(" + cell + ", map, " + t + ") - Placing");
                if (t.Spawned) t.DeSpawn();
                if (!GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Direct)) {
                    // should never happen??
                    Log.Error("Could not place thing "+t+" at "+cell);
                }
                placer.EffectOnPlaceThing(t);
                if (placer.ForbidOnPlacing(t)) t.SetForbidden(true, false);
                return true;
            }
            Debug.Message(Debug.Flag.PlaceThing, "There were StorageBlockersIn(" + cell + ", map, " + t + ") - cannot place");
            return false;
        }
        /// <summary>
        /// Some PRF Buildings are Magic, and can move stuff anywhere into a slotGroup
        ///   (or, you know, they pile stuff up until it falls, or use a machine
        ///    arm to move things, etc)
        /// </summary>
        /// <returns><c>true</c>, if thing was placed somewhere; <c>false</c> otherwise.</returns>
        /// <param name="placer">IPRF Buidling placing.</param>
        /// <param name="t">thing to place.</param>
        /// <param name="slotGroup">SlotGroup.</param>
        /// <param name="cell">optional first cell</param>
        public static bool PlaceThingAnywhereInSlotGroup(this IPRF_Building placer, Thing t,
            SlotGroup slotGroup, IntVec3? cell=null) {
            // Should we even be putting anything here?
            if (placer.ObeysStorageFilters && !slotGroup.parent.Accepts(t)) return false;
            Map map = placer.Map;
            // Go through slotGroup, starting with cell if given
            // TODO: go thru slotgroup in order of increasing distance from cell?
            foreach (var c in (cell != null ? ((new[] { (IntVec3)cell }).Concat(slotGroup.CellsList.Where(x => x != cell)))
                                          : slotGroup.CellsList)) {
                if (CallNoStorageBlockersIn(c, map, t)) {
                    Debug.Message(Debug.Flag.PlaceThing, "Found NoStorageBlockersIn(" + cell + ", map, " + t + ") - Placing");
                    if (t.Spawned) t.DeSpawn();
                    if (!GenPlace.TryPlaceThing(t, c, map, ThingPlaceMode.Direct)) {
                        // should never happen??//TODO: This happens if some was absorbed!! Handle this gracefully!!
                        Log.Error("Could not place thing " + t + " at " + cell);
                        return false;
                    }
                    placer.EffectOnPlaceThing(t);
                    if (placer.ForbidOnPlacing(t)) t.SetForbidden(true, false);
                    return true;
                }
            }
            Debug.Message(Debug.Flag.PlaceThing, "There were StorageBlockersIn every cell of " 
                       + slotGroup.parent + " - cannot place" + t);
            return false;
        }

        public static bool CanPlaceThingInSlotGroup(Thing t, SlotGroup slotGroup, Map map) {
            foreach (var c in slotGroup.CellsList) {
                if (CallNoStorageBlockersIn(c, map, t)) {
                    return true;
                }
            }
            Debug.Message(Debug.Flag.PlaceThing, "There were StorageBlockersIn every cell of "
                       + slotGroup.parent + " - can not place" + t);
            return false;
        }


        // static constructor to prep dynamic method
        static PlaceThingUtility() {
            // Set up CallNoStorageBlockersIn, to allow fast calling:
            // #DeepMagic
            var dm = new DynamicMethod("directly call RimWorld.StoreUtility's NoStorageBlockersIn",
                typeof(bool), new Type[] { typeof(IntVec3), typeof(Map), typeof(Thing) },
                true // skin JIT visibility checks - calling a private method is the entire point!
                );
            var il = dm.GetILGenerator();
            // build our function from IL Because why not?
            //   still faster than reflection!
            il.Emit(OpCodes.Ldarg_0); // put IntVec3 cell on the stack
            il.Emit(OpCodes.Ldarg_1); // Map
            il.Emit(OpCodes.Ldarg_2); // Thing
            //il.Emit(OpCodes.Ldarg_0); // put IntVec3 cell on the stack//uncomment for debug
            //il.Emit(OpCodes.Ldarg_1); // Map for debug
            //il.Emit(OpCodes.Ldarg_2); // Thing for debug
            il.Emit(OpCodes.Call, typeof(RimWorld.StoreUtility).GetMethod("NoStorageBlockersIn",
                BindingFlags.Static | BindingFlags.NonPublic));
            //il.Emit(OpCodes.Call, typeof(PlaceThingUtility).GetMethod("PostCallDebug", BindingFlags.Static | BindingFlags.Public));
            il.Emit(OpCodes.Ret);
            // Now do the magic to make it an actually callable Func<>:
            CallNoStorageBlockersIn = (Func<IntVec3, Map, Thing, bool>)dm.CreateDelegate(
                typeof(Func<,,,>).MakeGenericType(typeof(IntVec3), typeof(Map), typeof(Thing), typeof(bool)));
            // Mayb there's a way to make that faster? But this works ^.^
        }
        /*public static bool PostCallDebug(IntVec3 c, Map map, Thing t, bool res) {
            Log.Message("NSBI: " + t + " at " + c + "; result: " + res);
            return res;
        }*/
        // Call via "    bool result=CallNoStorageBlockersIn(c, map, thing);"
        public static Func<IntVec3, Map, Thing, bool> CallNoStorageBlockersIn;
    }
}
