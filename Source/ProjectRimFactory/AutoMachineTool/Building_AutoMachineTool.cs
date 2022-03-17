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


    public abstract class SAL_TargetBench : IExposable
    {
        protected Building_AutoMachineTool mySAL;
        protected IntVec3 Position;
        protected Map map;
        protected Rot4 Rotation;

        public SAL_TargetBench(Building_AutoMachineTool mySAL, IntVec3 position, Map map, Rot4 rotation)
        {
            this.mySAL = mySAL;
            Position = position;
            this.map = map;
            Rotation = rotation;
        }

        public abstract bool Ready();
        public abstract void Reserve();
        public abstract void Free();
        public abstract void WorkDone(out List<Thing> products);
        public abstract bool TryStartWork(out float workAmount);
        public abstract TargetInfo TargetInfo();

        public virtual void Reset(WorkingState workingState)
        {

        }
        public virtual void CreateWorkingEffect(MapTickManager mapTickManager)
        {

        }
        public virtual void CleanupWorkingEffect(MapTickManager mapTickManager)
        {

        }



        public void generalReserve(Building tb)
        {
            if (PRFGameComponent.PRF_StaticPawn == null) PRFGameComponent.GenStaticPawn();
            if (PRFGameComponent.PRF_StaticJob == null) PRFGameComponent.PRF_StaticJob = new Job(PRFDefOf.PRFStaticJob);

            List<ReservationManager.Reservation> reservations;
            reservations = (List<ReservationManager.Reservation>)ReflectionUtility.sal_reservations.GetValue(map.reservationManager);
            var res = new ReservationManager.Reservation(PRFGameComponent.PRF_StaticPawn, PRFGameComponent.PRF_StaticJob, 1, -1, tb, null);

            if (!reservations.Where(r => r.Claimant == PRFGameComponent.PRF_StaticPawn && r.Job == PRFGameComponent.PRF_StaticJob && r.Target == tb).Any()) reservations.Add(res);
            ReflectionUtility.sal_reservations.SetValue(map.reservationManager, reservations);

            //Spammy Debug
            /*
            reservations = (List<ReservationManager.Reservation>)ReflectionUtility.sal_reservations.GetValue(Map.reservationManager);
            reservations = reservations.Where(r => r.Faction != null && r.Faction.IsPlayer).ToList();
           foreach (ReservationManager.Reservation res in reservations)
            {
                Log.Message("Reservation for " + res.Claimant + " at " + res.Target);

            }
            */
        }
        public void generalRelease(Building tb)
        {
            if (PRFGameComponent.PRF_StaticPawn == null) PRFGameComponent.GenStaticPawn();
            if (PRFGameComponent.PRF_StaticJob == null) PRFGameComponent.PRF_StaticJob = new Job(PRFDefOf.PRFStaticJob);

            /*
            Log.Message("----------------------------------");
            List<ReservationManager.Reservation> reservations;
            reservations = (List<ReservationManager.Reservation>)ReflectionUtility.sal_reservations.GetValue(Map.reservationManager);
            reservations = reservations.Where(r => r.Faction != null && r.Faction.IsPlayer).ToList();
            foreach (ReservationManager.Reservation res in reservations)
            {
                Log.Message("Reservation for " + res.Claimant + " at " + res.Target);

            }
            */

            map.reservationManager.Release(tb, PRFGameComponent.PRF_StaticPawn, PRFGameComponent.PRF_StaticJob);
            //Log.Message("generalRelease for " + (Position + Rotation.FacingCell) );
        }

        public virtual void ExposeData()
        {
            
        }
    }
    public class SAL_TargetWorktable : SAL_TargetBench
    {
        public Building_WorkTable my_workTable;

        private Bill bill;
        private List<Thing> ingredients;
        private Thing dominant;
        private UnfinishedThing unfinished;

        [Unsaved]
        private Effecter workingEffect = null;
        [Unsaved]
        private Sustainer workingSound = null;

        public SAL_TargetWorktable(Building_AutoMachineTool mySAL, IntVec3 position, Map map, Rot4 rotation, Building_WorkTable my_workTable) : base(mySAL, position, map, rotation)
        {
            this.my_workTable = my_workTable;
        }

        public override bool Ready()
        {
            return !(!my_workTable.CurrentlyUsableForBills() || !my_workTable.billStack.AnyShouldDoNow);
        }

        public override void Free()
        {
            AllowBills();
        }
        public override void Reserve()
        {
            ForbidBills();
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look<UnfinishedThing>(ref this.unfinished, "unfinished");
            Scribe_References.Look<Bill>(ref this.bill, "bill");
            Scribe_References.Look<Thing>(ref this.dominant, "dominant");
            Scribe_Collections.Look<Thing>(ref this.ingredients, "ingredients", LookMode.Deep);
        }

        public interface IBill_PawnForbidded
        {
            Bill Original { get; set; }
        }

        /// <summary>
        /// Forbid bills to normal Pawns by converting them to a new bill type
        /// While saving the Original for restoration later
        /// </summary>
        private void ForbidBills()
        {
            if (my_workTable.BillStack.Bills.Any(b => !(b is IBill_PawnForbidded)))
            {
                var tmp = my_workTable.BillStack.Bills.ToList();
                my_workTable.BillStack.Clear();
                my_workTable.BillStack.Bills.AddRange(tmp.SelectMany(b =>
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
        private void AllowBills()
        {
            if (my_workTable.BillStack.Bills.Any(b => b is IBill_PawnForbidded))
            {
                var tmp = my_workTable.BillStack.Bills.ToList();
                my_workTable.BillStack.Clear();
                my_workTable.BillStack.Bills.AddRange(tmp.SelectMany(b =>
                {
                    var forbidded = b as IBill_PawnForbidded;
                    Bill unforbbided = b;
                    if (forbidded != null)
                    {
                        if (b is Bill_ProductionWithUft)
                        {
                            unforbbided = ((Bill_ProductionWithUft)b).CopyTo((Bill_ProductionWithUft)Activator.CreateInstance(forbidded.Original?.GetType() ?? typeof(Bill_ProductionWithUft), b.recipe, b.precept));
                        }
                        else if (b is Bill_Production)
                        {
                            unforbbided = ((Bill_Production)b).CopyTo((Bill_Production)Activator.CreateInstance(forbidded.Original?.GetType() ?? typeof(Bill_Production), b.recipe, b.precept));
                        }
                    }
                    return Option(unforbbided);
                }));
            }
        }

        private List<Thing> Consumable()
        {
            return mySAL.GetAllTargetCells()
                .SelectMany(c => c.AllThingsInCellForUse(this.map)) // Use GatherThingsUtility to also grab from belts
                .Distinct<Thing>().ToList();
        }

        public override void Reset(WorkingState workingState)
        {
            if (workingState == WorkingState.Working)
            {

                if (this.unfinished == null)
                {
                    this.ingredients.ForEach(t => GenPlace.TryPlaceThing(t, Position, this.map, ThingPlaceMode.Near));
                }
                else
                {
                    GenPlace.TryPlaceThing(this.unfinished, Position, this.map, ThingPlaceMode.Near);
                    this.unfinished.Destroy(DestroyMode.Cancel);
                }
            }

            this.bill = null;
            this.dominant = null;
            this.unfinished = null;
            this.ingredients = null;

            base.Reset(workingState);
        }

        public override void CreateWorkingEffect(MapTickManager mapTickManager)
        {

            workingEffect = this.bill.recipe.effectWorking?.Spawn();

            workingSound = this.bill.recipe.soundWorking?.TrySpawnSustainer(my_workTable);
            workingSound?.Maintain();

            mapTickManager.EachTickAction(EffectTick);

            base.CreateWorkingEffect(mapTickManager);
        }
        public override void CleanupWorkingEffect(MapTickManager mapTickManager)
        {
            workingEffect?.Cleanup();
            workingEffect = null;

            workingSound?.End();
            workingSound = null;

            mapTickManager.RemoveEachTickAction(this.EffectTick);
            base.CleanupWorkingEffect(mapTickManager);
        }
        protected bool EffectTick()
        {
            workingEffect?.EffectTick(new TargetInfo(mySAL), new TargetInfo(my_workTable));

            return this.workingEffect == null;
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
                        if (i.filter.Allows(thing) && (bill.ingredientFilter.Allows(thing) || i.IsFixedIngredient) && !this.map.reservationManager.AllReservedThings().Contains(thing))
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
                if (!bill.recipe.skillRequirements?.All(r => r.minLevel <= mySAL.GetSkillLevel(r.skill)) ?? false) continue;

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

        public override void WorkDone(out List<Thing> products)
        {

            products = GenRecipe2.MakeRecipeProducts(this.bill.recipe, mySAL, this.ingredients, this.dominant, my_workTable, this.bill.precept).ToList();

            this.ingredients.ForEach(i => bill.recipe.Worker.ConsumeIngredient(i, bill.recipe, map));
            Option(this.unfinished).ForEach(u => u.Destroy(DestroyMode.Vanish));
            this.bill.Notify_IterationCompleted(null, this.ingredients);

            this.bill = null;
            this.dominant = null;
            this.unfinished = null;
            this.ingredients = null;
            // Because we use custom GenRecipe2, we have to handle bonus items and product modifications directly:
            mySAL.ModifyProductExt?.ProcessProducts(products, this as IBillGiver, mySAL, this.bill.recipe); // this as IBillGiver is probably null

        }

        public override bool TryStartWork(out float workAmount)
        {

            var consumable = Consumable();

            List<ThingAmount> things;

            Bill nextbill = GetnextBill(consumable, out things);
            if (nextbill != null)
            {
                this.bill = nextbill;

                this.ingredients = things?.Where(t => t.count > 0).Select(t => t.thing.SplitOff(t.count))?.ToList() ?? new List<Thing>();

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
                        compColorable.SetColor(this.dominant.DrawColor);
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

        public override TargetInfo TargetInfo()
        {
            return my_workTable;
        }

        /// <summary>
        /// Used to reseve the workbench
        /// </summary>
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
        /// <summary>
        /// Used to reseve the workbench
        /// </summary>
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
    public class SAL_TargetResearch : SAL_TargetBench
    {
        private Building_ResearchBench researchBench;
        public SAL_TargetResearch(Building_AutoMachineTool mySAL, IntVec3 position, Map map, Rot4 rotation, Building_ResearchBench researchBench) : base(mySAL, position, map, rotation)
        {
            this.researchBench = researchBench;
        }
        public override bool Ready()
        {
            return !(Find.ResearchManager.currentProj == null || !Find.ResearchManager.currentProj.CanBeResearchedAt(researchBench, false));
        }
        public override void Free()
        {
            base.generalRelease(researchBench);
        }
        public override void Reserve()
        {
            base.generalReserve(researchBench);
        }

        public override void WorkDone(out List<Thing> products)
        {
            products = new List<Thing>();
            if (Find.ResearchManager.currentProj != null)
            {
                float statValue = Mathf.Max(mySAL.powerWorkSetting.GetSpeedFactor() * (mySAL.GetSkillLevel(SkillDefOf.Intellectual) * 0.115f + 0.08f), 0.1f);
                statValue *= researchBench.GetStatValue(StatDefOf.ResearchSpeedFactor);

                statValue /= Find.ResearchManager.currentProj.CostFactor(Faction.OfPlayer.def.techLevel);
                //Multiplier set to 100 instead of 1000 as the speedf factor is so powerfull (would be way too fast)
                statValue *= 100;

                Find.ResearchManager.ResearchPerformed(statValue, null);
            }
        }

        public override bool TryStartWork(out float workAmount)
        {
            workAmount = 0;
            if (Find.ResearchManager.currentProj != null)
            {
                workAmount = 1000f;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override TargetInfo TargetInfo()
        {
            return researchBench;
        }
    }
    public class SAL_TargetDeepDrill : SAL_TargetBench
    {
        private Building drilltypeBuilding;
        private CompDeepDrill compDeepDrill;

        //Based Upon Vanilla but capped at 1 to reduce unessesary calculations
        private readonly float[] miningyieldfactors = { 0.6f, 0.7f, 0.8f, 0.85f, 0.9f, 0.925f, 0.95f, 0.975f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        private const float DeepDrill_WorkAmount = 1000f;

        public SAL_TargetDeepDrill(Building_AutoMachineTool mySAL, IntVec3 position, Map map, Rot4 rotation, Building drilltypeBuilding) : base(mySAL, position, map, rotation)
        {
            this.drilltypeBuilding = drilltypeBuilding;
            this.compDeepDrill ??= drilltypeBuilding.TryGetComp<CompDeepDrill>();
        }
        public override bool Ready()
        {
            return !(drilltypeBuilding.TryGetComp<CompDeepDrill>().CanDrillNow() == false || (drilltypeBuilding.GetComp<CompForbiddable>()?.Forbidden ?? false));
        }
        public override void Free()
        {
            base.generalRelease(drilltypeBuilding);
        }
        public override void Reserve()
        {
            base.generalReserve(drilltypeBuilding);
        }

        public override void WorkDone(out List<Thing> products)
        {
            products = new List<Thing>();
            // From my understanding this WorkDone is added each pawn.tick
            //We dont want this with reflection so i will use a multiplier instead --> DeepDrill_WorkAmount

            CompDeepDrill compDeepDrill = drilltypeBuilding.TryGetComp<CompDeepDrill>();

            //Vanilla Mining Speed Calc may need an Update if Vanilla is Updated 
            float statValue = DeepDrill_WorkAmount * Mathf.Max(mySAL.powerWorkSetting.GetSpeedFactor() * (mySAL.GetSkillLevel(SkillDefOf.Mining) * 0.12f + 0.04f), 0.1f);

            ReflectionUtility.drill_portionProgress.SetValue(compDeepDrill, (float)ReflectionUtility.drill_portionProgress.GetValue(compDeepDrill) + statValue);
            ReflectionUtility.drill_portionYieldPct.SetValue(compDeepDrill, (float)ReflectionUtility.drill_portionYieldPct.GetValue(compDeepDrill) + statValue * miningyieldfactors[mySAL.GetSkillLevel(SkillDefOf.Mining)] / 10000f);
            ReflectionUtility.drill_lastUsedTick.SetValue(compDeepDrill, Find.TickManager.TicksGame);
            if ((float)ReflectionUtility.drill_portionProgress.GetValue(compDeepDrill) > 10000f)
            {
                ReflectionUtility.drill_TryProducePortion.Invoke(compDeepDrill, new object[] { ReflectionUtility.drill_portionYieldPct.GetValue(compDeepDrill), null });
                ReflectionUtility.drill_portionProgress.SetValue(compDeepDrill, 0);
                ReflectionUtility.drill_portionYieldPct.SetValue(compDeepDrill, 0);
            }
        }

        public override bool TryStartWork(out float workAmount)
        {
            workAmount = 0;
            compDeepDrill ??= drilltypeBuilding.TryGetComp<CompDeepDrill>();
            if (compDeepDrill.CanDrillNow())
            {
                workAmount = DeepDrill_WorkAmount;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override TargetInfo TargetInfo()
        {
            return drilltypeBuilding;
        }
    }

    public class Building_AutoMachineTool : Building_BaseRange<Building_AutoMachineTool>, IRecipeProductWorker
    {

        public override bool ProductLimitationDisable => true;

        public Building_AutoMachineTool()
        {
            this.forcePlace = false;
            this.targetEnumrationCount = 0;
        }
        
        private bool forbidItem = false;

        private SAL_TargetBench salTarget;

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
            Scribe_Deep.Look<SAL_TargetBench>(ref this.salTarget, "salTarget");
        }

        public bool GetTarget()
        {
            bool verdict = GetTarget(this.Position, this.Rotation, this.Map ,true);
            //Alter visuals based on the target
            if (verdict && !(salTarget is SAL_TargetWorktable))
            {
                this.compOutputAdjustable.Visible = false;
                this.powerWorkSetting.RangeSettingHide = true;
            }
            else if (verdict)
            {
                this.compOutputAdjustable.Visible = true;
                this.powerWorkSetting.RangeSettingHide = false;
            }


            return verdict;
        }
        public bool GetTarget(IntVec3 pos, Rot4 rot , Map map , bool spawned = false)
        {

            var buildings = (pos + rot.FacingCell).GetThingList(map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t.InteractionCell == pos);

            Building_WorkTable new_my_workTable = (Building_WorkTable)buildings
                .Where(t => t is Building_WorkTable)
                .FirstOrDefault();
            Building new_drilltypeBuilding = (Building)buildings
                .Where(t => t is Building && t.TryGetComp<CompDeepDrill>() != null)
                .FirstOrDefault();
            Building_ResearchBench new_researchBench = (Building_ResearchBench)buildings
                .Where(t => t is Building_ResearchBench)
                .FirstOrDefault();
            
            if (spawned)
            {
                if((salTarget is SAL_TargetWorktable && new_my_workTable == null) || (salTarget is SAL_TargetResearch && new_researchBench == null) || (salTarget is SAL_TargetDeepDrill && new_drilltypeBuilding == null))
                {
                    salTarget.Free();
                }
            }
            if(new_my_workTable != null)
            {
                salTarget = new SAL_TargetWorktable(this, this.Position, this.Map, this.Rotation, new_my_workTable);
            }
            else if (new_drilltypeBuilding != null)
            {
                salTarget = new SAL_TargetDeepDrill(this, this.Position, this.Map, this.Rotation, new_drilltypeBuilding);
            }
            else if (new_researchBench != null)
            {
                salTarget = new SAL_TargetResearch(this, this.Position, this.Map, this.Rotation, new_researchBench);
            }
            else
            {
                salTarget = null;
            }
            
            if (spawned && salTarget != null) salTarget.Reserve();

            return salTarget != null;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            GetTarget();
            extension_Skills = def.GetModExtension<ModExtension_Skills>();

        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            salTarget.Free();

            base.DeSpawn();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            this.WorkTableSetting();
        }

        protected override void Reset()
        {
            salTarget.Reset(this.State);
           
            base.Reset();
        }

        protected override void CleanupWorkingEffect()
        {
            base.CleanupWorkingEffect();
            salTarget.CleanupWorkingEffect(this.MapManager);
        }

        protected override void CreateWorkingEffect()
        {
            salTarget.CreateWorkingEffect(this.MapManager);
        }

        

        protected override TargetInfo ProgressBarTarget()
        {
            return salTarget?.TargetInfo() ?? TargetInfo.Invalid;
        }

        /// <summary>
        /// TODO Check that one again
        /// </summary>
        private void WorkTableSetting()
        {
            GetTarget();
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
            //Return if not ready
            if (!salTarget.Ready()) return false;
            var res = salTarget.TryStartWork(out workAmount);
            return res;
        }

        protected override bool FinishWorking(Building_AutoMachineTool working, out List<Thing> products)
        {
            salTarget.WorkDone(out products);
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

        public override IEnumerable<InspectTabBase> GetInspectTabs()
        {
            return base.GetInspectTabs();
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


        private Building_WorkTable GetmyTragetWorktable()
        {
            return (Building_WorkTable)this.FacingCell().GetThingList(Map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t is Building_WorkTable)
                .Where(t => t.InteractionCell == this.Position).FirstOrDefault();
        }

        protected override bool WorkInterruption(Building_AutoMachineTool working)
        {
            //Interupt if worktable chenged or is null
            if (salTarget == null || (salTarget is SAL_TargetWorktable && GetmyTragetWorktable() == null /*|| GetmyTragetWorktable() != my_workTable*/))
            {
                return true;
            }
            //Interrupt if worktable is not ready for work
            //if (my_workTable != null) return !my_workTable.CurrentlyUsableForBills();
            
            var notready = !salTarget.Ready();
            return notready;
        }

    }

}
