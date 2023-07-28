﻿using RimWorld;
using System.Reflection;
using Verse;


namespace ProjectRimFactory.SAL3
{
    public static class ReflectionUtility
    {
        public static readonly FieldInfo mapIndexOrState = typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly FieldInfo cachedTotallyDisabled = typeof(SkillRecord).GetField("cachedTotallyDisabled", BindingFlags.NonPublic | BindingFlags.Instance);
        // RimWorld.WorkGier_DoBill's static TryFindBestBillIngredientsInSet: expects a list of (valid) available ingredients for a bill, 
        //   fills a list of chosen ingredients for that bill if it returns true
        public static readonly MethodInfo TryFindBestBillIngredientsInSet = typeof(WorkGiver_DoBill).GetMethod("TryFindBestBillIngredientsInSet", BindingFlags.NonPublic | BindingFlags.Static);
        //For SAL Deep Drill Support
        public static readonly FieldInfo drill_portionProgress = typeof(CompDeepDrill).GetField("portionProgress", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo drill_portionYieldPct = typeof(CompDeepDrill).GetField("portionYieldPct", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo drill_lastUsedTick = typeof(CompDeepDrill).GetField("lastUsedTick", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly MethodInfo drill_TryProducePortion = typeof(CompDeepDrill).GetMethod("TryProducePortion", BindingFlags.NonPublic | BindingFlags.Instance);

        //BackCompatibility
        public static readonly FieldInfo BackCompatibility_conversionChain = typeof(BackCompatibility).GetField("conversionChain", BindingFlags.NonPublic | BindingFlags.Static);

        public static readonly FieldInfo ResearchManager_progress = typeof(ResearchManager).GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo allRecipesCached = typeof(ThingDef).GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic);

        //reservations
        public static readonly FieldInfo sal_reservations = typeof(Verse.AI.ReservationManager).GetField("reservations", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo sal_reservations2 = typeof(Verse.AI.ReservationManager).GetField("reservations", BindingFlags.NonPublic | BindingFlags.Static);

        //basePowerConsumption
        public static readonly FieldInfo CompProperties_Power_basePowerConsumption = typeof(CompProperties_Power).GetField("basePowerConsumption", BindingFlags.Instance | BindingFlags.NonPublic);

        //Toils Advanced port
        public static readonly FieldInfo JobDriver_toils = typeof(Verse.AI.JobDriver).GetField("toils", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
