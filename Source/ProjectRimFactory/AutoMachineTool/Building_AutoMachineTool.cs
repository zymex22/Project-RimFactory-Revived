using System;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_AutoMachineTool : Building_BaseRange<Building_AutoMachineTool>, IRecipeProductWorker
    {
        private Bill bill;
        private Thing dominant;


        private ModExtension_Skills extension_Skills;

        private bool forbidItem;
        private List<Thing> ingredients;
        private UnfinishedThing unfinished;

        [Unsaved] private Option<Effecter> workingEffect = Nothing<Effecter>();

        [Unsaved] private Option<Sustainer> workingSound = Nothing<Sustainer>();

        [Unsaved] private Option<Building_WorkTable> workTable;

        public Building_AutoMachineTool()
        {
            forcePlace = false;
            targetEnumrationCount = 0;
        }

        private Map M => Map;
        private IntVec3 P => Position;

        public ModExtension_BonusYield modExtension_BonusYield => def.GetModExtension<ModExtension_BonusYield>();

        protected override int? SkillLevel => def.GetModExtension<ModExtension_Tier>()?.skillLevel;

        // seem to be included in Building_BaseMachine.cs already (zymex)
        // public override int MaxPowerForSpeed { get { return this.Setting.AutoMachineToolTier(Extension.tier).maxSupplyPowerForSpeed; } }
        // public override int MinPowerForSpeed { get { return this.Setting.AutoMachineToolTier(Extension.tier).minSupplyPowerForSpeed; } }
        // protected override float SpeedFactor { get { return this.Setting.AutoMachineToolTier(Extension.tier).speedFactor; } }

        public override bool Glowable => false;

        public int GetSkillLevel(SkillDef def)
        {
            return extension_Skills?.GetExtendedSkillLevel(def, typeof(Building_AutoMachineTool)) ?? SkillLevel ?? 0;
        }

        public Room GetRoom(RegionType type)
        {
            return RegionAndRoomQuery.GetRoom(this, type);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref forbidItem, "forbidItem");

            Scribe_Deep.Look(ref unfinished, "unfinished");

            Scribe_References.Look(ref bill, "bill");
            Scribe_References.Look(ref dominant, "dominant");
            Scribe_Collections.Look(ref ingredients, "ingredients", LookMode.Deep);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            workTable = Nothing<Building_WorkTable>();
            extension_Skills = def.GetModExtension<ModExtension_Skills>();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            workTable.ForEach(AllowWorkTable);
            base.DeSpawn();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            WorkTableSetting();
        }

        protected override void Reset()
        {
            if (State == WorkingState.Working)
            {
                if (unfinished == null)
                {
                    ingredients.ForEach(t => GenPlace.TryPlaceThing(t, P, M, ThingPlaceMode.Near));
                }
                else
                {
                    GenPlace.TryPlaceThing(unfinished, P, M, ThingPlaceMode.Near);
                    unfinished.Destroy(DestroyMode.Cancel);
                }
            }

            bill = null;
            dominant = null;
            unfinished = null;
            ingredients = null;
            base.Reset();
        }

        protected override void CleanupWorkingEffect()
        {
            base.CleanupWorkingEffect();

            workingEffect.ForEach(e => e.Cleanup());
            workingEffect = Nothing<Effecter>();

            workingSound.ForEach(s => s.End());
            workingSound = Nothing<Sustainer>();

            MapManager.RemoveEachTickAction(EffectTick);
        }

        protected override void CreateWorkingEffect()
        {
            base.CreateWorkingEffect();

            workingEffect =
                workingEffect.Fold(() => Option(bill.recipe.effectWorking).Select(e => e.Spawn()))(e => Option(e));

            workingSound = workingSound.Fold(() =>
                    workTable.SelectMany(t => Option(bill.recipe.soundWorking).Select(s => s.TrySpawnSustainer(t))))(
                    s => Option(s))
                .Peek(s => s.Maintain());

            MapManager.EachTickAction(EffectTick);
        }

        protected bool EffectTick()
        {
            workingEffect.ForEach(e => workTable.ForEach(w => e.EffectTick(new TargetInfo(this), new TargetInfo(w))));
            return !workingEffect.HasValue;
        }

        private void ForbidWorkTable(Building_WorkTable worktable)
        {
            ForbidBills(worktable);
        }

        private void AllowWorkTable(Building_WorkTable worktable)
        {
            AllowBills(worktable);
        }

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
                            forbidded = ((Bill_ProductionWithUft) b).CopyTo(
                                (Bill_ProductionWithUftPawnForbidded) Activator.CreateInstance(
                                    typeof(Bill_ProductionWithUftPawnForbidded), b.recipe));
                            ((Bill_Production) b).repeatMode = BillRepeatModeDefOf.Forever;
                            forbidded.Original = b;
                        }
                        else if (b is Bill_Production)
                        {
                            forbidded = ((Bill_Production) b).CopyTo(
                                (Bill_ProductionPawnForbidded) Activator.CreateInstance(
                                    typeof(Bill_ProductionPawnForbidded), b.recipe));
                            ((Bill_Production) b).repeatMode = BillRepeatModeDefOf.Forever;
                            forbidded.Original = b;
                        }
                    }

                    return Option((Bill) forbidded);
                }));
            }
        }

        private void AllowBills(Building_WorkTable worktable)
        {
            if (worktable.BillStack.Bills.Any(b => b is IBill_PawnForbidded))
            {
                var tmp = worktable.BillStack.Bills.ToList();
                worktable.BillStack.Clear();
                worktable.BillStack.Bills.AddRange(tmp.SelectMany(b =>
                {
                    var forbidded = b as IBill_PawnForbidded;
                    var unforbbided = b;
                    if (forbidded != null)
                    {
                        if (b is Bill_ProductionWithUft)
                            unforbbided = ((Bill_ProductionWithUft) b).CopyTo(
                                (Bill_ProductionWithUft) Activator.CreateInstance(
                                    forbidded.Original?.GetType() ?? typeof(Bill_ProductionWithUft), b.recipe));
                        else if (b is Bill_Production)
                            unforbbided = ((Bill_Production) b).CopyTo(
                                (Bill_Production) Activator.CreateInstance(
                                    forbidded.Original?.GetType() ?? typeof(Bill_Production), b.recipe));
                    }

                    return Option(unforbbided);
                }));
            }
        }

        protected override TargetInfo ProgressBarTarget()
        {
            return workTable.GetOrDefault(null);
        }

        private void WorkTableSetting()
        {
            var currentWotkTable = GetTargetWorkTable();
            if (workTable.HasValue && !currentWotkTable.HasValue) AllowWorkTable(workTable.Value);
            currentWotkTable.ForEach(w => ForbidWorkTable(w));
            workTable = currentWotkTable;
        }

        protected override void Ready()
        {
            WorkTableSetting();
            base.Ready();
        }

        private IntVec3 FacingCell()
        {
            return Position + Rotation.FacingCell;
        }

        private Option<Building_WorkTable> GetTargetWorkTable()
        {
            return FacingCell().GetThingList(M)
                .Where(t => t.def.category == ThingCategory.Building)
                .SelectMany(t => Option(t as Building_WorkTable))
                .Where(t => t.InteractionCell == Position)
                .FirstOption();
        }

        protected override bool TryStartWorking(out Building_AutoMachineTool target, out float workAmount)
        {
            target = this;
            workAmount = 0;
            if (!workTable.Where(t => t.CurrentlyUsableForBills() && t.billStack.AnyShouldDoNow).HasValue) return false;
            var consumable = Consumable();
            var result = WorkableBill(consumable).Select(tuple =>
            {
                //changed from .value1 and value2 to item1 and item2 when ported over.. (zymex)
                bill = tuple.Item1;
                //                tuple.Value2.Select(v => v.thing).SelectMany(t => Option(t as Corpse)).ForEach(c => c.Strip());
                ingredients = tuple.Item2.Select(t => t.thing.SplitOff(t.count)).ToList();
                dominant = DominantIngredient(ingredients);
                if (bill.recipe.UsesUnfinishedThing)
                {
                    var stuff = !bill.recipe.unfinishedThingDef.MadeFromStuff ? null : dominant.def;
                    unfinished = (UnfinishedThing) ThingMaker.MakeThing(bill.recipe.unfinishedThingDef, stuff);
                    unfinished.BoundBill = (Bill_ProductionWithUft) bill;
                    unfinished.ingredients = ingredients;
                    var compColorable = unfinished.TryGetComp<CompColorable>();
                    if (compColorable != null) compColorable.Color = dominant.DrawColor;
                }

                return new
                {
                    Result = true,
                    WorkAmount = bill.recipe.WorkAmountTotal(bill.recipe.UsesUnfinishedThing ? dominant?.def : null)
                };
            }).GetOrDefault(new {Result = false, WorkAmount = 0f});
            workAmount = result.WorkAmount;
            return result.Result;
        }

        protected override bool FinishWorking(Building_AutoMachineTool working, out List<Thing> products)
        {
            products = GenRecipe2
                .MakeRecipeProducts(bill.recipe, this, ingredients, dominant, workTable.GetOrDefault(null)).ToList();
            ingredients.ForEach(i => bill.recipe.Worker.ConsumeIngredient(i, bill.recipe, M));
            Option(unfinished).ForEach(u => u.Destroy());
            bill.Notify_IterationCompleted(null, ingredients);

            bill = null;
            dominant = null;
            unfinished = null;
            ingredients = null;
            var bonus = modExtension_BonusYield?.GetBonusYield(bill.recipe, QualityCategory.Normal) ?? null;
            if (bonus != null) products.Add(bonus);

            return true;
        }

        public List<IntVec3> OutputZone()
        {
            return OutputCell().SlotGroupCells(M);
        }

        public override IntVec3 OutputCell()
        {
            return compOutputAdjustable.CurrentCell;
        }

        private List<Thing> Consumable()
        {
            return GetAllTargetCells()
                .SelectMany(c => c.GetThingList(M))
                .Where(c => c.def.category == ThingCategory.Item)
                .ToList();
        }

        private Option<Tuple<Bill, List<ThingAmount>>> WorkableBill(List<Thing> consumable)
        {
            return workTable
                .Where(t => t.CurrentlyUsableForBills())
                .SelectMany(wt => wt.billStack.Bills
                    .Where(b => b.ShouldDoNow())
                    .Where(b => b.recipe.AvailableNow)
                    .Where(b => Option(b.recipe.skillRequirements).Fold(true)(s =>
                        s.Where(x => x != null).All(r => r.minLevel <= GetSkillLevel(r.skill))))
                    .Select(b => Tuple(b, Ingredients(b, consumable)))
                    // changed from Value1 and Value2 to Item1 and Item2 when ported over (zymex)
                    .Where(t => t.Item1.recipe.ingredients.Count == 0 || t.Item2.Count > 0)
                    .FirstOption()
                );
        }

        private List<ThingAmount> Ingredients(Bill bill, List<Thing> consumable)
        {
            var initial = consumable
                //                .Where(c => bill.IsFixedOrAllowedIngredient(c))
                .Select(x => new ThingAmount(x, x.stackCount))
                .ToList();

            Func<List<ThingAmount>, List<ThingDefGroup>> grouping = consumableAmounts =>
                consumableAmounts
                    .GroupBy(c => c.thing.def)
                    .Select(c => new {Def = c.Key, Count = c.Sum(t => t.count), Amounts = c.Select(t => t)})
                    .OrderByDescending(g => g.Def.IsStuff)
                    .ThenByDescending(g => g.Count * bill.recipe.IngredientValueGetter.ValuePerUnitOf(g.Def))
                    .Select(g => new ThingDefGroup {def = g.Def, consumable = g.Amounts.ToList()})
                    .ToList();

            var grouped = grouping(initial);

            var ingredients = bill.recipe.ingredients.Select(i =>
            {
                var result = new List<ThingAmount>();
                var remain = i.GetBaseCount();

                foreach (var things in grouped)
                {
                    foreach (var amount in things.consumable)
                    {
                        var thing = amount.thing;
                        if (i.filter.Allows(thing) && (bill.ingredientFilter.Allows(thing) || i.IsFixedIngredient))
                        {
                            remain = remain - bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def) *
                                amount.count;
                            var consumption = amount.count;
                            if (remain <= 0.0f)
                            {
                                consumption -=
                                    Mathf.RoundToInt(-remain /
                                                     bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def));
                                remain = 0.0f;
                            }

                            result.Add(new ThingAmount(thing, consumption));
                        }

                        if (remain <= 0.0f)
                            break;
                    }

                    if (remain <= 0.0f)
                        break;

                    if (things.def.IsStuff && bill.recipe.productHasIngredientStuff ||
                        !bill.recipe.allowMixingIngredients)
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

                // 割り当てできなければ、空リスト.
                return new List<ThingAmount>();
            }).ToList();

            if (ingredients.All(x => x.Count > 0))
                return ingredients.SelectMany(c => c).ToList();
            return new List<ThingAmount>();
        }

        public override IEnumerable<InspectTabBase> GetInspectTabs()
        {
            return base.GetInspectTabs();
        }

        private Thing DominantIngredient(List<Thing> ingredients)
        {
            if (ingredients.Count == 0) return null;
            if (bill.recipe.productHasIngredientStuff) return ingredients[0];
            if (bill.recipe.products.Any(x => x.thingDef.MadeFromStuff))
                return ingredients.Where(x => x.def.IsStuff).RandomElementByWeight(x => (float) x.stackCount);
            return ingredients.RandomElementByWeight(x => (float) x.stackCount);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
        }

        public override string GetInspectString()
        {
            var msg = base.GetInspectString();
            return msg;
        }

        protected override bool WorkInterruption(Building_AutoMachineTool working)
        {
            if (!workTable.HasValue) return true;
            var currentTable = GetTargetWorkTable();
            if (!currentTable.HasValue) return true;
            if (currentTable.Value != workTable.Value) return true;
            return !workTable.Value.CurrentlyUsableForBills();
        }

        public interface IBill_PawnForbidded
        {
            Bill Original { get; set; }
        }

        public class Bill_ProductionPawnForbidded : Bill_Production, IBill_PawnForbidded
        {
            public Bill original;

            public Bill_ProductionPawnForbidded()
            {
            }

            public Bill_ProductionPawnForbidded(RecipeDef recipe) : base(recipe)
            {
            }

            public Bill Original
            {
                get => original;
                set => original = value;
            }

            public override bool PawnAllowedToStartAnew(Pawn p)
            {
                return false;
            }

            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Deep.Look(ref original, "original");
                if (Scribe.mode == LoadSaveMode.PostLoadInit) original.billStack = billStack;
            }

            public override Bill Clone()
            {
                var clone = (Bill_Production) original.Clone();
                return this.CopyTo(clone);
            }

            public override void Notify_DoBillStarted(Pawn billDoer)
            {
                base.Notify_DoBillStarted(billDoer);
                Option(original).ForEach(o => o.Notify_DoBillStarted(billDoer));
            }

            public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
            {
                base.Notify_IterationCompleted(billDoer, ingredients);
                Option(original).ForEach(o => o.Notify_IterationCompleted(billDoer, ingredients));
            }

            public override void Notify_PawnDidWork(Pawn p)
            {
                base.Notify_PawnDidWork(p);
                Option(original).ForEach(o => o.Notify_PawnDidWork(p));
            }

            // proxy call. override other properties and methods.
        }

        public class Bill_ProductionWithUftPawnForbidded : Bill_ProductionWithUft, IBill_PawnForbidded
        {
            public Bill original;

            public Bill_ProductionWithUftPawnForbidded()
            {
            }

            public Bill_ProductionWithUftPawnForbidded(RecipeDef recipe) : base(recipe)
            {
            }

            public Bill Original
            {
                get => original;
                set => original = value;
            }

            public override bool PawnAllowedToStartAnew(Pawn p)
            {
                return false;
            }

            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Deep.Look(ref original, "original");
                if (Scribe.mode == LoadSaveMode.PostLoadInit) original.billStack = billStack;
            }

            public override Bill Clone()
            {
                var clone = (Bill_ProductionWithUft) original.Clone();
                return this.CopyTo(clone);
            }

            public override void Notify_DoBillStarted(Pawn billDoer)
            {
                base.Notify_DoBillStarted(billDoer);
                Option(original).ForEach(o => o.Notify_DoBillStarted(billDoer));
            }

            public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
            {
                base.Notify_IterationCompleted(billDoer, ingredients);
                Option(original).ForEach(o => o.Notify_IterationCompleted(billDoer, ingredients));
            }

            public override void Notify_PawnDidWork(Pawn p)
            {
                base.Notify_PawnDidWork(p);
                Option(original).ForEach(o => o.Notify_PawnDidWork(p));
            }

            // proxy call. override other properties and methods.
        }

        private struct ThingDefGroup
        {
            public ThingDef def;
            public List<ThingAmount> consumable;
        }

        private class ThingAmount
        {
            public int count;

            public readonly Thing thing;

            public ThingAmount(Thing thing, int count)
            {
                this.thing = thing;
                this.count = count;
            }
        }
    }

    public class Building_AutoMachineToolCellResolver : BaseTargetCellResolver, IOutputCellResolver
    {
        private static readonly List<IntVec3> EmptyList = new List<IntVec3>();
        public override bool NeedClearingCache => false;

        public Option<IntVec3> OutputCell(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return center.GetThingList(map)
                .SelectMany(b => Option(b as Building_AutoMachineTool))
                .FirstOption()
                .Select(b => b.OutputCell());
        }

        public IEnumerable<IntVec3> OutputZoneCells(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return OutputCell(def, center, size, map, rot).Select(c => c.SlotGroupCells(map)).GetOrDefault(EmptyList);
        }

        public override IEnumerable<IntVec3> GetRangeCells(ThingDef def, IntVec3 center, IntVec2 size, Map map,
            Rot4 rot, int range)
        {
            return GenAdj.CellsOccupiedBy(center, rot, new IntVec2(1, 1) + new IntVec2(range * 2, range * 2));
        }

        public override int GetRange(float power)
        {
            return Mathf.RoundToInt(power / 500) + 1;
        }
    }
}