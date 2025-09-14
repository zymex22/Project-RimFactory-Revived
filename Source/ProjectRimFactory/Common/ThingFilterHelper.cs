using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Common;

/// <summary>
/// Thing Filter work a bit intuitively
/// For the Effective Use you need a ParentFilter and the actual Filter
/// The ParentFilter controls what the use can see and select (Allowed => Visible to the user; Disallowed => Hidden form the user)
/// The Actual filter is that what the use can interact with
/// </summary>
[StaticConstructorOnStartup]
public static class ThingFilterHelper
{
    
    public static readonly ThingFilter FilterConfigParentNoAnimalNoPlants;
    
    /// <summary>
    /// Init Parent Filter(s) for custom use
    /// </summary>
    static ThingFilterHelper()
    {
        FilterConfigParentNoAnimalNoPlants = new ThingFilter();
        FilterConfigParentNoAnimalNoPlants.SetAllowAll(null);
        List<string> disallowedCategories = ["Animals", "Plants"];
        foreach (var category in disallowedCategories)
        {
            var named2 = DefDatabase<ThingCategoryDef>.GetNamed(category, errorOnFail: false);
            if (named2 == null) continue;
            FilterConfigParentNoAnimalNoPlants.SetAllow(named2, allow: false);
        }
        
    }
}