using RimWorld;

namespace ProjectRimFactory.AnimalStation
{
    // ReSharper disable once UnusedType.Global
    public class Building_GenericBodyResourceGatherer : Building_CompHarvester
    {
        protected override bool CompValidator(CompHasGatherableBodyResource comp)
        {
            return true;
        }
    }
}
