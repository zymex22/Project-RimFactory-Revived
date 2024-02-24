using ProjectRimFactory.Common;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AnimalStation
{
    public abstract class Building_CompHarvester : Building_Storage, IPowerSupplyMachineHolder
    {
        public static readonly PropertyInfo ResourceAmount = typeof(CompHasGatherableBodyResource).GetProperty("ResourceAmount", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly PropertyInfo ResourceDef = typeof(CompHasGatherableBodyResource).GetProperty("ResourceDef", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo Fullness = typeof(CompHasGatherableBodyResource).GetField("fullness", BindingFlags.NonPublic | BindingFlags.Instance);

        public IEnumerable<IntVec3> ScannerCells
        {
            get
            {
                return this.GetComp<CompPowerWorkSetting>()?.GetRangeCells() ?? GenAdj.OccupiedRect(this).ExpandedBy(1).Cells;
            }
        }

        public IPowerSupplyMachine RangePowerSupplyMachine => this.GetComp<CompPowerWorkSetting>();

        public abstract bool CompValidator(CompHasGatherableBodyResource comp);

        public override void TickRare()
        {
            base.TickRare();
            if (!GetComp<CompPowerTrader>().PowerOn) return;
            foreach (Pawn p in (from c in ScannerCells
                                from p in c.GetThingList(Map).OfType<Pawn>()
                                where p.Faction == Faction.OfPlayer
                                select p).ToList())
            {
                foreach (CompHasGatherableBodyResource comp in (from comp in p.AllComps.OfType<CompHasGatherableBodyResource>()
                                                                where CompValidator(comp)
                                                                select comp).ToList())
                {
                    int amount = GenMath.RoundRandom((int)ResourceAmount.GetValue(comp, null) * comp.Fullness);
                    if (amount != 0)
                    {
                        var resource = (ThingDef)ResourceDef.GetValue(comp, null);
                        while (amount > 0)
                        {
                            int num = Mathf.Clamp(amount, 1, resource.stackLimit);
                            amount -= num;
                            Thing thing = ThingMaker.MakeThing(resource, null);
                            thing.stackCount = num;
                            GenPlace.TryPlaceThing(thing,
                                this.GetComp<CompOutputAdjustable>()?.CurrentCell ?? p.Position,
                                p.Map, ThingPlaceMode.Near, null);
                        }
                        Fullness.SetValue(comp, 0f);
                    }
                }
            }
        }
    }
}
