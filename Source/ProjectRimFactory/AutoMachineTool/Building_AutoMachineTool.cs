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

        private Map M { get { return this.Map; } }
        private IntVec3 P { get { return this.Position; } }

        private Bill bill;
        private List<Thing> ingredients;
        private Thing dominant;
        private UnfinishedThing unfinished;
        private int outputIndex = 0;
        private bool forbidItem = false;

        [Unsaved]
        private Option<Effecter> workingEffect = Nothing<Effecter>();
        [Unsaved]
        private Option<Sustainer> workingSound = Nothing<Sustainer>();
        [Unsaved]
        private Option<Building_WorkTable> workTable;


        ModExtension_Skills extension_Skills;

        public ModExtension_BonusYield modExtension_BonusYield => this.def.GetModExtension<ModExtension_BonusYield>();

        public int GetSkillLevel(SkillDef def)
        {
            return extension_Skills?.GetExtendedSkillLevel(def,typeof(Building_AutoMachineTool)) ?? this.SkillLevel ?? 0;
        }

        private IntVec3[] adjacent =
        {
            new IntVec3(0, 0, 1),
            new IntVec3(1, 0, 1),
            new IntVec3(1, 0, 0),
            new IntVec3(1, 0, -1),
            new IntVec3(0, 0, -1),
            new IntVec3(-1, 0, -1),
            new IntVec3(-1, 0, 0),
            new IntVec3(-1, 0, 1)
        };
        private string[] adjacentName =
        {
            "N",
            "NE",
            "E",
            "SE",
            "S",
            "SW",
            "W",
            "NW"
        };

        protected override int? SkillLevel { get { return this.def.GetModExtension<ModExtension_Tier>()?.skillLevel; } }

        // seem to be included in Building_BaseMachine.cs already (zymex)
        // public override int MaxPowerForSpeed { get { return this.Setting.AutoMachineToolTier(Extension.tier).maxSupplyPowerForSpeed; } }
        // public override int MinPowerForSpeed { get { return this.Setting.AutoMachineToolTier(Extension.tier).minSupplyPowerForSpeed; } }
        // protected override float SpeedFactor { get { return this.Setting.AutoMachineToolTier(Extension.tier).speedFactor; } }
        
        public override bool Glowable => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.outputIndex, "outputIndex");
            Scribe_Values.Look<bool>(ref this.forbidItem, "forbidItem");

            Scribe_Deep.Look<UnfinishedThing>(ref this.unfinished, "unfinished");

            Scribe_References.Look<Bill>(ref this.bill, "bill");
            Scribe_References.Look<Thing>(ref this.dominant, "dominant");
            Scribe_Collections.Look<Thing>(ref this.ingredients, "ingredients", LookMode.Deep);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.workTable = Nothing<Building_WorkTable>();
            extension_Skills = def.GetModExtension<ModExtension_Skills>();

        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.workTable.ForEach(this.AllowWorkTable);
            base.DeSpawn();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            this.WorkTableSetting();
        }

        protected override void Reset()
        {
            if (this.State == WorkingState.Working)
            {
                if (this.unfinished == null)
                {
                    this.ingredients.ForEach(t => GenPlace.TryPlaceThing(t, P, this.M, ThingPlaceMode.Near));
                }
                else
                {
                    GenPlace.TryPlaceThing(this.unfinished, P, this.M, ThingPlaceMode.Near);
                    this.unfinished.Destroy(DestroyMode.Cancel);
                }
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

            this.workingEffect.ForEach(e => e.Cleanup());
            this.workingEffect = Nothing<Effecter>();

            this.workingSound.ForEach(s => s.End());
            this.workingSound = Nothing<Sustainer>();

            MapManager.RemoveEachTickAction(this.EffectTick);
        }

        protected override void CreateWorkingEffect()
        {
            base.CreateWorkingEffect();

            this.workingEffect = this.workingEffect.Fold(() => Option(this.bill.recipe.effectWorking).Select(e => e.Spawn()))(e => Option(e));

            this.workingSound = this.workingSound.Fold(() => this.workTable.SelectMany(t => Option(this.bill.recipe.soundWorking).Select(s => s.TrySpawnSustainer(t))))(s => Option(s))
                .Peek(s => s.Maintain());

            MapManager.EachTickAction(this.EffectTick);
        }

        protected bool EffectTick()
        {
            this.workingEffect.ForEach(e => this.workTable.ForEach(w => e.EffectTick(new TargetInfo(this), new TargetInfo(w))));
            return !this.workingEffect.HasValue;
        }

        private void ForbidWorkTable(Building_WorkTable worktable)
        {
            this.ForbidBills(worktable);
        }

        private void AllowWorkTable(Building_WorkTable worktable)
        {
            this.AllowBills(worktable);
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
            return this.workTable.GetOrDefault(null);
        }

        private void WorkTableSetting()
        {
            var currentWotkTable = this.GetTargetWorkTable();
            if (this.workTable.HasValue && !currentWotkTable.HasValue)
            {
                this.AllowWorkTable(this.workTable.Value);
            }
            currentWotkTable.ForEach(w => this.ForbidWorkTable(w));
            this.workTable = currentWotkTable;
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

        private Option<Building_WorkTable> GetTargetWorkTable()
        {
            return this.FacingCell().GetThingList(M)
                .Where(t => t.def.category == ThingCategory.Building)
                .SelectMany(t => Option(t as Building_WorkTable))
                .Where(t => t.InteractionCell == this.Position)
                .FirstOption();
        }

        protected override bool TryStartWorking(out Building_AutoMachineTool target, out float workAmount)
        {
            target = this;
            workAmount = 0;
            if (!this.workTable.Where(t => t.CurrentlyUsableForBills() && t.billStack.AnyShouldDoNow).HasValue)
            {
                return false;
            }
            var consumable = Consumable();
            var result = WorkableBill(consumable).Select(tuple =>
            {
                //changed from .value1 and value2 to item1 and item2 when ported over.. (zymex)
                this.bill = tuple.Item1;
                //                tuple.Value2.Select(v => v.thing).SelectMany(t => Option(t as Corpse)).ForEach(c => c.Strip());
                this.ingredients = tuple.Item2.Select(t => t.thing.SplitOff(t.count)).ToList();
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
                return new { Result = true, WorkAmount = this.bill.recipe.WorkAmountTotal(this.bill.recipe.UsesUnfinishedThing ? this.dominant?.def : null) };
            }).GetOrDefault(new { Result = false, WorkAmount = 0f });
            workAmount = result.WorkAmount;
            return result.Result;
        }

        protected override bool FinishWorking(Building_AutoMachineTool working, out List<Thing> products)
        {
            products = GenRecipe2.MakeRecipeProducts(this.bill.recipe, this, this.ingredients, this.dominant, this.workTable.GetOrDefault(null)).ToList();
            this.ingredients.ForEach(i => bill.recipe.Worker.ConsumeIngredient(i, bill.recipe, M));
            Option(this.unfinished).ForEach(u => u.Destroy(DestroyMode.Vanish));
            this.bill.Notify_IterationCompleted(null, this.ingredients);

            this.bill = null;
            this.dominant = null;
            this.unfinished = null;
            this.ingredients = null;
            Thing bonus = modExtension_BonusYield?.GetBonusYield(QualityCategory.Normal) ?? null;
            if (bonus != null)
            {
                products.Add(bonus);
            }

            return true;
        }

        public List<IntVec3> OutputZone()
        {
            return this.OutputCell().SlotGroupCells(M);
        }

        public override IntVec3 OutputCell()
        {
            return this.Position + this.adjacent[this.outputIndex];
        }

        private List<Thing> Consumable()
        {
            return this.GetAllTargetCells()
                .SelectMany(c => c.GetThingList(M))
                .Where(c => c.def.category == ThingCategory.Item)
                .ToList();
        }

        private Option<Tuple<Bill, List<ThingAmount>>> WorkableBill(List<Thing> consumable)
        {
            return this.workTable
                .Where(t => t.CurrentlyUsableForBills())
                .SelectMany(wt => wt.billStack.Bills
                    .Where(b => b.ShouldDoNow())
                    .Where(b => b.recipe.AvailableNow)
                    .Where(b => Option(b.recipe.skillRequirements).Fold(true)(s => s.Where(x => x != null).All(r => r.minLevel <= this.GetSkillLevel(r.skill))))
                    .Select(b => Tuple(b, Ingredients(b, consumable)))
                    // changed from Value1 and Value2 to Item1 and Item2 when ported over (zymex)
                    .Where(t => t.Item1.recipe.ingredients.Count == 0 || t.Item2.Count > 0)
                    .FirstOption()
                );
        }

        private struct ThingDefGroup
        {
            public ThingDef def;
            public List<ThingAmount> consumable;
        }

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

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            msg += "PRF.AutoMachineTool.OutputDirection".Translate(("PRF.AutoMachineTool.OutputDirection" + this.adjacentName[this.outputIndex]).Translate());
            return msg;
        }

        public Room GetRoom(RegionType type)
        {
            return RegionAndRoomQuery.GetRoom(this, type);
        }

        protected override bool WorkInterruption(Building_AutoMachineTool working)
        {
            if (!this.workTable.HasValue)
            {
                return true;
            }
            var currentTable = GetTargetWorkTable();
            if (!currentTable.HasValue)
            {
                return true;
            }
            if (currentTable.Value != this.workTable.Value)
            {
                return true;
            }
            return !this.workTable.Value.CurrentlyUsableForBills();
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

    public class Building_AutoMachineToolCellResolver : BaseTargetCellResolver, IOutputCellResolver
    {
        public override bool NeedClearingCache => false;

        public override IEnumerable<IntVec3> GetRangeCells(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot, int range)
        {
            return GenAdj.CellsOccupiedBy(center, rot, new IntVec2(1, 1) + new IntVec2(range * 2, range * 2));
        }

        public override int GetRange(float power)
        {
            return Mathf.RoundToInt(power / 500) + 1;
        }

        public Option<IntVec3> OutputCell(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return center.GetThingList(map)
                .SelectMany(b => Option(b as Building_AutoMachineTool))
                .FirstOption()
                .Select(b => b.OutputCell());
        }

        private readonly static List<IntVec3> EmptyList = new List<IntVec3>();

        public IEnumerable<IntVec3> OutputZoneCells(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return this.OutputCell(def, center, size, map, rot).Select(c => c.SlotGroupCells(map)).GetOrDefault(EmptyList);
        }
    }
}
