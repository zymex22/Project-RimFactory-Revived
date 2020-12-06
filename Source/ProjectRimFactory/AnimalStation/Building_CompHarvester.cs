using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AnimalStation
{
    public abstract class Building_CompHarvester : Building_Storage, IPowerSupplyMachineHolder
    {
        public static readonly PropertyInfo ResourceAmount =
            typeof(CompHasGatherableBodyResource).GetProperty("ResourceAmount",
                BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly PropertyInfo ResourceDef =
            typeof(CompHasGatherableBodyResource).GetProperty("ResourceDef",
                BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly FieldInfo Fullness =
            typeof(CompHasGatherableBodyResource).GetField("fullness", BindingFlags.NonPublic | BindingFlags.Instance);

        public IEnumerable<IntVec3> ScannerCells => GetComp<CompPowerWorkSetting>()?.GetRangeCells() ??
                                                    this.OccupiedRect().ExpandedBy(1).Cells;

        public IPowerSupplyMachine RangePowerSupplyMachine => GetComp<CompPowerWorkSetting>();

        public abstract bool CompValidator(CompHasGatherableBodyResource comp);

        public override void TickRare()
        {
            base.TickRare();
            if (!GetComp<CompPowerTrader>().PowerOn) return;
            foreach (var p in (from c in ScannerCells
                from p in c.GetThingList(Map).OfType<Pawn>()
                where p.Faction == Faction.OfPlayer
                select p).ToList())
            foreach (var comp in (from comp in p.AllComps.OfType<CompHasGatherableBodyResource>()
                where CompValidator(comp)
                select comp).ToList())
            {
                var amount = GenMath.RoundRandom((int) ResourceAmount.GetValue(comp, null) * comp.Fullness);
                if (amount != 0)
                {
                    var resource = (ThingDef) ResourceDef.GetValue(comp, null);
                    while (amount > 0)
                    {
                        var num = Mathf.Clamp(amount, 1, resource.stackLimit);
                        amount -= num;
                        var thing = ThingMaker.MakeThing(resource);
                        thing.stackCount = num;
                        GenPlace.TryPlaceThing(thing,
                            GetComp<CompOutputAdjustable>()?.CurrentCell ?? p.Position,
                            p.Map, ThingPlaceMode.Near);
                    }

                    Fullness.SetValue(comp, 0f);
                }
            }
        }
    }
}