using Verse;
using RimWorld;

namespace ProjectRimFactory.CultivatorTools
{
    public class Building_Sprinkler : Building_RadialCellIterator
    {
        public override int TickRate => def.GetModExtension<CultivatorDefModExtension>()?.TickFrequencyDivisor ?? 50;

        private int GrowRate => def.GetModExtension<CultivatorDefModExtension>()?.GrowRate ?? 2500;

        public override bool DoIterationWork(IntVec3 c)
        {
            var plant = c.GetPlant(Map);
            if (plant != null && !Map.reservationManager.IsReservedByAnyoneOf(plant, Faction))
            {
                var rate = GetGrowthRatePerTickFor(plant);
                plant.Growth += rate * this.GrowRate;//Growth sped up by 1hr
            }
            return true;
        }
        public float GetGrowthRatePerTickFor(Plant p)
        {
            var num = 1f / (60000f * p.def.plant.growDays);
            return num * p.GrowthRate;
        }
    }
}
