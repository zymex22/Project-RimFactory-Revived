using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ModExtension_Miner : DefModExtension
    {
        public List<ThingDef> excludeOres;

        public bool IsExcluded(ThingDef def)
        {
            return excludeOres?.Contains(def) ?? false;
        }
    }
}
