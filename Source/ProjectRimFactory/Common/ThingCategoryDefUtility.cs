using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Common
{
    public static class ThingCategoryDefUtility
    {
        public static IEnumerable<ThingCategoryDef> ThisAndParents(this ThingCategoryDef cat)
        {
            yield return cat;
            foreach (var def in cat.Parents) yield return def;
        }
    }
}