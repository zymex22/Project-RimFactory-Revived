using ProjectRimFactory.AutoMachineTool;
using ProjectRimFactory.Common;
using ProjectRimFactory.Common.HarmonyPatches;
using ProjectRimFactory.SAL3.Exposables;
using ProjectRimFactory.SAL3.Tools;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public abstract class Building_ProgrammableAssembler : Building_DynamicBillGiver, IPowerSupplyMachineHolder, IAssemblerQueue
    {
        protected class BillReport : IExposable
        {
            public BillReport()
            {
            }
            public BillReport(Bill b, List<Thing> list)
            {
                bill = b;
                selected = list;
                workLeft = b.recipe.WorkAmountTotal(ProjectSAL_Utilities.CalculateDominantIngredient(b.recipe, list).def);
            }
            public Bill bill;
            public List<Thing> selected;
            public float workLeft;

            public void ExposeData()
            {
                Scribe_References.Look(ref bill, "bill");
                Scribe_Collections.Look(ref selected, "selected", LookMode.Deep);
                Scribe_Values.Look(ref workLeft, "workLeft");
            }
        }

        public override Graphic Graphic => (this.Active)
            ? (this.currentBillReport == null
                // powered on but idle:
                ? base.Graphic
                // powered on and working:
                : this.def.GetModExtension<AssemblerDefModExtension>()?.WorkingGrahic ?? base.Graphic)
            // not powered on:
            : this.def.GetModExtension<AssemblerDefModExtension>()?.PowerOffGrahic ?? base.Graphic;

        public bool DrawStatus => this.def.GetModExtension<AssemblerDefModExtension>()?.drawStatus ?? true;

        // Pawn
        public Pawn buildingPawn;

        public virtual void DoPawn()
        {
            try
            {

                Pawn p = PawnGenerator.GeneratePawn(PRFDefOf.PRFSlavePawn, Faction.OfPlayer);
                p.Name = new NameTriple("...", Label ?? "SAL_Name".Translate(), "...");
                //Assign skills
                foreach (var s in p.skills.skills)
                {
                    s.Level = extension_Skills.GetExtendedSkillLevel(s.def, typeof(Building_ProgrammableAssembler));
                }

                //disable stuff thats not needed
                //also prevents some instances of disabled skills
                p.ideo = null;
                p.genes = null;
                p.royalty = null;
                p.guest = null;

                //Assign Pawn's mapIndexOrState to building's mapIndexOrState
                ReflectionUtility.mapIndexOrState.SetValue(p, ReflectionUtility.mapIndexOrState.GetValue(this));

                //Assign Pawn's position without nasty errors
                p.SetPositionDirect(PositionHeld);

                //Clear pawn relations
                p.relations.ClearAllRelations();

                //Set backstories
                SetBackstoryAndSkills(p);

                //#54 Moved to the end based on testing - now using a function instead of reflection
                p.Notify_DisabledWorkTypesChanged();

                //Pawn work-related stuffs
                for (int i = 0; i < 24; i++)
                {
                    p.timetable.SetAssignment(i, TimeAssignmentDefOf.Work);
                }
                buildingPawn = p;
            }
            catch (Exception ex)
            {
                Log.Error("ERROR=: " + ex.ToString());
            }
        }

        private static void SetBackstoryAndSkills(Pawn p)
        {

            p.story.Childhood = PRFDefOf.ChildSpy47;
            p.story.Adulthood = PRFDefOf.ColonySettler53;

            //Clear traits
            p.story.traits.allTraits = new List<Trait>();
            //Reset cache

            //ReflectionUtility.cachedDisabledWorkTypes.SetValue(p.story, null);
            //Reset cache for each skill

            for (int i = 0; i < p.skills.skills.Count; i++)
            {
                ReflectionUtility.cachedTotallyDisabled.SetValue(p.skills.skills[i], BoolUnknown.Unknown);
            }

        }

        // Misc
        public BillStack billStack;
        public override BillStack BillStack => billStack;
        public Building_ProgrammableAssembler()
        {
            billStack = new BillStack(this);
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
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            yield return new Command_Toggle()
            {
                defaultLabel = "SALToggleForbidden".Translate(),
                defaultDesc = "SALToggleForbidden_Desc".Translate(),
                isActive = () => allowForbidden,
                toggleAction = () => allowForbidden ^= true,
                icon = TexCommand.ForbidOff,
            };
            if (Prefs.DevMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "DEBUG: Debug actions",
                    action = () => Find.WindowStack.Add(new FloatMenu(GetDebugOptions().ToList()))
                };
            }
            yield return new Command_Action
            {
                action = MakeMatchingStockpileZone,
                hotKey = KeyBindingDefOf.Misc1,
                defaultDesc = "DesignatorZoneCreateStorageResourcesDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile"),
                defaultLabel = "PRF_MakeStockpileZoneLabel".Translate()
            };
        }

        private void MakeMatchingStockpileZone()
        {
            var stockpileCells = IngredientStackCells.Where(c => c != OutputComp.CurrentCell && !this.Map.thingGrid.ThingsListAt(c).Any(t => !t.def.CanOverlapZones));
            if (stockpileCells.Count() > 0)
            {
                Designator_ZoneAddStockpile_Resources stockpileZone = new Designator_ZoneAddStockpile_Resources();
                stockpileZone.DesignateMultiCell(stockpileCells);
            }
            else
            {
                Messages.Message("PRF_CantCreateStockpile".Translate(), this, MessageTypeDefOf.CautionInput);
            }
        }


        public CompOutputAdjustable OutputComp
        {
            get
            {
                return GetComp<CompOutputAdjustable>();
            }
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
                {
                    Log.Message("Selected things: " + string.Join(", ", currentBillReport.selected.Select(StringConverter).ToArray()));
                }
            });
            yield return new FloatMenuOption("View all items available for input", () =>
            {
                Log.Message(string.Join(", ", AllAccessibleThings.Select(StringConverter).ToArray()));
            });
            yield break;
        }

        private MapTickManager mapManager;
        protected MapTickManager MapManager => this.mapManager;

        private PRFGameComponent prf_gamecomp = Current.Game.GetComponent<PRFGameComponent>();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.mapManager = map.GetComponent<MapTickManager>();
            if (buildingPawn == null)
                DoPawn();

            this.buildingPawn?.skills.skills.ForEach(s =>
            {
                s.Level = extension_Skills.GetExtendedSkillLevel(s.def, typeof(Building_ProgrammableAssembler));
                ReflectionUtility.cachedTotallyDisabled.SetValue(s, BoolUnknown.Unknown);
            });

            this.compPowerTrader = GetComp<CompPowerTrader>();
            this.compRefuelable = GetComp<CompRefuelable>();
            this.compFlick = GetComp<CompFlickable>();
            this.prf_gamecomp = Current.Game.GetComponent<PRFGameComponent>();

            //Assign Pawn's mapIndexOrState to building's mapIndexOrState
            ReflectionUtility.mapIndexOrState.SetValue(buildingPawn, ReflectionUtility.mapIndexOrState.GetValue(this));
            //Assign Pawn's position without nasty errors
            buildingPawn.SetPositionDirect(Position);

            //Need this type of call to set the Powerconsumption on load
            //A normal call will not work
            var rangePowerSupplyMachine = this.RangePowerSupplyMachine;
            if (rangePowerSupplyMachine != null)
            {
                this.MapManager.NextAction(rangePowerSupplyMachine.RefreshPowerStatus);
                this.MapManager.AfterAction(5, rangePowerSupplyMachine.RefreshPowerStatus);
            }
            prf_gamecomp.RegisterAssemblerQueue(this);

            //Check if the Current Bill still Exists
            if (currentBillReport.bill is null)
            {
                //Refund materials
                foreach (var thing in currentBillReport.selected)
                {
                    if (thing != null)
                    {
                        thing.SpawnSetup(this.Map, false);
                    }
                }

                //Remove Bill
                currentBillReport = null;
            }

        }

        protected virtual bool Active => compPowerTrader?.PowerOn != false
                                       && compRefuelable?.HasFuel != false
                                       && compFlick?.SwitchIsOn != false;

        protected IEnumerable<Thing> AllAccessibleThings => from t in AllThingsInArea
                                                            where (AllowForbidden || !t.IsForbidden(Faction))
                                                            where (!this.Map.reservationManager.AllReservedThings().Contains(t))
                                                            select t;

        protected IEnumerable<Thing> AllThingsInArea
        {
            get
            {
                foreach (var c in IngredientStackCells)
                {
                    foreach (var t in Map.thingGrid.ThingsListAt(c))
                    {
                        if (t is Building && t is IThingHolder holder)
                        {
                            if (holder.GetDirectlyHeldThings() is ThingOwner<Thing> owner)
                            {
                                foreach (var moreT in owner.InnerListForReading) yield return moreT;
                            }
                        }
                        else if (t.def.category == ThingCategory.Item)
                        {
                            yield return t;
                        }
                    }
                }
                yield break;
            }
        }


        /*=> from c in IngredientStackCells
                                                        from t in Map.thingGrid.ThingsListAt(c)
                                                        where (AllowForbidden || !t.IsForbidden(Faction)) && t.def.category == ThingCategory.Item
                                                        select t;*/
        protected IEnumerable<Bill> AllBillsShouldDoNow => from b in billStack.Bills
                                                           where b.ShouldDoNow()
                                                           select b;
        public override Thing GetThingBy(Func<Thing, bool> optionalValidator = null)
        {
            foreach (var t in thingQueue)
            {
                if (optionalValidator == null ||
                    optionalValidator(t))
                {
                    thingQueue.Remove(t);
                    return t;
                }
            }
            return null;
        }

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(10) && Active)
            {
                if (thingQueue.Count > 0 &&
                    PlaceThingUtility.PRFTryPlaceThing(this, thingQueue[0],
                        OutputComp.CurrentCell, Map))
                {
                    thingQueue.RemoveAt(0);
                }
                if (currentBillReport != null)
                {
                    //Update the Required Work
                    currentBillReport.workLeft -= 10f * ProductionSpeedFactor * (this.TryGetComp<CompPowerWorkSetting>()?.GetSpeedFactor() ?? 1f);
                    //If Work Finished
                    if (currentBillReport.workLeft <= 0)
                    {
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
                }
                else if (this.IsHashIntervalTick(60) && AllowProduction_thingQueue)
                {
                    //Start Bill if Possible
                    if ((currentBillReport = TryGetNextBill()) != null)
                    {
                        Notify_BillStarted();
                    }
                }
            }
            // Effect.
            if (currentBillReport != null && Active)
            {
                var ext = this.def.GetModExtension<AssemblerDefModExtension>();
                if (this.effecter == null)
                {
                    this.effecter = (ext == null ? currentBillReport.bill.recipe?.effectWorking : ext.GetEffecter(this.currentBillReport.bill.recipe))?.Spawn();
                }
                if (this.sound == null)
                {
                    this.sound = (ext == null ? currentBillReport.bill.recipe?.soundWorking : ext.GetSound(this.currentBillReport.bill.recipe))?.TrySpawnSustainer(this);
                }
                this.effecter?.EffectTick(this, this);
                this.sound?.SustainerUpdate();
                if (this.GetComp<CompGlowerPulse>() != null)
                {
                    this.GetComp<CompGlowerPulse>().Glows = true;
                }
            }
            else
            {
                if (this.effecter != null)
                {
                    this.effecter.Cleanup();
                    this.effecter = null;
                }
                if (this.sound != null)
                {
                    this.sound.End();
                    this.sound = null;
                }
                if (this.GetComp<CompGlowerPulse>() != null)
                {
                    this.GetComp<CompGlowerPulse>().Glows = false;
                }
            }
            //Fuel
            if (compRefuelable != null && Active && currentBillReport != null)
            {
                compRefuelable.Notify_UsedThisTick();
            }

        }
        // TryGetNextBill returns a new BillReport to start if one is available
        protected BillReport TryGetNextBill()
        {
            var allThings = AllAccessibleThings;
            IEnumerable<Bill> allBills = AllBillsShouldDoNow;

            foreach (Bill b in allBills)
            {
                List<ThingCount> chosen = new List<ThingCount>();

                List<Thing> allAccessibleAllowedThings = allThings.Where(x => b.IsFixedOrAllowedIngredient(x)).ToList();

                if (allAccessibleAllowedThings.Count > 0 || b.ingredientFilter.AllowedThingDefs.Count() == 0)
                {
                    if (TryFindBestBillIngredientsInSet(allAccessibleAllowedThings, b, chosen))
                    {
                        return new BillReport(b, (from ta in chosen select ta.Thing.SplitOff(ta.Count)).ToList());
                    }
                }

            }
            return null;
        }

        private ModExtension_Skills extension_Skills => this.def.GetModExtension<ModExtension_Skills>();

        bool TryFindBestBillIngredientsInSet(List<Thing> accessibleThings, Bill b, List<ThingCount> chosen)
        {
            //TryFindBestBillIngredientsInSet Expects a List of Both Avilibale & Allowed Things as "accessibleThings"
            List<IngredientCount> missing = new List<IngredientCount>(); // Needed for 1.4
            return (bool)ReflectionUtility.TryFindBestBillIngredientsInSet.Invoke(null, new object[] { accessibleThings, b, chosen, new IntVec3(), false, missing });
        }

        protected virtual void ProduceItems()
        {
            if (currentBillReport == null)
            {
                Log.Error("Project RimFactory :: Tried to make products when assembler isn't engaged in a bill.");
                return;
            }
            // GenRecipe handles creating any bonus products
            IEnumerable<Thing> products = GenRecipe.MakeRecipeProducts(currentBillReport.bill.recipe, buildingPawn, currentBillReport.selected, ProjectSAL_Utilities.CalculateDominantIngredient(currentBillReport.bill.recipe, currentBillReport.selected), this);
            foreach (Thing thing in products)
            {
                PostProcessRecipeProduct(thing);
                thingQueue.Add(thing);
            }
            for (int i = 0; i < currentBillReport.selected.Count; i++)
            {
                if (currentBillReport.selected[i] is Corpse c)
                {
                    if (c.InnerPawn?.apparel != null)
                    {
                        List<Apparel> apparel = new List<Apparel>(c.InnerPawn.apparel.WornApparel);
                        for (int j = 0; j < apparel.Count; j++)
                        {
                            thingQueue.Add(apparel[j]);
                            c.InnerPawn.apparel.Remove(apparel[j]);
                        }
                    }
                    if (c.InnerPawn.inventory?.innerContainer != null)
                    {
                        List<Thing> things = c.InnerPawn.inventory.innerContainer.ToList();
                        for (int j = 0; j < things.Count; j++)
                        {
                            thingQueue.Add(things[j]);
                            c.InnerPawn.inventory.innerContainer.Remove(things[j]);
                        }
                    }

                }
                currentBillReport.bill.recipe.Worker.ConsumeIngredient(currentBillReport.selected[i], currentBillReport.bill.recipe, Map);

            }

            thingQueue.AddRange(from Thing t in currentBillReport.selected where t.Spawned select t);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            if (this.Active)
            {
                if (currentBillReport == null)
                { // assembler is not working despite power:
                    // show why it is not working:
                    if (this.BillStack.AnyShouldDoNow)
                    { // it DOES have bills
                        stringBuilder.AppendLine("SearchingForIngredients".Translate());
                    }
                    else
                    { // it DOESN'T have bills:
                        stringBuilder.AppendLine("AssemblerNoBills".Translate());
                    }
                }
                else
                { // assembler is working
                    stringBuilder.AppendLine("SAL3_BillReport".Translate(currentBillReport.bill.Label.ToString(), currentBillReport.workLeft.ToStringWorkAmount()));
                }
            }
            // even if it's not active, show any products ready to place:
            //   (we always show this: even if 0 products, it lets new players
            //    know it will hold products until it CAN place them)
            stringBuilder.AppendLine("SAL3_Products".Translate(thingQueue.Count, max_thingQueue_Count));
            return stringBuilder.ToString().TrimEndNewlines();
        }

        // Settings
        protected bool allowForbidden; // internal variable
        public virtual bool AllowForbidden => allowForbidden;
        protected virtual float ProductionSpeedFactor => def.GetModExtension<AssemblerDefModExtension>()?.workSpeedBaseFactor ?? 1f;

        protected virtual bool SatisfiesSkillRequirements(RecipeDef recipe)
        {
            if (this.buildingPawn != null && this.buildingPawn.skills != null)
            {
                return recipe.PawnSatisfiesSkillRequirements(this.buildingPawn);
            }
            else
            {
                return recipe.skillRequirements?.All(s => s.minLevel <= extension_Skills.GetExtendedSkillLevel(s.skill, typeof(Building_ProgrammableAssembler))) ?? true;
            }
        }

        public IPowerSupplyMachine RangePowerSupplyMachine => this.GetComp<CompPowerWorkSetting>();

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (this.GetComp<CompPowerWorkSetting>() == null)
            {
                GenDraw.DrawFieldEdges(IngredientStackCells.ToList());
            }
        }
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (this.DrawStatus && Find.CameraDriver.CurrentZoom < CameraZoomRange.Middle)
            {
                string label = "";
                string label2 = "";
                // only show overlay status text if has power:
                if (this.Active)
                {

                    if (currentBillReport != null) // the assembler is actively working
                    { // set the status text to the bill's label:
                        label = currentBillReport.bill.LabelCap;
                    }
                    else // the assembler is NOT working
                    {
                        // show why it is not working:
                        if (this.BillStack.AnyShouldDoNow)
                        { // it DOES have bills
                            label = "SearchingForIngredients".Translate();
                        }
                        else
                        { // it DOESN'T have bills:
                            label = "AssemblerNoBills".Translate();
                        }
                    }
                    // draw the label on the screen:

                }
                else if (compFlick?.SwitchIsOn == false)
                {
                    label = "SwitchedOff".Translate();
                }

                if (!AllowProduction_thingQueue)
                {
                    label2 = "PRF_OutputBufferWarning".Translate();
                }
                else if (thingQueue.Count > 1)
                {
                    label2 = "SAL3_Products".Translate(thingQueue.Count, max_thingQueue_Count);
                }
                Vector2 vectorpos = GenMapUI.LabelDrawPosFor(this, 0f);

                //Don't show an Empty Line
                if (label != "")
                {
                    GenMapUI.DrawThingLabel(vectorpos, label, Color.white);
                }

                vectorpos.y += Verse.Text.CalcSize(label).y;

                //Don't show an Empty Line
                if (label2 != "")
                {
                    GenMapUI.DrawThingLabel(vectorpos, label2, Color.yellow);
                }

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

        public List<Thing> GetThingQueue()
        {
            return thingQueue;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            //Spawn Items in Queue on Minify
            for (int i = thingQueue.Count - 1; i >= 0; i--)
            {
                //some of the things contaned are marked as Destroyed for some reason. they should not be Destroyed
                if (thingQueue[i].Destroyed) thingQueue[i].ForceSetStateToUnspawned();

                PlaceThingUtility.PRFTryPlaceThing(this, thingQueue[i], this.Position, this.Map, true);
            }

            base.DeSpawn(mode);

            prf_gamecomp.DeRegisterAssemblerQueue(this);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            //this.Map is null after base.Destroy(mode);
            //Spawn Items in Queue on Deconstruct or Destruction of Assembler
            for (int i = thingQueue.Count - 1; i >= 0; i--)
            {
                PlaceThingUtility.PRFTryPlaceThing(this, thingQueue[i], this.Position, this.Map, true);
            }

            base.Destroy(mode);

            prf_gamecomp.DeRegisterAssemblerQueue(this);
        }

        // (Some) Internal variables:
        // Logic
        protected BillReport currentBillReport;

        // thingQueue is List to save properly
        //   List of produced things, waiting to be placed:
        protected List<Thing> thingQueue = new List<Thing>();
        //max number of items that can be stored in thingQueue before production is halted
        protected const int max_thingQueue_Count = 100;

        protected bool AllowProduction_thingQueue => thingQueue.Count < max_thingQueue_Count;

        Map IAssemblerQueue.Map => this.Map;

        [Unsaved]
        private Effecter effecter = null;
        [Unsaved]
        private Sustainer sound = null;
        protected CompPowerTrader compPowerTrader = null;
        protected CompRefuelable compRefuelable = null;
        protected CompFlickable compFlick = null;
    }
}
