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
using ProjectRimFactory.SAL3;



namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_AutoMachineTool : Building_BaseRange<Building_AutoMachineTool>, IRecipeProductWorker
    {
        public interface IBill_PawnForbidded
        {
            Bill Original { get; set; }
        }

        public class Bill_ProductionPawnForbidded : Bill_Production, IBill_PawnForbidded
        {
            public Bill_ProductionPawnForbidded() : base()
            {
            }

            public Bill_ProductionPawnForbidded(RecipeDef recipe) : base(recipe)
            {
            }

            public override bool PawnAllowedToStartAnew(Pawn p)
            {
                return false;
            }

            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Deep.Look(ref this.original, "original");
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    this.original.billStack = this.billStack;
                }
            }

            public Bill original;

            public Bill Original { get => this.original; set => this.original = value; }

            public override Bill Clone()
            {
                var clone = (Bill_Production)this.original.Clone();
                return this.CopyTo(clone);
            }

            public override void Notify_DoBillStarted(Pawn billDoer)
            {
                base.Notify_DoBillStarted(billDoer);
                Option(this.original).ForEach(o => o.Notify_DoBillStarted(billDoer));
            }

            public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
            {
                base.Notify_IterationCompleted(billDoer, ingredients);
                Option(this.original).ForEach(o => o.Notify_IterationCompleted(billDoer, ingredients));
            }

            public override void Notify_PawnDidWork(Pawn p)
            {
                base.Notify_PawnDidWork(p);
                Option(this.original).ForEach(o => o.Notify_PawnDidWork(p));
            }

            // proxy call. override other properties and methods.
        }

        public class Bill_ProductionWithUftPawnForbidded : Bill_ProductionWithUft, IBill_PawnForbidded
        {
            public Bill_ProductionWithUftPawnForbidded() : base()
            {
            }

            public Bill_ProductionWithUftPawnForbidded(RecipeDef recipe) : base(recipe)
            {
            }

            public override bool PawnAllowedToStartAnew(Pawn p)
            {
                return false;
            }

            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Deep.Look(ref this.original, "original");
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    this.original.billStack = this.billStack;
                }
            }

            public Bill original;

            public Bill Original { get => this.original; set => this.original = value; }

            public override Bill Clone()
            {
                var clone = (Bill_ProductionWithUft)this.original.Clone();
                return this.CopyTo(clone);
            }

            public override void Notify_DoBillStarted(Pawn billDoer)
            {
                base.Notify_DoBillStarted(billDoer);
                Option(this.original).ForEach(o => o.Notify_DoBillStarted(billDoer));
            }

            public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
            {
                base.Notify_IterationCompleted(billDoer, ingredients);
                Option(this.original).ForEach(o => o.Notify_IterationCompleted(billDoer, ingredients));
            }

            public override void Notify_PawnDidWork(Pawn p)
            {
                base.Notify_PawnDidWork(p);
                Option(this.original).ForEach(o => o.Notify_PawnDidWork(p));
            }

            // proxy call. override other properties and methods.
        }

        public Building_AutoMachineTool()
        {
            this.forcePlace = false;
            this.targetEnumrationCount = 0;
        }

        private Bill bill;
        private List<Thing> ingredients;
        private Thing dominant;
        private UnfinishedThing unfinished;
        
        private bool forbidItem = false;

        [Unsaved]
        private Effecter workingEffect = null;
        [Unsaved]
        private Sustainer workingSound = null;
        [Unsaved]
        //private Option<Building_WorkTable> workTable;

        private Building_WorkTable my_workTable = null;
        private Building drilltypeBuilding = null;
        private Building_ResearchBench researchBench = null;


        ModExtension_Skills extension_Skills;

        public ModExtension_ModifyProduct ModifyProductExt => this.def.GetModExtension<ModExtension_ModifyProduct>();

        public int GetSkillLevel(SkillDef def)
        {
            return extension_Skills?.GetExtendedSkillLevel(def,typeof(Building_AutoMachineTool)) ?? this.SkillLevel ?? 0;
        }

        protected override int? SkillLevel { get { return this.def.GetModExtension<ModExtension_Tier>()?.skillLevel; } }

        public override bool Glowable => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.forbidItem, "forbidItem");

            Scribe_Deep.Look<UnfinishedThing>(ref this.unfinished, "unfinished");

            Scribe_References.Look<Bill>(ref this.bill, "bill");
            Scribe_References.Look<Thing>(ref this.dominant, "dominant");
            Scribe_Collections.Look<Thing>(ref this.ingredients, "ingredients", LookMode.Deep);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //this.workTable = Nothing<Building_WorkTable>();
            my_workTable = null;
            extension_Skills = def.GetModExtension<ModExtension_Skills>();

        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            //this.workTable.ForEach(this.AllowWorkTable);
            AllowBills(my_workTable);

            base.DeSpawn();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            this.WorkTableSetting();
        }

        protected override void Reset()
        {
            if (this.State == WorkingState.Working && my_workTable != null)
            {
                if (this.unfinished == null)
                {
                    this.ingredients.ForEach(t => GenPlace.TryPlaceThing(t, Position, this.Map, ThingPlaceMode.Near));
                }
                else
                {
                    GenPlace.TryPlaceThing(this.unfinished, Position, this.Map, ThingPlaceMode.Near);
                    this.unfinished.Destroy(DestroyMode.Cancel);
                }
            }
            if (drilltypeBuilding != null)
            {

                CompDeepDrill compDeepDrill = drilltypeBuilding.TryGetComp<CompDeepDrill>();

                //Handle Progress
                float statValue = 1000f;

                ReflectionUtility.drill_portionProgress.SetValue(compDeepDrill, (float)ReflectionUtility.drill_portionProgress.GetValue(compDeepDrill) + statValue);
                ReflectionUtility.drill_portionYieldPct.SetValue(compDeepDrill, (float)ReflectionUtility.drill_portionYieldPct.GetValue(compDeepDrill) + statValue * 1 / 10000f);
                ReflectionUtility.drill_lastUsedTick.SetValue(compDeepDrill, Find.TickManager.TicksGame);
                if ((float)ReflectionUtility.drill_portionProgress.GetValue(compDeepDrill) > 10000f)
                {
                    ReflectionUtility.drill_TryProducePortion.Invoke(compDeepDrill, new object[] { ReflectionUtility.drill_portionYieldPct.GetValue(compDeepDrill) });
                    ReflectionUtility.drill_portionProgress.SetValue(compDeepDrill, 0);
                    ReflectionUtility.drill_portionYieldPct.SetValue(compDeepDrill, 0);
                }

            }
            if (researchBench != null && Find.ResearchManager.currentProj != null)
            {
                float statValue = 100f;
                statValue /= Find.ResearchManager.currentProj.CostFactor(Faction.OfPlayer.def.techLevel);
                Find.ResearchManager.ResearchPerformed(statValue, null);

            }


            this.bill = null;
            this.dominant = null;
            this.unfinished = null;
            this.ingredients = null;
            base.Reset();
        }

        protected override void CleanupWorkingEffect()
        {
            base.CleanupWorkingEffect();
            workingEffect?.Cleanup();
            workingEffect = null;

            workingSound?.End();
            workingSound = null;

            MapManager.RemoveEachTickAction(this.EffectTick);
        }

        protected override void CreateWorkingEffect()
        {
            if (my_workTable != null)
            {
                base.CreateWorkingEffect();

                this.workingEffect = this.bill.recipe.effectWorking.Spawn();

                this.workingSound = this.bill.recipe.soundWorking?.TrySpawnSustainer(my_workTable);
                workingSound?.Maintain();

                MapManager.EachTickAction(this.EffectTick);
            }
           
        }

        protected bool EffectTick()
        {
            workingEffect.EffectTick(new TargetInfo(this), new TargetInfo(my_workTable));

            return this.workingEffect == null;
        }

        /// <summary>
        /// Forbid bills to normal Pawns by converting them to a new bill type
        /// While saving the Original for restoration later
        /// </summary>
        /// <param name="worktable"></param>
        private void ForbidBills(Building_WorkTable worktable)
        {
            if (worktable.BillStack.Bills.Any(b => !(b is IBill_PawnForbidded)))
            {
                var tmp = worktable.BillStack.Bills.ToList();
                worktable.BillStack.Clear();
                worktable.BillStack.Bills.AddRange(tmp.SelectMany(b =>
                {
                    var forbidded = b as IBill_PawnForbidded;
                    if (forbidded == null)
                    {
                        if (b is Bill_ProductionWithUft)
                        {
                            forbidded = ((Bill_ProductionWithUft)b).CopyTo((Bill_ProductionWithUftPawnForbidded)Activator.CreateInstance(typeof(Bill_ProductionWithUftPawnForbidded), b.recipe));
                            ((Bill_Production)b).repeatMode = BillRepeatModeDefOf.Forever;
                            forbidded.Original = b;
                        }
                        else if (b is Bill_Production)
                        {
                            forbidded = ((Bill_Production)b).CopyTo((Bill_ProductionPawnForbidded)Activator.CreateInstance(typeof(Bill_ProductionPawnForbidded), b.recipe));
                            ((Bill_Production)b).repeatMode = BillRepeatModeDefOf.Forever;
                            forbidded.Original = b;
                        }
                    }
                    return Option((Bill)forbidded);
                }));
            }
        }
        /// <summary>
        /// Unforbid Bills by restoring them to the correct class Called after ForbidBills
        /// </summary>
        /// <param name="worktable"></param>
        private void AllowBills(Building_WorkTable worktable)
        {
            if (worktable.BillStack.Bills.Any(b => b is IBill_PawnForbidded))
            {
                var tmp = worktable.BillStack.Bills.ToList();
                worktable.BillStack.Clear();
                worktable.BillStack.Bills.AddRange(tmp.SelectMany(b =>
                {
                    var forbidded = b as IBill_PawnForbidded;
                    Bill unforbbided = b;
                    if (forbidded != null)
                    {
                        if (b is Bill_ProductionWithUft)
                        {
                            unforbbided = ((Bill_ProductionWithUft)b).CopyTo((Bill_ProductionWithUft)Activator.CreateInstance(forbidded.Original?.GetType() ?? typeof(Bill_ProductionWithUft), b.recipe));
                        }
                        else if (b is Bill_Production)
                        {
                            unforbbided = ((Bill_Production)b).CopyTo((Bill_Production)Activator.CreateInstance(forbidded.Original?.GetType() ?? typeof(Bill_Production), b.recipe));
                        }
                    }
                    return Option(unforbbided);
                }));
            }
        }

        protected override TargetInfo ProgressBarTarget()
        {
            return my_workTable;
        }

        /// <summary>
        /// TODO Check that one again
        /// </summary>
        private void WorkTableSetting()
        {
            var mynewWorkTable = GetmyTragetWorktable();
            //This If Check seems wrong. What happens if a Workbench gets replaced by another one?
            if(my_workTable != null && mynewWorkTable == null)
            {
                AllowBills(my_workTable);
            }

            drilltypeBuilding = GetTragetDrill();
            researchBench = GetTragetresearchBench();

            my_workTable = mynewWorkTable;
            if (my_workTable != null)
            {
                ForbidBills(my_workTable);
            }
           
        }

        protected override void Ready()
        {
            this.WorkTableSetting();
            base.Ready();
        }

        private IntVec3 FacingCell()
        {
            return this.Position + this.Rotation.FacingCell;
        }

        private Building_WorkTable GetmyTragetWorktable()
        {
            return (Building_WorkTable)this.FacingCell().GetThingList(Map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t is Building_WorkTable)
                .Where(t => t.InteractionCell == this.Position).FirstOrDefault();

        }

        private Building GetTragetDrill()
        {
            return (Building)this.FacingCell().GetThingList(Map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t is Building && t.TryGetComp<CompDeepDrill>() != null)
                .Where(t => t.InteractionCell == this.Position).FirstOrDefault();

        }

        private Building_ResearchBench GetTragetresearchBench()
        {
            return (Building_ResearchBench)this.FacingCell().GetThingList(Map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t is Building_ResearchBench)
                .Where(t => t.InteractionCell == this.Position).FirstOrDefault();

        }


        /// <summary>
        /// Try to start a new Bill to work on
        /// </summary>
        /// <param name="target"></param>
        /// <param name="workAmount"></param>
        /// <returns></returns>
        protected override bool TryStartWorking(out Building_AutoMachineTool target, out float workAmount)
        {
            target = this;
            workAmount = 0;
            //Return if there is no bill that shoul be done
            if ((my_workTable == null || !my_workTable.CurrentlyUsableForBills() || !my_workTable.billStack.AnyShouldDoNow) && drilltypeBuilding == null && (researchBench == null || Find.ResearchManager.currentProj == null)) return false;
            
            if (drilltypeBuilding != null)
            {
                CompDeepDrill compDeepDrill = drilltypeBuilding.TryGetComp<CompDeepDrill>();
                if (compDeepDrill.CanDrillNow())
                {
                   // Log.Message("Started Drill Bill");
                    workAmount = 100;
                    return true;
                }
                else
                {
                    return false;
                }



            }
            if (researchBench != null)
            {
                if (Find.ResearchManager.currentProj != null)
                {
                    workAmount = 1000;
                    return true;
                }
                else
                {
                    return false;
                }

            }

            var consumable = Consumable();

            List<ThingAmount> things;
            
            Bill nextbill = GetnextBill(consumable, out things);
            if (nextbill != null)
            {
                this.bill = nextbill;
                this.ingredients = things?.Select(t => t.thing.SplitOff(t.count)).ToList() ?? new List<Thing>();
                //Get dominant ingredient
                this.dominant = this.DominantIngredient(this.ingredients);


                if (this.bill.recipe.UsesUnfinishedThing)
                {
                    ThingDef stuff = (!this.bill.recipe.unfinishedThingDef.MadeFromStuff) ? null : this.dominant.def;
                    this.unfinished = (UnfinishedThing)ThingMaker.MakeThing(this.bill.recipe.unfinishedThingDef, stuff);
                    this.unfinished.BoundBill = (Bill_ProductionWithUft)this.bill;
                    this.unfinished.ingredients = this.ingredients;
                    CompColorable compColorable = this.unfinished.TryGetComp<CompColorable>();
                    if (compColorable != null)
                    {
                        compColorable.Color = this.dominant.DrawColor;
                    }
                }

                ThingDef thingDef = null;
                if (this.bill.recipe.UsesUnfinishedThing && this.bill.recipe.unfinishedThingDef.MadeFromStuff)
                {
                    thingDef = this.bill.recipe.UsesUnfinishedThing ? this.dominant?.def : null;
                }
                workAmount = this.bill.recipe.WorkAmountTotal(thingDef);

                return true;

            }
            else
            {
                workAmount = 0;
                return false;
            }
        }

        protected override bool FinishWorking(Building_AutoMachineTool working, out List<Thing> products)
        {
            products = GenRecipe2.MakeRecipeProducts(this.bill.recipe, this, this.ingredients, this.dominant, my_workTable).ToList();

            this.ingredients.ForEach(i => bill.recipe.Worker.ConsumeIngredient(i, bill.recipe, Map));
            Option(this.unfinished).ForEach(u => u.Destroy(DestroyMode.Vanish));
            this.bill.Notify_IterationCompleted(null, this.ingredients);

            this.bill = null;
            this.dominant = null;
            this.unfinished = null;
            this.ingredients = null;
            // Because we use custom GenRecipe2, we have to handle bonus items and product modifications directly:
            ModifyProductExt?.ProcessProducts(products, this as IBillGiver, this, this.bill.recipe); // this as IBillGiver is probably null

           

            return true;
        }

        public List<IntVec3> OutputZone()
        {
            return this.OutputCell().SlotGroupCells(Map);
        }
        
        public override IntVec3 OutputCell()
        {
            return compOutputAdjustable.CurrentCell;
        }

        private List<Thing> Consumable()
        {
            return this.GetAllTargetCells()
                .SelectMany(c=> c.AllThingsInCellForUse(Map)) // Use GatherThingsUtility to also grab from belts
                .ToList();
        }


        private Bill GetnextBill(List<Thing> consumable, out List<ThingAmount> ingredients)
        {
            ingredients = new List<ThingAmount>();
            //Return null as Workbench is not ready
            if (!my_workTable.CurrentlyUsableForBills()) return null;
            foreach (Bill bill in my_workTable.billStack)
            {
                //Ready to start?
                if (!bill.ShouldDoNow() || !bill.recipe.AvailableNow) continue;
                //Sufficiant skills?
                if (!bill.recipe.skillRequirements?.All(r => r.minLevel <= this.GetSkillLevel(r.skill)) ?? false) continue;

                if (bill.recipe.ingredients.Count == 0)
                {
                    ingredients = null;
                    return bill;
                }
                if (consumable == null) continue;
                ingredients = Ingredients(bill, consumable);
                if (ingredients.Count > 0) return bill;

            }
            ingredients = new List<ThingAmount>();
            return null;

        }

        private struct ThingDefGroup
        {
            public ThingDef def;
            public List<ThingAmount> consumable;
        }

        /// <summary>
        /// I guess thet finds the correct ingridiants for the bill
        /// </summary>
        /// <param name="bill"></param>
        /// <param name="consumable"></param>
        /// <returns></returns>
        private List<ThingAmount> Ingredients(Bill bill, List<Thing> consumable)
        {
            var initial = consumable
                //                .Where(c => bill.IsFixedOrAllowedIngredient(c))
                .Select(x => new ThingAmount(x, x.stackCount))
                .ToList();

            Func<List<ThingAmount>, List<ThingDefGroup>> grouping = (consumableAmounts) =>
                consumableAmounts
                    .GroupBy(c => c.thing.def)
                    .Select(c => new { Def = c.Key, Count = c.Sum(t => t.count), Amounts = c.Select(t => t) })
                    .OrderByDescending(g => g.Def.IsStuff)
                    .ThenByDescending(g => g.Count * bill.recipe.IngredientValueGetter.ValuePerUnitOf(g.Def))
                    .Select(g => new ThingDefGroup() { def = g.Def, consumable = g.Amounts.ToList() })
                    .ToList();

            var grouped = grouping(initial);

            var ingredients = bill.recipe.ingredients.Select(i =>
            {
                var result = new List<ThingAmount>();
                float remain = i.GetBaseCount();

                foreach (var things in grouped)
                {
                    foreach (var amount in things.consumable)
                    {
                        var thing = amount.thing;
                        if (i.filter.Allows(thing) && (bill.ingredientFilter.Allows(thing) || i.IsFixedIngredient))
                        {
                            remain = remain - bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def) * amount.count;
                            int consumption = amount.count;
                            if (remain <= 0.0f)
                            {
                                consumption -= Mathf.RoundToInt(-remain / bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def));
                                remain = 0.0f;
                            }
                            result.Add(new ThingAmount(thing, consumption));
                        }
                        if (remain <= 0.0f)
                            break;
                    }
                    if (remain <= 0.0f)
                        break;

                    if ((things.def.IsStuff && bill.recipe.productHasIngredientStuff) || !bill.recipe.allowMixingIngredients)
                    {
                        // ミックスしたり、stuffの場合には、一つの要求素材に複数種類のものを混ぜられない.
                        // なので、この種類では満たせなかったので、残りを戻して、中途半端に入った利用予定を空にする.
                        remain = i.GetBaseCount();
                        result.Clear();
                    }
                }

                if (remain <= 0.0f)
                {
                    // 残りがなく、必要分が全て割り当てられれば、割り当てた分を減らして、その状態でソートして割り当て分を返す.
                    result.ForEach(r =>
                    {
                        var list = grouped.Find(x => x.def == r.thing.def).consumable;
                        var c = list.Find(x => x.thing == r.thing);
                        list.Remove(c);
                        c.count = c.count - r.count;
                        list.Add(c);
                    });
                    grouped = grouping(grouped.SelectMany(x => x.consumable).ToList());
                    return result;
                }
                else
                {
                    // 割り当てできなければ、空リスト.
                    return new List<ThingAmount>();
                }
            }).ToList();

            if (ingredients.All(x => x.Count > 0))
            {
                return ingredients.SelectMany(c => c).ToList();
            }
            else
            {
                return new List<ThingAmount>();
            }
        }

        public override IEnumerable<InspectTabBase> GetInspectTabs()
        {
            return base.GetInspectTabs();
        }

        private Thing DominantIngredient(List<Thing> ingredients)
        {
            if (ingredients.Count == 0)
            {
                return null;
            }
            if (this.bill.recipe.productHasIngredientStuff)
            {
                return ingredients[0];
            }
            if (this.bill.recipe.products.Any(x => x.thingDef.MadeFromStuff))
            {
                return ingredients.Where(x => x.def.IsStuff).RandomElementByWeight((Thing x) => (float)x.stackCount);
            }
            return ingredients.RandomElementByWeight((Thing x) => (float)x.stackCount);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            return msg;
        }

        public Room GetRoom(RegionType type)
        {
            return RegionAndRoomQuery.GetRoom(this, type);
        }

        protected override bool WorkInterruption(Building_AutoMachineTool working)
        {
            //Interupt if worktable chenged or is null
            if (my_workTable == null || GetmyTragetWorktable() == null || GetmyTragetWorktable() != my_workTable) return true;
            //Interrupt if worktable is not ready for work
            return !my_workTable.CurrentlyUsableForBills();
        }

        private class ThingAmount
        {
            public ThingAmount(Thing thing, int count)
            {
                this.thing = thing;
                this.count = count;
            }

            public Thing thing;

            public int count;
        }
    }

}
