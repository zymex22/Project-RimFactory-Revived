using Verse;

namespace ProjectRimFactory.Common
{
    interface ILimitWatcher
    {
        public bool ItemIsLimit(ThingDef thing,bool CntStacks, int limit);

    }
}
