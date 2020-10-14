using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.Industry
{
    public class Building_FuelingMachine : Building
    {
        public IntVec3 FuelableCell => Rotation.FacingCell + Position;
        public override void Tick()
        { // in case you *really* want to use Tick
            base.Tick();
            if (this.IsHashIntervalTick(10)) Refuel();
        }
        public override void TickRare() { // prefer to use TickRare
            base.TickRare();
            Refuel();
        }

        //This function will be Harmony Patched if SOS2 is instaled
        //This is nessesary to support play without SOS2
        private Thing checkIfThingIsSoS2Weapon(Thing thing)
        {
            return null;
        }
        //This function will be Harmony Patched if SOS2 is instaled
        //This is nessesary to support play without SOS2
        private void refuelSoSWeapon(Thing thing)
        {
            return;
        }


        public void Refuel() {
            if (!GetComp<CompPowerTrader>().PowerOn) return;
            // Get what we are supposed to refuel:
            //   (only refuel one thing - if you need to adjust this to fuel more
            //    than one thing, make the loop here and put some breaking logic
            //    instead of all the "return;"s below)
            CompRefuelable refuelableComp = null;

            foreach (Thing tmpThing in Map.thingGrid.ThingsListAt(FuelableCell)) {
                if (tmpThing is Building) {
                    refuelableComp = (tmpThing as Building).GetComp<CompRefuelable>();
                    // Check if there is already enough fuel:
                    //  (because Fuel is a float, we check the current fuel + .99, so it
                    //   only refuels when Fuel drops at least one full unit below taret level)
                    if (refuelableComp != null && refuelableComp.Fuel + 0.9999f < refuelableComp.TargetFuelLevel) {
                        foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(this)) {
                            List<Thing> l = Map.thingGrid.ThingsListAt(cell);
                            for (int i = l.Count - 1; i >= 0; i--) { // count down because items may be destroyed
                                Thing item = l[i];
                                // Without this check, if there is something that is fueled by
                                //     minified Power Conduits (weird, but ...possible?), then
                                //     our FuelingMachine will happily rip conduits out of the
                                //     ground to fuel it.  I'm okay with this behavior.
                                //     Feature.  Not a bug.
                                // But if it ever causes a problem, uncomment this check:
                                // if (item.def.category != ThingCategory.Item) continue;
                                if (refuelableComp.Props.fuelFilter.Allows(item)) {
                                    // round down to not waste fuel:
                                    int num = Mathf.Min(item.stackCount, Mathf.FloorToInt(refuelableComp.TargetFuelLevel - refuelableComp.Fuel));
                                    if (num > 0) {
                                        refuelableComp.Refuel(num);
                                        item.SplitOff(num).Destroy();
                                    } else { // It's not quite 1 below TargetFuelLevel
                                             // but we KNOW we are at least .9999f below TargetFuelLevel (see test above)
                                             // So we call it close enough to 1:
                                        refuelableComp.Refuel(1);
                                        item.SplitOff(1).Destroy();
                                    }
                                    // check fuel as float (as above)
                                    if (refuelableComp.Fuel + 0.9999f >= refuelableComp.TargetFuelLevel) goto Fueled; // fully fueled
                                }
                            }
                        }
                    }
                } // end if is Building
            Fueled:
                //This is a generalized system for mod compatibility - mostlyfor Mortar like buildings
                foreach (var rr in registeredRefuelables) {
                    // basically, if (tmpThing is Building_From_rr)
                    if (rr.buildingType.IsAssignableFrom(tmpThing.GetType())) {
                        object o = rr.objectThatNeedsFueling(tmpThing as Building);
                        if (o != null) { // it needs "refueling" - or ammo loaded
                            foreach (Thing t in AllThingsForFueling) {
                                int count = rr.fuelTest(o, t);
                                if (count > 0) { // it wants some of this for fuel/ammo!
                                    Thing fuel = t.SplitOff(count);
                                    rr.refuelAction(o, fuel);
                                    goto Fueled;
                                }
                            }
                        }
                    }
                }
            } // end loop checking for things that need fuel
        }

        protected IEnumerable<Thing> AllThingsForFueling {
            get {
                foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(this)) {
                    List<Thing> l = Map.thingGrid.ThingsListAt(cell);
                    for (int i = l.Count - 1; i >= 0; i--) { // count down because items may be destroyed
                        yield return l[i];
                    }
                }
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(new List<IntVec3>(GenAdj.CellsAdjacent8Way(this)));
            GenDraw.DrawFieldEdges(new List<IntVec3>() { FuelableCell }, Color.yellow);
        }
        //****************************** Registering Refuelables from other mods *****************//
        public static void RegisterRefuelable(Type buildingType, Func<Building, object> objectThatNeedsFueling, 
                                              Func<object, Thing, int> fuelTest,
                                              Action<object, Thing> refuelAction) {
            registeredRefuelables.Add(new RegisteredRefuelable(buildingType, objectThatNeedsFueling, fuelTest,
                refuelAction));
        }
        protected static List<RegisteredRefuelable> registeredRefuelables = new List<RegisteredRefuelable>();

        protected class RegisteredRefuelable {
            public readonly Type buildingType;
            public readonly Func<Building, object> objectThatNeedsFueling;
            public readonly Func<object, Thing, int> fuelTest;
            public readonly Action<object, Thing> refuelAction;
            public RegisteredRefuelable(Type b, Func<Building, object> nf, Func<object, Thing, int> test, Action<object, Thing> a) {
                buildingType = b;
                objectThatNeedsFueling = nf;
                fuelTest = test;
                refuelAction = a;
            }
        }
    }
    /// <summary>
    /// Sample way another mod can add cmpatibility for PRF's AutoRefueling machine - it has a 
    ///   robotic arm and can fuel anything with CompRefuelable, but also load mortars. If you
    ///   have something that is basically a vanilla mortar, it will be fine. Adding buildings
    ///   that have their own "refueling" mechanism requires a small amount of work but can be
    ///   done by the other mod's authors - just follow the template/example that adds vanilla
    ///   mortars to the list of things that can be refueled.
    /// </summary>
    [StaticConstructorOnStartup]
    // -- Pick your own name! --
    static class RegisterVanillaMortarsAsRefuelable {
        static RegisterVanillaMortarsAsRefuelable() {
            if (!ModLister.HasActiveModWithName("Project RimFactory Revived")) return;
            // This gets the PRF Building_FuelingMachine from our assembly - it works from any mod's
            //   assembly!
            Type refueler = Type.GetType("ProjectRimFactory.Industry.Building_FuelingMachine, ProjectRimFactory", false);
            // But we still check in case anything went wrong.
            if (refueler == null) {
                // -- Feel free to add your own error message if things fail. --
                Log.Warning("PRF failed to load compatibility for PRF; auto loading mortars won't work");
                return;
            }
            // Call PRF's RegisterRefuelable(Type buildingType, 
            //                               Func<Building, object> getObjectThatNeedsFueling, 
            //                               Func<object, Thing, int> isThisFuelTest,
            //                               Action< object, Thing > refuelAction);
            refueler.GetMethod("RegisterRefuelable", System.Reflection.BindingFlags.Static |
                                                     System.Reflection.BindingFlags.Public).Invoke(null,
                new object[] {
                // ----- pass the type of your Building (we only refuel buildings -----
                typeof(Building_TurretGun),
                // ----- pass a method/delegate that returns `null` if nothing needs refueling,
                //       or the object that needs refueling. Note you may pass static methods -----
                (Func<Building, object>)FindCompNeedsShells,
                // ----- pass a method/delegate that checks if the Thing t can refuel your object c
                //       You must return the number of this Thing you want to refuel your object,
                //       (0 if you do not want any of this as fuel)
                (Func<object, Thing, int>)delegate (object c, Thing t)
                {
                    CompChangeableProjectile comp = c as CompChangeableProjectile;
                    if (comp.allowedShellsSettings.filter.Allows(t)) return 1;
                    return 0;
                },
                // ----- pass a (void) method/action delegate that consumes the Thing t - 
                //       you probably want to Destroy() it? But whatever you need - the
                //       Thing t is completely yours here. It may or may not be spawned -----
                (Action<object, Thing>)delegate (object c, Thing t)
                {
                    (c as CompChangeableProjectile).LoadShell(t.def, 1);
                    t.Destroy();
                }});
        }
        static object FindCompNeedsShells(Building b) {
            var changeableProjectileComp = (b as Building_TurretGun).gun?.TryGetComp<CompChangeableProjectile>();
            if (changeableProjectileComp?.Loaded == false) return changeableProjectileComp;
            return null;
        }
    }
}