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
        private static readonly PropertyInfo ResourceAmount = typeof(CompHasGatherableBodyResource).GetProperty("ResourceAmount", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly PropertyInfo ResourceDef = typeof(CompHasGatherableBodyResource).GetProperty("ResourceDef", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo Fullness = typeof(CompHasGatherableBodyResource).GetField("fullness", BindingFlags.NonPublic | BindingFlags.Instance);
        
        private CompPowerTrader compPowerTrader;
        private CompOutputAdjustable compOutputAdjustable;
        private CompPowerWorkSetting compPowerWorkSetting;

        private IntVec3 OutputCell(Pawn pawn)
        {
            return compOutputAdjustable?.CurrentCell ?? pawn.Position;
        }
        
        private IEnumerable<IntVec3> ScannerCells => compPowerWorkSetting?.GetRangeCells() ?? this.OccupiedRect().ExpandedBy(1).Cells;

        public IPowerSupplyMachine RangePowerSupplyMachine => GetComp<CompPowerWorkSetting>();

        protected abstract bool CompValidator(CompHasGatherableBodyResource comp);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPowerTrader = GetComp<CompPowerTrader>();
            compOutputAdjustable  = GetComp<CompOutputAdjustable>();
            compPowerWorkSetting  = GetComp<CompPowerWorkSetting>();
        }

        public override void TickRare()
        {
            base.TickRare();
            if (!Spawned) return;
            if (!compPowerTrader.PowerOn) return;
            foreach (var pawn in (from cell in ScannerCells
                                from pawn in cell.GetThingList(Map).OfType<Pawn>()
                                where pawn.Faction == Faction.OfPlayer
                                select pawn).ToList())
            {
                foreach (var comp in (from comp in pawn.AllComps.OfType<CompHasGatherableBodyResource>()
                                                                where CompValidator(comp)
                                                                select comp).ToList())
                {
                    var amount = GenMath.RoundRandom((int)ResourceAmount.GetValue(comp, null) * comp.Fullness);
                    if (amount == 0) continue;
                    var resource = (ThingDef)ResourceDef.GetValue(comp, null);
                    while (amount > 0)
                    {
                        var num = Mathf.Clamp(amount, 1, resource.stackLimit);
                        amount -= num;
                        var thing = ThingMaker.MakeThing(resource);
                        thing.stackCount = num;
                        GenPlace.TryPlaceThing(thing, OutputCell(pawn), pawn.Map, ThingPlaceMode.Near);
                    }
                    Fullness.SetValue(comp, 0f);
                }
            }
        }
    }
}
