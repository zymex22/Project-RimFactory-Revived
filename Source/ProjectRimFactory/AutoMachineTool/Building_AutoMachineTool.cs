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

    public class PRF_SAL_Trarget
    {
        //only tep as public
        public Building_WorkTable my_workTable = null;
        private Building drilltypeBuilding = null;
        private Building_ResearchBench researchBench = null;
        private Building_AutoMachineTool mySAL = null;


        private IntVec3 Position = new IntVec3();
        private Map Map;
        private Rot4 Rotation;


        public PRF_SAL_Trarget(Map map, IntVec3 cell, Rot4 rot, Building_AutoMachineTool sal)
        {
            Map = map;
            Position = cell;
            Rotation = rot;
            mySAL = sal;
        }


        public bool ValidTarget => my_workTable != null || drilltypeBuilding != null || researchBench != null;


        public bool GetTarget()
        {
            return GetTarget(this.Position, this.Rotation,true);



        }

        public bool GetTarget(IntVec3 pos, Rot4 rot , bool spawned = false)
        {

            Building_WorkTable new_my_workTable = (Building_WorkTable)(pos + rot.FacingCell).GetThingList(Map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t is Building_WorkTable)
                .Where(t => t.InteractionCell == this.Position).FirstOrDefault();
                Building new_drilltypeBuilding = (Building)(pos + rot.FacingCell).GetThingList(Map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t is Building && t.TryGetComp<CompDeepDrill>() != null)
                .Where(t => t.InteractionCell == this.Position).FirstOrDefault();
            Building_ResearchBench new_researchBench = (Building_ResearchBench)(pos + rot.FacingCell).GetThingList(Map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t is Building_ResearchBench)
                .Where(t => t.InteractionCell == this.Position).FirstOrDefault();
            if (spawned && ((my_workTable != null && new_my_workTable == null) || (researchBench != null && new_researchBench == null) || (drilltypeBuilding != null && new_drilltypeBuilding == null)))
            {
                FreeTarget();
            }
            my_workTable = new_my_workTable;
            drilltypeBuilding = new_drilltypeBuilding;
            researchBench = new_researchBench;
            if (spawned && ValidTarget) ReserveTraget();


            return ValidTarget;

        }

        /// <summary>
        /// Return True if the Traget is Ready for work
        /// </summary>
        /// <returns></returns>
        public bool TrargetReady()
        {
            //no target --> not ready
            if (!ValidTarget) return false;

            if ((my_workTable != null && (!my_workTable.CurrentlyUsableForBills() || !my_workTable.billStack.AnyShouldDoNow) ) ||
                (researchBench != null && (Find.ResearchManager.currentProj == null || !Find.ResearchManager.currentProj.CanBeResearchedAt(researchBench,false) )) ||
                (drilltypeBuilding != null && drilltypeBuilding.TryGetComp<CompDeepDrill>().CanDrillNow() == false ) 
                )
            {
                return false;
            }
            return true;
        }

        //TODO
        public void ReserveTraget()
        {
            if (my_workTable != null) ForbidBills();
            if (researchBench != null) generalReserve();
            if (drilltypeBuilding != null) generalReserve();

        }
        //TODO
        public void FreeTarget()
        {
            if (my_workTable != null) AllowBills();
            if (researchBench != null) generalRelease();
            if (drilltypeBuilding != null) generalRelease();
        }


        private void generalReserve()
        {
            if (PRFGameComponent.PRF_StaticPawn == null) PRFGameComponent.GenStaticPawn();
            if (PRFGameComponent.PRF_StaticJob == null) PRFGameComponent.PRF_StaticJob = new Job(PRFDefOf.PRFStaticJob);

            Building tb = researchBench ?? drilltypeBuilding;

            List<ReservationManager.Reservation> reservations;
            reservations = (List<ReservationManager.Reservation>)ReflectionUtility.sal_reservations.GetValue(Map.reservationManager);
            var res = new ReservationManager.Reservation(PRFGameComponent.PRF_StaticPawn, PRFGameComponent.PRF_StaticJob, 1, -1, tb/*(Position + Rotation.FacingCell)*/, null);

            if (!reservations.Where(r => r.Claimant == PRFGameComponent.PRF_StaticPawn && r.Job == PRFGameComponent.PRF_StaticJob && r.Target == tb).Any()) reservations.Add(res);
            ReflectionUtility.sal_reservations.SetValue(Map.reservationManager, reservations);

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

        private void generalRelease()
        {
            if (PRFGameComponent.PRF_StaticPawn == null) PRFGameComponent.GenStaticPawn();
            if (PRFGameComponent.PRF_StaticJob == null) PRFGameComponent.PRF_StaticJob = new Job(PRFDefOf.PRFStaticJob);

            Building tb = researchBench ?? drilltypeBuilding;
            
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

            Map.reservationManager.Release(tb, PRFGameComponent.PRF_StaticPawn, PRFGameComponent.PRF_StaticJob);
            //Log.Message("generalRelease for " + (Position + Rotation.FacingCell) );
        }


        #region WorkTableReserve

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

        #endregion

        /*
        public Effecter GetEffecter()
        {
            if (my_workTable != null)
            {
                return this.bill.recipe.effectWorking.Spawn();
            }
            return null;
        } 
        public Sustainer GetSustainer()
        {
            if (my_workTable != null)
            {
                return this.bill.recipe.soundWorking?.TrySpawnSustainer(my_workTable);
            }
            return null;
        }
        */

        //Based Upon Vanilla but capped at 1 to reduce unessesary calculations
        private readonly float[] miningyieldfactors = { 0.6f, 0.7f, 0.8f, 0.85f, 0.9f, 0.925f, 0.95f, 0.975f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        public void SignalWorkDone()
        {
            if (drilltypeBuilding != null)
            {

                CompDeepDrill compDeepDrill = drilltypeBuilding.TryGetComp<CompDeepDrill>();

                //Vanilla Mining Speed Calc may need an Update if Vanilla is Updated 
                float statValue = Mathf.Max( mySAL.powerWorkSetting.GetSpeedFactor() * (mySAL.GetSkillLevel(SkillDefOf.Mining) * 0.12f + 0.04f), 0.1f);

                ReflectionUtility.drill_portionProgress.SetValue(compDeepDrill, (float)ReflectionUtility.drill_portionProgress.GetValue(compDeepDrill) + statValue);
                ReflectionUtility.drill_portionYieldPct.SetValue(compDeepDrill, (float)ReflectionUtility.drill_portionYieldPct.GetValue(compDeepDrill) + statValue * miningyieldfactors[mySAL.GetSkillLevel(SkillDefOf.Mining)] / 10000f);
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

                float statValue = Mathf.Max(mySAL.powerWorkSetting.GetSpeedFactor() * (mySAL.GetSkillLevel(SkillDefOf.Intellectual) * 0.115f + 0.08f), 0.1f);
                statValue *= researchBench.GetStatValue(StatDefOf.ResearchSpeedFactor);

                statValue /= Find.ResearchManager.currentProj.CostFactor(Faction.OfPlayer.def.techLevel);
                Find.ResearchManager.ResearchPerformed(statValue, null);

            }

        }
        public bool TryStartWork( out float workAmount)
        {
            workAmount = 0;
            if (drilltypeBuilding != null)
            {
                CompDeepDrill compDeepDrill = drilltypeBuilding.TryGetComp<CompDeepDrill>();
                if (compDeepDrill.CanDrillNow())
                {
                    // Log.Message("Started Drill Bill");
                    workAmount = 1000000f;
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
                    workAmount = 1000000f;
                    return true;
                }
                else
                {
                    return false;
                }

            }

            return false;
        }




    }



    public class Building_AutoMachineTool : Building_BaseRange<Building_AutoMachineTool>, IRecipeProductWorker
    {
  
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

        private PRF_SAL_Trarget salTarget;

        private Building_WorkTable my_workTable = null;


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
            salTarget = new PRF_SAL_Trarget(map, Position, Rotation,this);
            my_workTable = null;
            extension_Skills = def.GetModExtension<ModExtension_Skills>();

        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            salTarget.FreeTarget();

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
            salTarget.SignalWorkDone();


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

        protected override TargetInfo ProgressBarTarget()
        {
            return my_workTable;
        }

        /// <summary>
        /// TODO Check that one again
        /// </summary>
        private void WorkTableSetting()
        {
            salTarget.GetTarget();
            my_workTable = salTarget.my_workTable;
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
            if (!salTarget.TrargetReady()) return false;

            if (my_workTable == null)
            {
                float val = 0;
                bool status = salTarget.TryStartWork(out val);
                workAmount = val;
                Log.Message("Started with " + workAmount);
                return status;

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
