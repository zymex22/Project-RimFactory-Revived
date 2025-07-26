using System.Reflection;
using RimWorld;
using Verse;

namespace ProjectRimFactory.SAL3.Tools
{
    public static class ReflectionUtility
    {
        public static readonly FieldInfo MapIndexOrState = typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly FieldInfo CachedTotallyDisabled = typeof(SkillRecord).GetField("cachedTotallyDisabled", BindingFlags.NonPublic | BindingFlags.Instance);
        // RimWorld.WorkGiver_DoBill's static TryFindBestBillIngredientsInSet: expects a list of (valid) available ingredients for a bill, 
        //   fills a list with chosen ingredients for that bill if it returns true
        public static readonly MethodInfo TryFindBestBillIngredientsInSet = typeof(WorkGiver_DoBill).GetMethod("TryFindBestBillIngredientsInSet", BindingFlags.NonPublic | BindingFlags.Static);
        //For SAL Deep Drill Support
        public static readonly FieldInfo DrillPortionProgress = typeof(CompDeepDrill).GetField("portionProgress", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo DrillPortionYieldPct = typeof(CompDeepDrill).GetField("portionYieldPct", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo DrillLastUsedTick = typeof(CompDeepDrill).GetField("lastUsedTick", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly MethodInfo DrillTryProducePortion = typeof(CompDeepDrill).GetMethod("TryProducePortion", BindingFlags.NonPublic | BindingFlags.Instance);

        //BackCompatibility
        public static readonly FieldInfo BackCompatibilityConversionChain = typeof(BackCompatibility).GetField("conversionChain", BindingFlags.NonPublic | BindingFlags.Static);

        public static readonly FieldInfo ResearchManagerProgress = typeof(ResearchManager).GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AllRecipesCached = typeof(ThingDef).GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic);

        //reservations
        public static readonly FieldInfo SalReservations = typeof(Verse.AI.ReservationManager).GetField("reservations", BindingFlags.NonPublic | BindingFlags.Instance);

        //basePowerConsumption
        public static readonly FieldInfo CompPropertiesPowerBasePowerConsumption = typeof(CompProperties_Power).GetField("basePowerConsumption", BindingFlags.Instance | BindingFlags.NonPublic);
        
        public static readonly FieldInfo StockGeneratorSingleDefThingDef = typeof(StockGenerator_SingleDef).GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
