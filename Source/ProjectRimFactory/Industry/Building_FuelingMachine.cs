using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProjectRimFactory.Industry
{
    public class Building_FuelingMachine : Building
    {
        public IntVec3 FuelableCell => Rotation.FacingCell + Position;
        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(10) && GetComp<CompPowerTrader>().PowerOn)
            {
                // Get what we are supposed to refuel:
                //   (only refuel one thing - if you need to adjust this to fuel more
                //    than one thing, make the loop here and put some breaking logic
                //    instead of all the "return;"s below)
                CompRefuelable refuelableComp=null;
                foreach (Thing tmpThing in Map.thingGrid.ThingsListAt(FuelableCell)) {
                    if (tmpThing is Building) refuelableComp=(tmpThing as Building).GetComp<CompRefuelable>();
                    if (refuelableComp != null) break;
                }
                if (refuelableComp != null) {
                    if (refuelableComp.Fuel >= refuelableComp.TargetFuelLevel) return; // fully fueled
                    foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(this))
                    {
                        List<Thing> l = Map.thingGrid.ThingsListAt(cell);
                        for (int i=l.Count-1; i>=0; i--) { // count down because items may be destroyed
                            Thing item=l[i];
                            // Without this check, if there is something that is fueled by
                            //     minified Power Conduits (weird, but ...possible?), then
                            //     our FuelingMachine will happily rip conduits out of the
                            //     ground to fuel it.  I'm okay with this behavior.
                            //     Feature.  Not a bug.
                            // But if it ever causes a problem, uncomment this check:
                            // if (item.def.category != ThingCategory.Item) continue;
                            if (refuelableComp.Props.fuelFilter.Allows(item))
                            {
                                int num = Mathf.Min(item.stackCount, Mathf.CeilToInt(refuelableComp.TargetFuelLevel - refuelableComp.Fuel));
                                if (num > 0)
                                {
                                    refuelableComp.Refuel(num);
                                    item.SplitOff(num).Destroy();
                                }
                                if (refuelableComp.Fuel >= refuelableComp.TargetFuelLevel) return; // fully fueled
                            }
                        }
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
    }
}
