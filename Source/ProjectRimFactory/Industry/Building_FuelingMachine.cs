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
                foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(this))
                {
                    Thing item = cell.GetFirstItem(Map);
                    if (item != null)
                    {
                        CompRefuelable refuelableComp = FuelableCell.GetFirstBuilding(Map)?.GetComp<CompRefuelable>();
                        if (refuelableComp != null && refuelableComp.Fuel + 1 < refuelableComp.TargetFuelLevel && refuelableComp.Props.fuelFilter.Allows(item))
                        {
                            int num = Mathf.Min(item.stackCount, Mathf.CeilToInt(refuelableComp.TargetFuelLevel - refuelableComp.Fuel));
                            if (num > 0)
                            {
                                refuelableComp.Refuel(num);
                                item.SplitOff(num).Destroy();
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
