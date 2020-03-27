using RimWorld;

namespace ProjectRimFactory.AnimalStation
{
    public class Building_GenericBodyResourceGatherer : Building_CompHarvester
    {
        public override bool CompValidator(CompHasGatherableBodyResource comp)
        {
            return true;
        }
    }
}
