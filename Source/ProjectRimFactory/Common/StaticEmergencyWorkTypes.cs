using System.Linq;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common;

[StaticConstructorOnStartup]
public static class StaticEmergencyWorkTypes
{
    public static readonly WorkTypeDef[] EmergencyWorkTypes;
    static StaticEmergencyWorkTypes()
    {
        EmergencyWorkTypes = DefDatabase<WorkGiverDef>.AllDefsListForReading
            .Where(def => def.emergency).Select(def => def.workType).Distinct().ToArray();
    }
}