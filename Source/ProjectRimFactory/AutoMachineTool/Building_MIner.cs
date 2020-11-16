using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_Miner : Building_BaseMachine<Building_Miner>, IBillGiver, IRecipeProductWorker, ITabBillTable
    {
        private ModExtension_WorkIORange Extension { get { return this.def.GetModExtension<ModExtension_WorkIORange>(); } }

        public BillStack BillStack => this.billStack;

        public IEnumerable<IntVec3> IngredientStackCells => Enumerable.Empty<IntVec3>();

        ThingDef ITabBillTable.def => this.def;

        BillStack ITabBillTable.billStack => this.BillStack;

        public IEnumerable<RecipeDef> AllRecipes => this.def.AllRecipes;

        public IEnumerable<RecipeDef> GetAllRecipes()
        {
            return this.AllRecipes;
        }

        public bool IsRemovable(RecipeDef recipe)
        {
            return false;
        }

        public void RemoveRecipe(RecipeDef recipe)
        {
        }

        public BillStack billStack;

        public Building_Miner()
        {
            this.billStack = new BillStack(this);
            base.forcePlace = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.outputIndex, "outputIndex");
            Scribe_Deep.Look(ref this.billStack, "billStack", new object[] { this });
            Scribe_References.Look(ref this.workingBill, "workingBill");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.workingBill == null)
            {
                this.readyOnStart = true;
            }
            this.billStack.Bills.RemoveAll(b => this.def.GetModExtension<ModExtension_Miner>()?.IsExcluded(b.recipe.ProducedThingDef) ?? false);
        }

        protected override bool WorkInterruption(Building_Miner working)
        {
            return !this.workingBill.ShouldDoNow();
        }

        private Bill workingBill;

        protected override bool TryStartWorking(out Building_Miner target, out float workAmount)
        {
            target = this;
            workAmount = 0;
            if (this.billStack.AnyShouldDoNow)
            {
                this.workingBill = this.billStack.FirstShouldDoNow;
                workAmount = this.workingBill.recipe.workAmount;
                return true;
            }
            return false;
        }

        protected override bool FinishWorking(Building_Miner working, out List<Thing> products)
        {
            var bonus = this.def.GetModExtension<ModExtension_BonusYield>()?.GetBonusYield(this.workingBill.recipe);
            if (bonus == null)
            {
                products = GenRecipe2.MakeRecipeProducts(this.workingBill.recipe, this, new List<Thing>(), null, this).ToList();
            }
            else
            {
                products = new List<Thing>().Append(bonus);
            }
            this.workingBill.Notify_IterationCompleted(null, new List<Thing>());
            this.workingBill = null;
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

        public int GetSkillLevel(SkillDef def)
        {
            return 20;
        }


        [Unsaved]
        private Option<Effecter> workingEffect = Nothing<Effecter>();

        protected override void CleanupWorkingEffect()
        {
            base.CleanupWorkingEffect();

            this.workingEffect.ForEach(e => e.Cleanup());
            this.workingEffect = Nothing<Effecter>();

            MapManager.RemoveEachTickAction(this.EffectTick);

            if (this.GetComp<CompGlowerPulse>() != null)
            {
                this.GetComp<CompGlowerPulse>().Glows = false;
            }
        }

        protected override void CreateWorkingEffect()
        {
            base.CreateWorkingEffect();

            this.workingEffect = this.workingEffect.Fold(() => Option(this.workingBill.recipe.effectWorking).Select(e => e.Spawn()))(e => Option(e));

            MapManager.EachTickAction(this.EffectTick);
            if (this.GetComp<CompGlowerPulse>() != null)
            {
                this.GetComp<CompGlowerPulse>().Glows = true;
            }
        }

        protected bool EffectTick()
        {
            // this.workingEffect.ForEach(e => e.EffectTick(new TargetInfo(this.OutputCell(), this.Map), new TargetInfo(this)));
            this.workingEffect.ForEach(e => e.EffectTick(new TargetInfo(this), new TargetInfo(this)));
            return !this.workingEffect.HasValue;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.adjacent = GenAdj.CellsAdjacent8Way(this).ToArray();

            if (this.GetComp<CompGlowerPulse>() != null)
            {
                this.GetComp<CompGlowerPulse>().Glows = false;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            var direction = new Command_Action();
            direction.action = () =>
            {
                if (this.outputIndex + 1 >= this.adjacent.Count())
                {
                    this.outputIndex = 0;
                }
                else
                {
                    this.outputIndex++;
                }
            };
            direction.activateSound = SoundDefOf.Checkbox_TurnedOn;
            direction.defaultLabel = "PRF.AutoMachineTool.SelectOutputDirectionLabel".Translate();
            direction.defaultDesc = "PRF.AutoMachineTool.SelectOutputDirectionDesc".Translate();
            direction.icon = RS.OutputDirectionIcon;
            yield return direction;
        }

        private int outputIndex = 0;

        private IntVec3[] adjacent;

        public override IntVec3 OutputCell()
        {
            return this.adjacent[this.outputIndex];
        }

        public Bill MakeNewBill(RecipeDef recipe)
        {
            return recipe.MakeNewBill();
        }
    }

    [StaticConstructorOnStartup]
    public static class RecipeRegister
    {
        static RecipeRegister()
        {
            var minerDef = DefDatabase<ThingDef>.GetNamedSilentFail("Building_AutoMachineTool_Miner");
            if (minerDef != null)
            {
                var effecter = minerDef.GetModExtension<ModExtension_EffectWorking>()?.effectWorking;
                var mineables = DefDatabase<ThingDef>.AllDefs
                    .Where(d => d.mineable && d.building != null && d.building.mineableThing != null && d.building.mineableYield > 0)
                    .Where(d => d.building.isResourceRock || d.building.isNaturalRock)
                    .Select(d => new ThingDefCountClass(d.building.mineableThing, d.building.mineableYield))
                    // Create recipes for exluded items - for now - so players who had those recipes
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

                var recipeDefs = mineables.Select(m => CreateMiningRecipe(m, effecter)).ToList();                
                DefDatabase<RecipeDef>.Add(recipeDefs);
                // These 3 lines remove exluded recipes from available bills:
                var acceptableRecipeDefs=recipeDefs
                    .FindAll(r => !minerDef.GetModExtension<ModExtension_Miner>()?.IsExcluded(r.products[0].thingDef) ?? true);
                minerDef.recipes = acceptableRecipeDefs;
                //change those three lines to this when all recipes are done:
                //minerDef.recipes = recipeDefs;
            }
        }

        private static RecipeDef CreateMiningRecipe(ThingDefCountClass defCount, EffecterDef effecter)
        {
            RecipeDef r = new RecipeDef();
            r.defName = "Recipe_AutoMachineTool_Mine_" + defCount.thingDef.defName;
            r.label = "PRF.AutoMachineTool.AutoMiner.MineOre".Translate(defCount.thingDef.label);
            r.jobString = "PRF.AutoMachineTool.AutoMiner.MineOre".Translate(defCount.thingDef.label);

            r.workAmount = Mathf.Max(10000f, StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(defCount.thingDef, null)) * defCount.count * 1000);
            r.workSpeedStat = StatDefOf.WorkToMake;
            r.efficiencyStat = StatDefOf.WorkToMake;

            r.workSkill = SkillDefOf.Mining;
            r.workSkillLearnFactor = 0;

            r.products = new List<ThingDefCountClass>().Append(defCount);
            r.defaultIngredientFilter = new ThingFilter();

            r.effectWorking = effecter;

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
