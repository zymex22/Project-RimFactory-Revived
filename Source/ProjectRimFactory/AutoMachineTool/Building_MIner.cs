using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_Miner : Building_BaseMachine<Building_Miner>, IBillGiver, IRecipeProductWorker, IXMLThingDescription, IBillTab
    {

        public BillStack BillStack => billStack;

        public IEnumerable<IntVec3> IngredientStackCells => [];

        public IEnumerable<RecipeDef> AllRecipes => def.AllRecipes;

        public IEnumerable<RecipeDef> GetAllRecipes()
        {
            return AllRecipes;
        }

        public BillStack billStack;

        public Building_Miner()
        {
            billStack = new BillStack(this);
            ForcePlace = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref billStack, "billStack", new object[] { this });
            Scribe_References.Look(ref workingBill, "workingBill");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && workingBill == null)
            {
                ReadyOnStart = true;
            }
            billStack.Bills.RemoveAll(b => def.GetModExtension<ModExtension_Miner>()?.IsExcluded(b.recipe.ProducedThingDef) ?? false);
        }

        protected override bool WorkInterruption(Building_Miner _)
        {
            return !workingBill.ShouldDoNow();
        }

        private Bill workingBill;

        protected override bool TryStartWorking(out Building_Miner target, out float workAmount)
        {
            target = this;
            workAmount = 0;
            if (!billStack.AnyShouldDoNow) return false;
            workingBill = billStack.FirstShouldDoNow;
            workAmount = workingBill.recipe.workAmount;
            return true;
        }

        protected override bool FinishWorking(Building_Miner _, out List<Thing> outputProducts)
        {
            outputProducts = GenRecipe2.MakeRecipeProducts(workingBill.recipe, this, [], null, this).ToList();
            // Because we use custom GenRecipe2, we have to handle bonus items and product modifications (bonuses) directly:
            def.GetModExtension<ModExtension_ModifyProduct>()?.ProcessProducts(outputProducts,
                                                        this as IBillGiver, this, workingBill.recipe);
            workingBill.Notify_IterationCompleted(null, []);
            workingBill = null;
            return true;
        }

        public bool CurrentlyUsableForBills()
        {
            return false;
        }

        public bool UsableForBillsAfterFueling()
        {
            return false;
        }

        public Room GetRoom(RegionType type)
        {
            return RegionAndRoomQuery.GetRoom(this, type);
        }

        public int GetSkillLevel(SkillDef skillDef)
        {
            return 20;
        }


        [Unsaved]
        private Option<Effecter> workingEffect = Nothing<Effecter>();

        protected override void CleanupWorkingEffect()
        {
            base.CleanupWorkingEffect();

            workingEffect.ForEach(e => e.Cleanup());
            workingEffect = Nothing<Effecter>();

            MapManager.RemoveEachTickAction(EffectTick);

            if (GetComp<CompGlowerPulse>() != null)
            {
                GetComp<CompGlowerPulse>().Glows = false;
            }
        }

        protected override void CreateWorkingEffect()
        {
            base.CreateWorkingEffect();

            workingEffect = workingEffect.Fold(() => Option(workingBill.recipe.effectWorking).Select(e => e.Spawn()))(e => Option(e));

            MapManager.EachTickAction(EffectTick);
            if (GetComp<CompGlowerPulse>() != null)
            {
                GetComp<CompGlowerPulse>().Glows = true;
            }
        }

        protected bool EffectTick()
        {
            // this.workingEffect.ForEach(e => e.EffectTick(new TargetInfo(this.OutputCell(), this.Map), new TargetInfo(this)));
            workingEffect.ForEach(e => e.EffectTick(new TargetInfo(this), new TargetInfo(this)));
            return !workingEffect.HasValue;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (GetComp<CompGlowerPulse>() != null)
            {
                GetComp<CompGlowerPulse>().Glows = false;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }
        }
        
        public override IntVec3 OutputCell()
        {
            return CompOutputAdjustable.CurrentCell;
        }

        public Bill MakeNewBill(RecipeDef recipe)
        {
            return recipe.MakeNewBill();
        }

        public string GetDescription(ThingDef def)
        {
            var helpText = "PRF_DescriptionUpdate_CanMine".Translate();
            foreach (var recipeDef in def.recipes)
            {
                helpText += $"    - {recipeDef.products?[0]?.Label}\r\n";
            }
            helpText += "\r\n";
            return helpText;
        }

        public void Notify_BillDeleted(Bill bill)
        {
        }
    }

    [StaticConstructorOnStartup]
    public static class RecipeRegister
    {
        static RecipeRegister()
        {
            var minerDef = DefDatabase<ThingDef>.GetNamedSilentFail("PRF_BillTypeMiner_I");
            if (minerDef == null) return;
            var effecter = minerDef.GetModExtension<ModExtension_EffectWorking>()?.effectWorking;
            var mineables = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.mineable && d.building != null && d.building.mineableThing != null && d.building.mineableYield > 0)
                .Where(d => d.building.isResourceRock || d.building.isNaturalRock)
                .Select(d => new ThingDefCountClass(d.building.mineableThing, d.building.mineableYield))
                // Create recipes for excluded items - for now - so players who had those recipes
                // don't get errors are save game load.
                // Once people have had this change for a while (Nov 2020?), can uncomment this line
                // and the line below - and the line farther down can be removed
                // If players can continue to add such recipes somehow, do not change these:
                //.Where(d => !minerDef.GetModExtension<ModExtension_Miner>()?.IsExcluded(d.thingDef) ?? true)
                .ToList();

            var mineablesSet = mineables.Select(d => d.thingDef).ToHashSet();

            mineables.AddRange(
                DefDatabase<ThingDef>.AllDefs
                    .Where(d => d.deepCommonality > 0f && d.deepCountPerPortion > 0)
                    .Where(d => !mineablesSet.Contains(d))
                    .Select(d => new ThingDefCountClass(d, d.deepCountPerPortion))
                // this line can be uncommented when the above line is
                //.Where(d => !minerDef.GetModExtension<ModExtension_Miner>()?.IsExcluded(d.thingDef) ?? true)
            );

            var recipeDefs_all = mineables.Select(m => CreateMiningRecipe(m, effecter)).ToList();

            //Check for duplicates
            List<RecipeDef> recipeDefs = [];
            List<String> recipeDefsnames = [];
            for (var i = 0; i < recipeDefs_all.Count; i++)
            {
                if (recipeDefsnames.Contains(recipeDefs_all[i].defName)) continue;
                recipeDefs.Add(recipeDefs_all[i]);
                recipeDefsnames.Add(recipeDefs_all[i].defName);
            }


            DefDatabase<RecipeDef>.Add(recipeDefs);
            // These 3 lines remove exluded recipes from available bills:
            var acceptableRecipeDefs = recipeDefs
                .FindAll(r => !minerDef.GetModExtension<ModExtension_Miner>()?.IsExcluded(r.products[0].thingDef) ?? true);
            minerDef.recipes = acceptableRecipeDefs;
            SAL3.ReflectionUtility.allRecipesCached.SetValue(minerDef, null);

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
                effectWorking = effecter
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
}
