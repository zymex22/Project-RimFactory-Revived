using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public interface IRecipeProductWorker
    {
        Map Map { get; }
        IntVec3 Position { get; }
        Room GetRoom(RegionType type);
        int GetSkillLevel(SkillDef def);
    }

    public static class IRecipeProductWorkerExtension
    {
        public static float GetStatValue(this IRecipeProductWorker maker, StatDef stat, bool applyPostProcess = true)
        {
            if (stat == StatDefOf.FoodPoisonChance) return 0.0005f;
            return 1.0f;
        }
    }

    // TODO:本体更新時に合わせる.
    internal static class GenRecipe2
    {
        public static IEnumerable<Thing> MakeRecipeProducts(RecipeDef recipeDef, IRecipeProductWorker worker,
            List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver)
        {
            var result = MakeRecipeProductsInt(recipeDef, worker, ingredients, dominantIngredient, billGiver);
            return result;
        }

        public static IEnumerable<Thing> MakeRecipeProductsInt(RecipeDef recipeDef, IRecipeProductWorker worker,
            List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver)
        {
            float efficiency;
            if (recipeDef.efficiencyStat == null)
                efficiency = 1f;
            else
                efficiency = worker.GetStatValue(recipeDef.efficiencyStat);
            if (recipeDef.workTableEfficiencyStat != null)
            {
                var building_WorkTable = billGiver as Building_WorkTable;
                if (building_WorkTable != null)
                    efficiency *= building_WorkTable.GetStatValue(recipeDef.workTableEfficiencyStat);
            }

            if (recipeDef.products != null)
                for (var i = 0; i < recipeDef.products.Count; i++)
                {
                    var prod = recipeDef.products[i];
                    ThingDef stuffDef;
                    if (prod.thingDef.MadeFromStuff)
                        stuffDef = dominantIngredient.def;
                    else
                        stuffDef = null;
                    var product = ThingMaker.MakeThing(prod.thingDef, stuffDef);
                    product.stackCount = Mathf.CeilToInt(prod.count * efficiency);
                    if (dominantIngredient != null) product.SetColor(dominantIngredient.DrawColor, false);
                    var ingredientsComp = product.TryGetComp<CompIngredients>();
                    if (ingredientsComp != null)
                        for (var l = 0; l < ingredients.Count; l++)
                            ingredientsComp.RegisterIngredient(ingredients[l].def);
                    var foodPoisonable = product.TryGetComp<CompFoodPoisonable>();
                    if (foodPoisonable != null)
                    {
                        var room = worker.GetRoom(RegionType.Set_Passable);
                        var chance = room == null
                            ? RoomStatDefOf.FoodPoisonChance.roomlessScore
                            : room.GetStat(RoomStatDefOf.FoodPoisonChance);
                        if (Rand.Chance(chance))
                        {
                            foodPoisonable.SetPoisoned(FoodPoisonCause.FilthyKitchen);
                        }
                        else
                        {
                            var statValue = worker.GetStatValue(StatDefOf.FoodPoisonChance);
                            if (Rand.Chance(statValue)) foodPoisonable.SetPoisoned(FoodPoisonCause.IncompetentCook);
                        }
                    }

                    yield return PostProcessProduct(product, recipeDef, worker);
                }

            if (recipeDef.specialProducts != null)
                for (var j = 0; j < recipeDef.specialProducts.Count; j++)
                for (var k = 0; k < ingredients.Count; k++)
                {
                    var ing = ingredients[k];
                    var specialProductType = recipeDef.specialProducts[j];
                    if (specialProductType != SpecialProductType.Butchery)
                    {
                        if (specialProductType == SpecialProductType.Smelted)
                            foreach (var product2 in ing.SmeltProducts(efficiency))
                                yield return PostProcessProduct(product2, recipeDef, worker);
                    }
                    else
                    {
                        foreach (var product3 in ButcherProducts(ing, efficiency, worker))
                            yield return PostProcessProduct(product3, recipeDef, worker);
                    }
                }
        }

        private static Thing PostProcessProduct(Thing product, RecipeDef recipeDef, IRecipeProductWorker worker)
        {
            var compQuality = product.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                if (recipeDef.workSkill == null)
                    Log.Error(recipeDef + " needs workSkill because it creates a product with a quality.");
                var level = worker.GetSkillLevel(recipeDef.workSkill);
                var qualityCategory = QualityUtility.GenerateQualityCreatedByPawn(level, false);
                compQuality.SetQuality(qualityCategory, ArtGenerationContext.Colony);
            }

            var compArt = product.TryGetComp<CompArt>();
            if (compArt != null)
                if (compQuality.Quality >= QualityCategory.Excellent)
                {
                    /*
                    TaleRecorder.RecordTale(TaleDefOf.CraftedArt, new object[]
                    {
                        product
                    });
                    */
                }

            if (product.def.Minifiable) product = product.MakeMinified();
            return product;
        }

        private static IEnumerable<Thing> ButcherProducts(Thing thing, float efficiency, IRecipeProductWorker worker)
        {
            var corpse = thing as Corpse;
            if (corpse != null)
                return ButcherProducts(corpse, efficiency, worker);
            return thing.ButcherProducts(null, efficiency);
        }

        public static IEnumerable<Thing> ButcherProducts(Corpse corpse, float efficiency, IRecipeProductWorker worker)
        {
            foreach (var t in corpse.InnerPawn.ButcherProducts(null, efficiency)) yield return t;
            if (corpse.InnerPawn.RaceProps.BloodDef != null)
                FilthMaker.TryMakeFilth(worker.Position, worker.Map, corpse.InnerPawn.RaceProps.BloodDef,
                    corpse.InnerPawn.LabelIndefinite());
        }
    }
}