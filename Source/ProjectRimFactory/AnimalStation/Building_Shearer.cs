using RimWorld;

namespace ProjectRimFactory.AnimalStation
{
    // ReSharper disable once UnusedType.Global
    public class Building_Shearer : Building_CompHarvester
    {
        protected override bool CompValidator(CompHasGatherableBodyResource comp)
        {
            return comp is CompShearable;
        }
    }
}
