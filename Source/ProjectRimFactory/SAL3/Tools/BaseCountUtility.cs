using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3
{
    public static class BaseCountUtility
    {
        public static float CalculateBaseCountFinalised(Thing item, IngredientCount ingredient)
        {
            float basecount = item.stackCount;
            if (ShouldUseNutritionMath(item, ingredient))
            {
                basecount *= item.GetStatValue(StatDefOf.Nutrition);
            }
            if (item.def.smallVolume)
            {
                basecount *= 0.05f;
            }
            return basecount;
        }
        public static bool ShouldUseNutritionMath(Thing t, IngredientCount ingredient)
        {
            return t.GetStatValue(StatDefOf.Nutrition) > 0f && !(t is Corpse) && IngredientFilterHasNutrition(ingredient.filter);
        }

        public static bool IngredientFilterHasNutrition(ThingFilter filter)
        {
            if (filter != null)
            {
                bool isNutrition(string str) => str == "Foods" || str == "PlantMatter";
                var field = typeof(ThingFilter).GetField("categories", BindingFlags.NonPublic | BindingFlags.Instance);
                var categories = (List<string>)(field.GetValue(filter) ?? new List<string>());
                foreach (string s in categories)
                {
                    if (DefDatabase<ThingCategoryDef>.GetNamed(s).Parents.Select(t => t.defName).Any(isNutrition) || isNutrition(s)) return true;
                }
            }
            return false;
        }
        public static int CalculateIngredientIntFinalised(Thing item, IngredientCount ingredient)
        {
            float basecount = ingredient.GetBaseCount();
            if (ShouldUseNutritionMath(item, ingredient))
            {
                basecount /= item.GetStatValue(StatDefOf.Nutrition);
            }
            if (item.def.smallVolume)
            {
                basecount /= 0.05f;
            }
            return Mathf.RoundToInt(basecount);
        }
    }
}
