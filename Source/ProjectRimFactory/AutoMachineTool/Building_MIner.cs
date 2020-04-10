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
            products = GenRecipe2.MakeRecipeProducts(this.workingBill.recipe, this, new List<Thing>(), null, this).ToList();
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
        }

        protected override void CreateWorkingEffect()
        {
            base.CreateWorkingEffect();

            this.workingEffect = this.workingEffect.Fold(() => Option(this.workingBill.recipe.effectWorking).Select(e => e.Spawn()))(e => Option(e));

            MapManager.EachTickAction(this.EffectTick);
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
                var mineables = DefDatabase<ThingDef>.AllDefs
                    .Where(d => d.mineable && d.building != null && d.building.mineableThing != null && d.building.mineableYield > 0)
                    .Where(d => d.building.isResourceRock || d.building.isNaturalRock)
                    .Select(d => new ThingDefCountClass(d.building.mineableThing, d.building.mineableYield))
                    .ToList();

                var mineablesSet = mineables.Select(d => d.thingDef).ToHashSet();

                mineables.AddRange(
                    DefDatabase<ThingDef>.AllDefs
                        .Where(d => d.deepCommonality > 0f && d.deepCountPerPortion > 0)
                        .Where(d => !mineablesSet.Contains(d))
                        .Select(d => new ThingDefCountClass(d, d.deepCountPerPortion)));

                var recipeDefs = mineables.Select(CreateMiningRecipe).ToList();

                DefDatabase<RecipeDef>.Add(recipeDefs);
                minerDef.recipes = recipeDefs;
            }
        }

        private static RecipeDef CreateMiningRecipe(ThingDefCountClass defCount)
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

            r.effectWorking = EffecterDefOf.Drill;

            return r;
        }
    }

    public class Building_MinerOutputCellResolver : OutputCellResolver
    {
        public override Option<IntVec3> OutputCell(IntVec3 cell, Map map, Rot4 rot)
        {
            return cell.GetThingList(map)
                .SelectMany(b => Option(b as Building_Miner))
                .FirstOption()
                .Select(b => b.OutputCell());
        }
    }
}
