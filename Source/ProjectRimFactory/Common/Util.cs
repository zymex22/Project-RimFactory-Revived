using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRimFactory.Common
{
    public static class Util
    {
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
        public static bool PlaceItem(this IPRF_Building placer, Thing t, IntVec3 cell, Map map, 
            bool forcePlace=false) {
            // Storage:
            SlotGroup slotGroup = cell.GetSlotGroup(map);
            if (slotGroup!=null) {
                if (slotGroup.parent is IPRF_Building) {
                    if (placer.PlaceItemNextBuilding((slotGroup.parent as IPRF_Building),
                        t, cell, map)) {
                        return true;
                    }
                    if (forcePlace) goto ForcePlace;
                    return false;
                }
                if (placer.PlaceItemInSlotGroup(t, slotGroup, cell, map)) {
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
                    return true;
                    // TODO: should effect go here?
                }
                if (otherThing.def.passability == Traversability.Impassable)
                    cellIsImpassible = true;
                if (otherThing is IPRF_Building) {
                    if (placer.PlaceItemNextBuilding((otherThing as IPRF_Building),
                        t, cell, map)) {
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
                if (placer.ForbidOnPlacing()) t.SetForbidden(true, false);
//                effect(t);
                return true;
            }
            if (!forcePlace) return false;
            ForcePlace:
            if (t.Spawned) t.DeSpawn();
            // TODO: effet?
            GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);
            if (placer.ForbidOnPlacing())
                t.SetForbidden(true, false);
            return true;
        }
        private static bool PlaceItemNextBuilding(this IPRF_Building placer, 
            IPRF_Building nextBuilding, Thing t, 
            IntVec3 cell, Map map) {
            return nextBuilding.AcceptsItem(t);
        }
        private static bool PlaceItemInSlotGroup(this IPRF_Building placer, SlotGroup slotGroup,
            Thing t, IntVec3 cell, Map map) {
            // Head off a lot of potential calculation:
            // TODO: should buildings be able to ignore this?
            //    (e.g., a conveyor belt that just dumps everything when it hits the end)
            if (!slotGroup.parent.Accepts(t)) return false;
            // PRF Buildings are Magic, and can move stuff anywhere into a slotGroup
            //   (or, you know, they pile stuff up until it falls, or use a machine
            //    arm to move things, etc)
            //TODO: make this use NoStorageBlockersIn() - faster AND lets
            //   us set conveyor belts to dumping random stuff into stockpiles
            //   if we want to be silly (or realistic)
            if (StoreUtility.IsValidStorageFor(cell, map, t)) {
                if (t.Spawned) t.DeSpawn();
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Direct);
                if (placer.ForbidOnPlacing()) t.SetForbidden(true, false);
                //TODO: effect
                //                effect(t);
                return true;
            }
            return true;
        }

        public static Color A(this Color color, float a)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.A(color, a);
        }

        public static IntVec3 FacingCell(IntVec3 center, IntVec2 size, Rot4 rot)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.FacingCell(center, size, rot);
        }

        public static IEnumerable<IntVec3> FacingRect(IntVec3 pos, Rot4 dir, int range)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.FacingRect(pos, dir, range);
        }

        public static IEnumerable<IntVec3> FacingRect(Thing thing, Rot4 dir, int range)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.FacingRect(thing, dir, range);
        }

        public static IEnumerable<IntVec3> FacingRect(IntVec3 center, IntVec2 size, Rot4 dir, int range)
        {
            return ProjectRimFactory.AutoMachineTool.Ops.FacingRect(center, size, dir, range);
        }
    }
}
