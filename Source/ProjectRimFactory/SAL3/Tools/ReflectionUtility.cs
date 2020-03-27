using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace ProjectRimFactory.SAL3
{
    public static class ReflectionUtility
    {
        public static readonly FieldInfo mapIndexOrState = typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance);
        //public static readonly FieldInfo cachedDisabledWorkTypes = typeof(Pawn_StoryTracker).GetField("cachedDisabledWorkTypes", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo cachedTotallyDisabled = typeof(SkillRecord).GetField("cachedTotallyDisabled", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo ingredientsOrdered = typeof(WorkGiver_DoBill).GetField("ingredientsOrdered", BindingFlags.NonPublic | BindingFlags.Static);

        public static readonly MethodInfo TryFindBestBillIngredientsInSet = typeof(WorkGiver_DoBill).GetMethod("TryFindBestBillIngredientsInSet", BindingFlags.NonPublic | BindingFlags.Static);
        public static readonly MethodInfo MakeIngredientsListInProcessingOrder = typeof(WorkGiver_DoBill).GetMethod("MakeIngredientsListInProcessingOrder", BindingFlags.NonPublic | BindingFlags.Static);
    }
}
