using RimWorld;
using System.Collections.Generic;
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
            if (stat == StatDefOf.FoodPoisonChance)
            {
                return 0.0005f;
            }
            return 1.0f;
        }
    }

    // TODO:本体更新時に合わせる.
    static class GenRecipe2
    {
        public static IEnumerable<Thing> MakeRecipeProducts(RecipeDef recipeDef, IRecipeProductWorker worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver, Precept_ThingStyle precept = null)
        {
            var result = MakeRecipeProductsInt(recipeDef, worker, ingredients, dominantIngredient, billGiver, precept);
            return result;
        }

        public static IEnumerable<Thing> MakeRecipeProductsInt(RecipeDef recipeDef, IRecipeProductWorker worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver, Precept_ThingStyle precept = null)
        {
            float efficiency;
            if (recipeDef.efficiencyStat == null)
            {
                efficiency = 1f;
            }
            else
            {
                efficiency = worker.GetStatValue(recipeDef.efficiencyStat, true);
            }
            if (recipeDef.workTableEfficiencyStat != null)
            {
                Building_WorkTable building_WorkTable = billGiver as Building_WorkTable;
                if (building_WorkTable != null)
                {
                    efficiency *= building_WorkTable.GetStatValue(recipeDef.workTableEfficiencyStat, true);
                }
            }
            if (recipeDef.products != null)
            {
                for (int i = 0; i < recipeDef.products.Count; i++)
                {
                    ThingDefCountClass prod = recipeDef.products[i];
                    ThingDef stuffDef;
                    if (prod.thingDef.MadeFromStuff)
                    {
                        stuffDef = dominantIngredient.def;
                    }
                    else
                    {
                        stuffDef = null;
                    }
                    Thing product = ThingMaker.MakeThing(prod.thingDef, stuffDef);
                    product.stackCount = Mathf.CeilToInt((float)prod.count * efficiency);
                    if (dominantIngredient != null)
                    {
                        product.SetColor(dominantIngredient.DrawColor, false);
                    }
                    CompIngredients ingredientsComp = product.TryGetComp<CompIngredients>();
                    if (ingredientsComp != null)
                    {
                        for (int l = 0; l < ingredients.Count; l++)
                        {
                            ingredientsComp.RegisterIngredient(ingredients[l].def);
                        }
                    }
                    CompFoodPoisonable foodPoisonable = product.TryGetComp<CompFoodPoisonable>();
                    if (foodPoisonable != null)
                    {
                        Room room = worker.GetRoom(RegionType.Set_Passable);
                        float chance = (room == null) ? RoomStatDefOf.FoodPoisonChance.roomlessScore : room.GetStat(RoomStatDefOf.FoodPoisonChance);
                        if (Rand.Chance(chance))
                        {
                            foodPoisonable.SetPoisoned(FoodPoisonCause.FilthyKitchen);
                        }
                        else
                        {
                            float statValue = worker.GetStatValue(StatDefOf.FoodPoisonChance, true);
                            if (Rand.Chance(statValue))
                            {
                                foodPoisonable.SetPoisoned(FoodPoisonCause.IncompetentCook);
                            }
                        }
                    }
                    yield return GenRecipe2.PostProcessProduct(product, recipeDef, worker, precept);
                }
            }
            if (recipeDef.specialProducts != null)
            {
                for (int j = 0; j < recipeDef.specialProducts.Count; j++)
                {
                    for (int k = 0; k < ingredients.Count; k++)
                    {
                        Thing ing = ingredients[k];
                        SpecialProductType specialProductType = recipeDef.specialProducts[j];
                        if (specialProductType != SpecialProductType.Butchery)
                        {
                            if (specialProductType == SpecialProductType.Smelted)
                            {
                                foreach (Thing product2 in ing.SmeltProducts(efficiency))
                                {
                                    yield return GenRecipe2.PostProcessProduct(product2, recipeDef, worker, precept);
                                }
                            }
                        }
                        else
                        {
                            foreach (Thing product3 in ButcherProducts(ing, efficiency, worker))
                            {
                                yield return GenRecipe2.PostProcessProduct(product3, recipeDef, worker, precept);
                            }
                        }
                    }
                }
            }
        }

        private static Thing PostProcessProduct(Thing product, RecipeDef recipeDef, IRecipeProductWorker worker, Precept_ThingStyle precept = null)
        {
            CompQuality compQuality = product.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                if (recipeDef.workSkill == null)
                {
                    Log.Error(recipeDef + " needs workSkill because it creates a product with a quality.");
                }
                int level = worker.GetSkillLevel(recipeDef.workSkill);
                QualityCategory qualityCategory = QualityUtility.GenerateQualityCreatedByPawn(level, false);
                compQuality.SetQuality(qualityCategory, ArtGenerationContext.Colony);
            }
            CompArt compArt = product.TryGetComp<CompArt>();
            if (compArt != null)
            {
                if (compQuality.Quality >= QualityCategory.Excellent)
                {
                    /*
                    TaleRecorder.RecordTale(TaleDefOf.CraftedArt, new object[]
                    {
                        product
                    });
                    */
                }
            }
            if (precept != null)
            {
                product.StyleSourcePrecept = precept;
            }
            if (product.def.Minifiable)
            {
                product = product.MakeMinified();
            }
            return product;
        }

        private static IEnumerable<Thing> ButcherProducts(Thing thing, float efficiency, IRecipeProductWorker worker)
        {
            var corpse = thing as Corpse;
            if (corpse != null)
            {
                return ButcherProducts(corpse, efficiency, worker);
            }
            else
            {
                return thing.ButcherProducts(null, efficiency);
            }
        }

        public static IEnumerable<Thing> ButcherProducts(Corpse corpse, float efficiency, IRecipeProductWorker worker)
        {
            foreach (Thing t in corpse.InnerPawn.ButcherProducts(null, efficiency))
            {
                yield return t;
            }
            if (corpse.InnerPawn.RaceProps.BloodDef != null)
            {
                FilthMaker.TryMakeFilth(worker.Position, worker.Map, corpse.InnerPawn.RaceProps.BloodDef, corpse.InnerPawn.LabelIndefinite(), 1);
            }
        }
    }
}
