using RimWorld;

namespace ProjectRimFactory.AnimalStation
{
    public class Building_Shearer : Building_CompHarvester
    {
        public override bool CompValidator(CompHasGatherableBodyResource comp)
        {
            return comp is CompShearable;
        }
    }
}
