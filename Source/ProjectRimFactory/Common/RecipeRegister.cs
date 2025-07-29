using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.AutoMachineTool;
using ProjectRimFactory.SAL3.Tools;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common;

[StaticConstructorOnStartup]
// ReSharper disable once UnusedType.Global
public static class RecipeRegister
{
    private static readonly List<RecipeDef> RecipeDefs = [];

    private static void PrepareMineables()
    {
        var mineables = DefDatabase<ThingDef>.AllDefs
            .Where(d => d.mineable && d.building is { mineableThing: not null, mineableYield: > 0 })
            .Where(d => d.building.isResourceRock || d.building.isNaturalRock)
            .Select(d => new ThingDefCountClass(d.building.mineableThing, d.building.mineableYield))
            .ToList();
        
        var mineablesSet = new HashSet<ThingDef>();
        mineablesSet.AddRange(mineables.Select(d => d.thingDef));
        

        mineables.AddRange(
            DefDatabase<ThingDef>.AllDefs
                .Where(d => d.deepCommonality > 0f && d.deepCountPerPortion > 0)
                .Where(d => !mineablesSet.Contains(d))
                .Select(d => new ThingDefCountClass(d, d.deepCountPerPortion))
            // this line can be uncommented when the above line is
            //.Where(d => !minerDef.GetModExtension<ModExtension_Miner>()?.IsExcluded(d.thingDef) ?? true)
        );
        
        var recipeDefsAll = mineables.Select(m => CreateMiningRecipe(m,default)).ToList();

        //Check for duplicates
        List<string> recipeDefNames = [];
        for (var i = 0; i < recipeDefsAll.Count; i++)
        {
            if (recipeDefNames.Contains(recipeDefsAll[i].defName)) continue;
            RecipeDefs.Add(recipeDefsAll[i]);
            recipeDefNames.Add(recipeDefsAll[i].defName);
        }
        
        DefDatabase<RecipeDef>.Add(RecipeDefs);
    }
    
    
    static RecipeRegister()
    {
        PrepareMineables();
        
        var minerDefs =
            DefDatabase<ThingDef>.AllDefs.Where(def => def.thingClass == typeof(SAL3.Things.Assemblers.Building_Miner));
        foreach (var def in minerDefs)
        {
            HandleMinerDef(def);
        }
    }

    private static void HandleMinerDef(ThingDef minerDef)
    {
        if (minerDef == null) return;
        
        // TODO Check if we want to still support that
        // var _ = minerDef.GetModExtension<ModExtension_EffectWorking>()?.effectWorking;
        // may require redesign of PrepareMineables


        var modExt = minerDef.GetModExtension<ModExtension_Miner>();
        // These 3 lines remove exluded recipes from available bills:
        var acceptableRecipeDefs = RecipeDefs
            .FindAll(r => !modExt?.IsExcluded(r.products[0].thingDef) ?? true);
        minerDef.recipes = acceptableRecipeDefs;
        ReflectionUtility.AllRecipesCached.SetValue(minerDef, null);

        //change those three lines to this when all recipes are done:
        //minerDef.recipes = recipeDefs;
    }

    private static RecipeDef CreateMiningRecipe(ThingDefCountClass defCount, EffecterDef effecter)
    {
        var r = new RecipeDef
        {
            defName = "Recipe_AutoMachineTool_Mine_" + defCount.thingDef.defName,
            label = "PRF.AutoMachineTool.AutoMiner.MineOre".Translate(defCount.thingDef.label),
            jobString = "PRF.AutoMachineTool.AutoMiner.MineOre".Translate(defCount.thingDef.label),
            workAmount = Mathf.Max(10000f, StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(defCount.thingDef, null)) * defCount.count * 1000),
            workSpeedStat = StatDefOf.WorkToMake,
            efficiencyStat = StatDefOf.WorkToMake,
            workSkill = SkillDefOf.Mining,
            workSkillLearnFactor = 0,
            products = new List<ThingDefCountClass>().Append(defCount),
            defaultIngredientFilter = new ThingFilter(),
            effectWorking = effecter,
            ingredients =  []
        };

        // ChunkStone が Recipe の WorkAmount 経由で価値を設定されてしまうため、BaseMarketValue に0を設定して、計算されないようにする。
        // <see cref="StatWorker_MarketValue.CalculatedBaseMarketValue(BuildableDef, ThingDef)"/>
        if (!defCount.thingDef.statBases.StatListContains(StatDefOf.MarketValue) && defCount.count == 1)
        {
            defCount.thingDef.BaseMarketValue = 0;
        }

        return r;
    }
}