using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectRimFactory.AutoMachineTool;
using ProjectRimFactory.Common;
using ProjectRimFactory.Common.HarmonyPatches;
using ProjectRimFactory.SAL3.Exposables;
using ProjectRimFactory.SAL3.Tools;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public abstract class Building_ProgrammableAssembler : Building_DynamicBillGiver, IPowerSupplyMachineHolder,
        IAssemblerQueue
    {
        //max number of items that can be stored in thingQueue before production is halted
        protected const int max_thingQueue_Count = 100;

        // Settings
        protected bool allowForbidden; // internal variable

        // Misc
        public BillStack billStack;
        // Pawn

        public Pawn buildingPawn;
        protected CompFlickable compFlick;
        protected CompPowerTrader compPowerTrader;
        protected CompRefuelable compRefuelable;

        // (Some) Internal variables:
        // Logic
        protected BillReport currentBillReport;


        [Unsaved] private Effecter effecter;

        [Unsaved] private Sustainer sound;

        // thingQueue is List to save properly
        //   List of produced things, waiting to be placed:
        protected List<Thing> thingQueue = new List<Thing>();

        public Building_ProgrammableAssembler()
        {
            billStack = new BillStack(this);
        }

        public override Graphic Graphic => Active
            ? currentBillReport == null
                // powered on but idle:
                ? base.Graphic
                // powered on and working:
                : def.GetModExtension<AssemblerDefModExtension>()?.WorkingGrahic ?? base.Graphic
            // not powered on:
            : def.GetModExtension<AssemblerDefModExtension>()?.PowerOffGrahic ?? base.Graphic;

        public bool DrawStatus => def.GetModExtension<AssemblerDefModExtension>()?.drawStatus ?? true;


        public ModExtension_BonusYield modExtension_BonusYield => def.GetModExtension<ModExtension_BonusYield>();
        public override BillStack BillStack => billStack;

        public CompOutputAdjustable OutputComp => GetComp<CompOutputAdjustable>();

        protected MapTickManager MapManager { get; private set; }

        protected virtual bool Active => compPowerTrader?.PowerOn != false
                                         && compRefuelable?.HasFuel != false
                                         && compFlick?.SwitchIsOn != false;

        protected IEnumerable<Thing> AllAccessibleThings => from t in AllThingsInArea
            where AllowForbidden || !t.IsForbidden(Faction)
            select t;

        protected IEnumerable<Thing> AllThingsInArea
        {
            get
            {
                foreach (var c in IngredientStackCells)
                foreach (var t in Map.thingGrid.ThingsListAt(c))
                    if (t is Building && t is IThingHolder holder)
                    {
                        if (holder.GetDirectlyHeldThings() is ThingOwner<Thing> owner)
                            foreach (var moreT in owner.InnerListForReading)
                                yield return moreT;
                    }
                    else if (t.def.category == ThingCategory.Item)
                    {
                        yield return t;
                    }
            }
        }


        /*=> from c in IngredientStackCells
                                                        from t in Map.thingGrid.ThingsListAt(c)
                                                        where (AllowForbidden || !t.IsForbidden(Faction)) && t.def.category == ThingCategory.Item
                                                        select t;*/
        protected IEnumerable<Bill> AllBillsShouldDoNow => from b in billStack.Bills
            where b.ShouldDoNow()
            select b;

        private ModExtension_Skills extension_Skills => def.GetModExtension<ModExtension_Skills>();
        public virtual bool AllowForbidden => allowForbidden;

        protected virtual float ProductionSpeedFactor =>
            def.GetModExtension<AssemblerDefModExtension>()?.workSpeedBaseFactor ?? 1f;

        protected bool AllowProduction_thingQueue => thingQueue.Count < max_thingQueue_Count;

        public List<Thing> GetThingQueue()
        {
            return thingQueue;
        }

        public IPowerSupplyMachine RangePowerSupplyMachine => GetComp<CompPowerWorkSetting>();

        public virtual void DoPawn()
        {
            try
            {
                var p = PawnGenerator.GeneratePawn(PRFDefOf.PRFSlavePawn, Faction.OfPlayer);
                p.Name = new NameTriple("...", Label ?? "SAL_Name".Translate(), "...");
                //Assign skills
                foreach (var s in p.skills.skills)
                    s.Level = extension_Skills.GetExtendedSkillLevel(s.def, typeof(Building_ProgrammableAssembler));

                // This ensures that pawns do not end up with disabled work types - see Issue#54
                ReflectionUtility.cachedDisabledWorkTypesPermanent.SetValue(p, new List<WorkTypeDef>());


                //Assign Pawn's mapIndexOrState to building's mapIndexOrState
                ReflectionUtility.mapIndexOrState.SetValue(p, ReflectionUtility.mapIndexOrState.GetValue(this));

                //Assign Pawn's position without nasty errors
                p.SetPositionDirect(PositionHeld);

                //Clear pawn relations
                p.relations.ClearAllRelations();

                //Set backstories
                SetBackstoryAndSkills(p);

                //Pawn work-related stuffs
                for (var i = 0; i < 24; i++) p.timetable.SetAssignment(i, TimeAssignmentDefOf.Work);
                buildingPawn = p;
            }
            catch (Exception ex)
            {
                Log.Error("ERROR=: " + ex);
            }
        }

        private static void SetBackstoryAndSkills(Pawn p)
        {
            if (BackstoryDatabase.TryGetWithIdentifier("ChildSpy47", out var bs))
                p.story.childhood = bs;
            else
                Log.Error("Tried to assign child backstory ChildSpy47, but not found");
            if (BackstoryDatabase.TryGetWithIdentifier("ColonySettler53", out var bstory))
                p.story.adulthood = bstory;
            else
                Log.Error("Tried to assign child backstory ColonySettler53, but not found");
            //Clear traits

            p.story.traits.allTraits = new List<Trait>();
            //Reset cache

            //ReflectionUtility.cachedDisabledWorkTypes.SetValue(p.story, null);
            //Reset cache for each skill

            for (var i = 0; i < p.skills.skills.Count; i++)
                ReflectionUtility.cachedTotallyDisabled.SetValue(p.skills.skills[i], BoolUnknown.Unknown);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref billStack, "bills", this);
            Scribe_Deep.Look(ref currentBillReport, "currentBillReport");
            Scribe_Collections.Look(ref thingQueue, "thingQueue", LookMode.Deep);
            Scribe_Values.Look(ref allowForbidden, "allowForbidden");
            Scribe_Deep.Look(ref buildingPawn, "buildingPawn");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
            yield return new Command_Toggle
            {
                defaultLabel = "SALToggleForbidden".Translate(),
                defaultDesc = "SALToggleForbidden_Desc".Translate(),
                isActive = () => allowForbidden,
                toggleAction = () => allowForbidden ^= true,
                icon = TexCommand.ForbidOff
            };
            if (Prefs.DevMode)
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Debug actions",
                    action = () => Find.WindowStack.Add(new FloatMenu(GetDebugOptions().ToList()))
                };
        }

        protected virtual IEnumerable<FloatMenuOption> GetDebugOptions()
        {
            string StringConverter(Thing t)
            {
                return t.GetUniqueLoadID();
            }

            yield return new FloatMenuOption("View selected things", () =>
            {
                if (currentBillReport != null)
                    Log.Message("Selected things: " +
                                string.Join(", ", currentBillReport.selected.Select(StringConverter).ToArray()));
            });
            yield return new FloatMenuOption("View all items available for input",
                () => { Log.Message(string.Join(", ", AllAccessibleThings.Select(StringConverter).ToArray())); });
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            MapManager = map.GetComponent<MapTickManager>();
            if (buildingPawn == null)
                DoPawn();

            buildingPawn?.skills.skills.ForEach(s =>
            {
                s.Level = extension_Skills.GetExtendedSkillLevel(s.def, typeof(Building_ProgrammableAssembler));
                ReflectionUtility.cachedTotallyDisabled.SetValue(s, BoolUnknown.Unknown);
            });

            compPowerTrader = GetComp<CompPowerTrader>();
            compRefuelable = GetComp<CompRefuelable>();
            compFlick = GetComp<CompFlickable>();

            //Assign Pawn's mapIndexOrState to building's mapIndexOrState
            ReflectionUtility.mapIndexOrState.SetValue(buildingPawn, ReflectionUtility.mapIndexOrState.GetValue(this));
            //Assign Pawn's position without nasty errors
            buildingPawn.SetPositionDirect(Position);

            //Need this type of call to set the Powerconsumption on load
            //A normal call will not work
            var rangePowerSupplyMachine = RangePowerSupplyMachine;
            if (rangePowerSupplyMachine != null)
            {
                MapManager.NextAction(rangePowerSupplyMachine.RefreshPowerStatus);
                MapManager.AfterAction(5, rangePowerSupplyMachine.RefreshPowerStatus);
            }

            PRFGameComponent.RegisterAssemblerQueue(this);
        }

        public override Thing GetThingBy(Func<Thing, bool> optionalValidator = null)
        {
            foreach (var t in thingQueue)
                if (optionalValidator == null ||
                    optionalValidator(t))
                {
                    thingQueue.Remove(t);
                    return t;
                }

            return null;
        }

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(10) && Active)
            {
                if (thingQueue.Count > 0 &&
                    this.PRFTryPlaceThing(thingQueue[0],
                        OutputComp.CurrentCell, Map))
                    thingQueue.RemoveAt(0);
                if (currentBillReport != null)
                {
                    //Update the Required Work
                    currentBillReport.workLeft -= 10f * ProductionSpeedFactor *
                                                  (this.TryGetComp<CompPowerWorkSetting>()?.GetSpeedFactor() ?? 1f);
                    //If Work Finished
                    if (currentBillReport.workLeft <= 0)
                        try
                        {
                            ProduceItems();
                            currentBillReport.bill.Notify_IterationCompleted(buildingPawn, currentBillReport.selected);
                            Notify_RecipeCompleted(currentBillReport.bill.recipe);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error producing items for {GetUniqueLoadID()}: " + ex);
                        }
                        finally
                        {
                            currentBillReport = null;
                        }
                }
                else if (this.IsHashIntervalTick(60) && AllowProduction_thingQueue)
                {
                    //Start Bill if Possible
                    if ((currentBillReport = TryGetNextBill()) != null) Notify_BillStarted();
                }
            }

            // Effect.
            if (currentBillReport != null && Active)
            {
                var ext = def.GetModExtension<AssemblerDefModExtension>();
                if (effecter == null)
                    effecter = (ext == null
                        ? currentBillReport.bill.recipe?.effectWorking
                        : ext.GetEffecter(currentBillReport.bill.recipe))?.Spawn();
                if (sound == null)
                    sound = (ext == null
                        ? currentBillReport.bill.recipe?.soundWorking
                        : ext.GetSound(currentBillReport.bill.recipe))?.TrySpawnSustainer(this);
                effecter?.EffectTick(this, this);
                sound?.SustainerUpdate();
                if (GetComp<CompGlowerPulse>() != null) GetComp<CompGlowerPulse>().Glows = true;
            }
            else
            {
                if (effecter != null)
                {
                    effecter.Cleanup();
                    effecter = null;
                }

                if (sound != null)
                {
                    sound.End();
                    sound = null;
                }

                if (GetComp<CompGlowerPulse>() != null) GetComp<CompGlowerPulse>().Glows = false;
            }

            //Fuel
            if (compRefuelable != null && Active && currentBillReport != null) compRefuelable.Notify_UsedThisTick();
        }

        // TryGetNextBill returns a new BillReport to start if one is available
        protected BillReport TryGetNextBill()
        {
            foreach (var b in AllBillsShouldDoNow)
            {
                var chosen = new List<ThingCount>();

                var allAccessibleAllowedThings =
                    AllAccessibleThings.Where(x => b.IsFixedOrAllowedIngredient(x)).ToList();

                if (allAccessibleAllowedThings.Count > 0 || b.ingredientFilter.AllowedThingDefs.Count() == 0)
                    if (TryFindBestBillIngredientsInSet(allAccessibleAllowedThings, b, chosen))
                        return new BillReport(b, (from ta in chosen select ta.Thing.SplitOff(ta.Count)).ToList());
            }

            return null;
        }

        private bool TryFindBestBillIngredientsInSet(List<Thing> accessibleThings, Bill b, List<ThingCount> chosen)
        {
            ReflectionUtility.MakeIngredientsListInProcessingOrder.Invoke(null,
                new[] {ReflectionUtility.ingredientsOrdered.GetValue(null), b});
            //TryFindBestBillIngredientsInSet Expects a List of Both Avilibale & Allowed Things as "accessibleThings"
            return (bool) ReflectionUtility.TryFindBestBillIngredientsInSet.Invoke(null,
                new object[] {accessibleThings, b, chosen, new IntVec3(), false});
        }

        protected virtual void ProduceItems()
        {
            if (currentBillReport == null)
            {
                Log.Error("Project RimFactory :: Tried to make products when assembler isn't engaged in a bill.");
                return;
            }

            var products = GenRecipe.MakeRecipeProducts(currentBillReport.bill.recipe, buildingPawn,
                currentBillReport.selected,
                ProjectSAL_Utilities.CalculateDominantIngredient(currentBillReport.bill.recipe,
                    currentBillReport.selected), this);
            foreach (var thing in products)
            {
                PostProcessRecipeProduct(thing);
                thingQueue.Add(thing);
            }

            for (var i = 0; i < currentBillReport.selected.Count; i++)
            {
                if (currentBillReport.selected[i] is Corpse c && c.InnerPawn?.apparel != null)
                {
                    var apparel = new List<Apparel>(c.InnerPawn.apparel.WornApparel);
                    for (var j = 0; j < apparel.Count; j++)
                    {
                        thingQueue.Add(apparel[j]);
                        c.InnerPawn.apparel.Remove(apparel[j]);
                    }
                }

                currentBillReport.bill.recipe.Worker.ConsumeIngredient(currentBillReport.selected[i],
                    currentBillReport.bill.recipe, Map);
            }

            //Bonus
            var bonus = modExtension_BonusYield?.GetBonusYield(currentBillReport.bill.recipe, QualityCategory.Normal) ??
                        null;
            if (bonus != null) thingQueue.Add(bonus);

            thingQueue.AddRange(from Thing t in currentBillReport.selected where t.Spawned select t);
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            if (Active)
            {
                if (currentBillReport == null)
                {
                    // assembler is not working despite power:
                    // show why it is not working:
                    if (BillStack.AnyShouldDoNow) // it DOES have bills
                        stringBuilder.AppendLine("SearchingForIngredients".Translate());
                    else // it DOESN'T have bills:
                        stringBuilder.AppendLine("AssemblerNoBills".Translate());
                }
                else
                {
                    // assembler is working
                    stringBuilder.AppendLine("SAL3_BillReport".Translate(currentBillReport.bill.Label,
                        currentBillReport.workLeft.ToStringWorkAmount()));
                }
            }

            // even if it's not active, show any products ready to place:
            //   (we always show this: even if 0 products, it lets new players
            //    know it will hold products until it CAN place them)
            stringBuilder.AppendLine("SAL3_Products".Translate(thingQueue.Count, max_thingQueue_Count));
            return stringBuilder.ToString().TrimEndNewlines();
        }

        protected virtual bool SatisfiesSkillRequirements(RecipeDef recipe)
        {
            if (buildingPawn != null && buildingPawn.skills != null)
                return recipe.PawnSatisfiesSkillRequirements(buildingPawn);
            return recipe.skillRequirements?.All(s =>
                s.minLevel <=
                extension_Skills.GetExtendedSkillLevel(s.skill, typeof(Building_ProgrammableAssembler))) ?? true;
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (GetComp<CompPowerWorkSetting>() == null) GenDraw.DrawFieldEdges(IngredientStackCells.ToList());
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (DrawStatus && Find.CameraDriver.CurrentZoom < CameraZoomRange.Middle)
            {
                var label = "";
                var label2 = "";
                // only show overlay status text if has power:
                if (Active)
                {
                    if (currentBillReport != null) // the assembler is actively working
                    {
                        // set the status text to the bill's label:
                        label = currentBillReport.bill.LabelCap;
                    }
                    else // the assembler is NOT working
                    {
                        // show why it is not working:
                        if (BillStack.AnyShouldDoNow) // it DOES have bills
                            label = "SearchingForIngredients".Translate();
                        else // it DOESN'T have bills:
                            label = "AssemblerNoBills".Translate();
                    }

                    // draw the label on the screen:
                }
                else if (compFlick?.SwitchIsOn == false)
                {
                    label = "SwitchedOff".Translate();
                }

                if (!AllowProduction_thingQueue)
                    label2 = "PRF_OutputBufferWarning".Translate();
                else if (thingQueue.Count > 1)
                    label2 = "SAL3_Products".Translate(thingQueue.Count, max_thingQueue_Count);
                var vectorpos = GenMapUI.LabelDrawPosFor(this, 0f);
                GenMapUI.DrawThingLabel(vectorpos, label, Color.white);
                vectorpos.y += Text.CalcSize(label).y;


                GenMapUI.DrawThingLabel(vectorpos, label2, Color.yellow);
            }
        }

        // Other virtual methods
        protected virtual void Notify_RecipeCompleted(RecipeDef recipe)
        {
        }

        protected virtual void PostProcessRecipeProduct(Thing thing)
        {
        }

        protected virtual void Notify_BillStarted()
        {
        }

        protected class BillReport : IExposable
        {
            public Bill bill;
            public List<Thing> selected;
            public float workLeft;

            public BillReport()
            {
            }

            public BillReport(Bill b, List<Thing> list)
            {
                bill = b;
                selected = list;
                workLeft = b.recipe.WorkAmountTotal(
                    ProjectSAL_Utilities.CalculateDominantIngredient(b.recipe, list).def);
            }

            public void ExposeData()
            {
                Scribe_References.Look(ref bill, "bill");
                Scribe_Collections.Look(ref selected, "selected", LookMode.Deep);
                Scribe_Values.Look(ref workLeft, "workLeft");
            }
        }
    }
}