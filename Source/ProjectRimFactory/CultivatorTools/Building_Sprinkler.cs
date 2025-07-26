using RimWorld;
using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    // ReSharper disable once UnusedType.Global
    public class Building_Sprinkler : Building_RadialCellIterator
    {
        protected override int TickRate => CultivatorDefModExtension?.TickFrequencyDivisor ?? 50;

        private int GrowRate => CultivatorDefModExtension?.GrowRate ?? 2500;

        protected override bool DoIterationWork(IntVec3 cell)
        {
            var plant = cell.GetPlant(Map);
            if (plant == null || Map.reservationManager.IsReservedByAnyoneOf(plant, Faction)) return true;
            var rate = GetGrowthRatePerTickFor(plant);
            plant.Growth += rate * GrowRate; //Growth sped up by 1hr
            return true;
        }

        private static float GetGrowthRatePerTickFor(Plant p)
        {
            var num = 1f / (60000f * p.def.plant.growDays);
            return num * p.GrowthRate;
        }
    }
}
