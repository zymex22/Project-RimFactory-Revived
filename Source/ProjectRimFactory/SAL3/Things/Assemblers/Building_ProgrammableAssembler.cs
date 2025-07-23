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
                workLeft = b.recipe.WorkAmountTotal(ProjectSAL_Utilities.CalculateDominantIngredient(b.recipe, list));
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
        
        private CompPowerTrader compPowerTrader;
        private CompRefuelable compRefuelable;
        private CompFlickable compFlick;
        private CompOutputAdjustable compOutputAdjustable;
        
        public override Graphic Graphic => (this.Active)
            ? (CurrentBillReport == null
                // powered on but idle:
                ? base.Graphic
                // powered on and working:
                : def.GetModExtension<AssemblerDefModExtension>()?.WorkingGrahic ?? base.Graphic)
            // not powered on:
            : def.GetModExtension<AssemblerDefModExtension>()?.PowerOffGrahic ?? base.Graphic;

        public bool DrawStatus => def.GetModExtension<AssemblerDefModExtension>()?.drawStatus ?? true;

        // Pawn
        public Pawn BuildingPawn;

        public virtual void DoPawn()
        {
            try
            {
                var p = PawnGenerator.GeneratePawn(PRFDefOf.PRFSlavePawn, Faction.OfPlayer);
                p.Name = new NameTriple("...", Label ?? "SAL_Name".Translate(), "...");
                //Assign skills
                foreach (var s in p.skills.skills)
                {
                    s.Level = ExtensionSkills.GetExtendedSkillLevel(s.def, typeof(Building_ProgrammableAssembler));
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
                BuildingPawn = p;
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
            Scribe_Deep.Look(ref CurrentBillReport, "currentBillReport");
            Scribe_Collections.Look(ref ThingQueue, "thingQueue", LookMode.Deep);
            Scribe_Values.Look(ref allowForbidden, "allowForbidden");
            Scribe_Deep.Look(ref BuildingPawn, "buildingPawn");
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
            var stockpileCells = IngredientStackCells
                .Where(c => c != compOutputAdjustable.CurrentCell && !Map.thingGrid.ThingsListAt(c).Any(t => !t.def.CanOverlapZones));
            if (stockpileCells.Any())
            {
                Designator_ZoneAddStockpile_Resources stockpileZone = new();
                stockpileZone.DesignateMultiCell(stockpileCells);
            }
            else
            {
                Messages.Message("PRF_CantCreateStockpile".Translate(), this, MessageTypeDefOf.CautionInput);
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
                if (CurrentBillReport != null)
                {
                    Log.Message("Selected things: " + string.Join(", ", CurrentBillReport.selected.Select(StringConverter).ToArray()));
                }
            });
            yield return new FloatMenuOption("View all items available for input", () =>
            {
                Log.Message(string.Join(", ", AllAccessibleThings.Select(StringConverter).ToArray()));
            });
            yield break;
        }

        private MapTickManager mapManager;
        protected MapTickManager MapManager => mapManager;

        private PRFGameComponent prfGameComp = Current.Game.GetComponent<PRFGameComponent>();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compOutputAdjustable = GetComp<CompOutputAdjustable>();
            mapManager = map.GetComponent<MapTickManager>();
            if (BuildingPawn == null)
                DoPawn();

            BuildingPawn?.skills.skills.ForEach(s =>
            {
                s.Level = ExtensionSkills.GetExtendedSkillLevel(s.def, typeof(Building_ProgrammableAssembler));
                ReflectionUtility.cachedTotallyDisabled.SetValue(s, BoolUnknown.Unknown);
            });

            compPowerTrader = GetComp<CompPowerTrader>();
            compRefuelable = GetComp<CompRefuelable>();
            compFlick = GetComp<CompFlickable>();
            prfGameComp = Current.Game.GetComponent<PRFGameComponent>();

            //Assign Pawn's mapIndexOrState to building's mapIndexOrState
            ReflectionUtility.mapIndexOrState.SetValue(BuildingPawn, ReflectionUtility.mapIndexOrState.GetValue(this));
            //Assign Pawn's position without nasty errors
            BuildingPawn.SetPositionDirect(Position);

            //Need this type of call to set the Powerconsumption on load
            //A normal call will not work
            var rangePowerSupplyMachine = RangePowerSupplyMachine;
            if (rangePowerSupplyMachine != null)
            {
                MapManager.NextAction(rangePowerSupplyMachine.RefreshPowerStatus);
                MapManager.AfterAction(5, rangePowerSupplyMachine.RefreshPowerStatus);
            }
            prfGameComp.RegisterAssemblerQueue(this);

            //Check if the Current Bill still Exists
            if (CurrentBillReport?.bill is null)
            {
                //Remove Bill
                CurrentBillReport = null;
            }

        }

        protected virtual bool Active => compPowerTrader?.PowerOn != false
                                       && compRefuelable?.HasFuel != false
                                       && compFlick?.SwitchIsOn != false;

        protected IEnumerable<Thing> AllAccessibleThings => from t in AllThingsInArea
                                                            where (AllowForbidden || !t.IsForbidden(Faction))
                                                            where (!Map.reservationManager.AllReservedThings().Contains(t))
                                                            select t;

        protected IEnumerable<Thing> AllThingsInArea
        {
            get
            {
                foreach (var cell in IngredientStackCells)
                {
                    foreach (var thing in Map.thingGrid.ThingsListAt(cell))
                    {
                        if (thing is Building and IThingHolder holder)
                        {
                            if (holder.GetDirectlyHeldThings() is not ThingOwner<Thing> owner) continue;
                            foreach (var additionalThing in owner.InnerListForReading) yield return additionalThing;
                        }
                        else if (thing.def.category == ThingCategory.Item)
                        {
                            yield return thing;
                        }
                    }
                }
                yield break;
            }
        }
        
        protected IEnumerable<Bill> AllBillsShouldDoNow => from b in billStack.Bills
                                                           where b.ShouldDoNow()
                                                           select b;
        public override Thing GetThingBy(Func<Thing, bool> optionalValidator = null)
        {
            foreach (var t in ThingQueue)
            {
                if (optionalValidator != null && !optionalValidator(t)) continue;
                ThingQueue.Remove(t);
                return t;
            }
            return null;
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (this.IsHashIntervalTick(10) && Active)
            {
                if (ThingQueue.Count > 0 && this.PRFTryPlaceThing(ThingQueue[0], compOutputAdjustable.CurrentCell, Map))
                {
                    ThingQueue.RemoveAt(0);
                }
                if (CurrentBillReport != null)
                {
                    //Update the Required Work
                    CurrentBillReport.workLeft -= 10f * ProductionSpeedFactor * (this.TryGetComp<CompPowerWorkSetting>()?.GetSpeedFactor() ?? 1f);
                    //If Work Finished
                    if (CurrentBillReport.workLeft <= 0)
                    {
                        try
                        {
                            ProduceItems();
                            CurrentBillReport.bill.Notify_IterationCompleted(BuildingPawn, CurrentBillReport.selected);
                            Notify_RecipeCompleted(CurrentBillReport.bill.recipe);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error producing items for {GetUniqueLoadID()}: " + ex);
                        }
                        finally
                        {
                            CurrentBillReport = null;
                        }
                    }
                }
                else if (this.IsHashIntervalTick(60) && AllowProductionThingQueue)
                {
                    //Start Bill if Possible
                    if ((CurrentBillReport = TryGetNextBill()) != null)
                    {
                        Notify_BillStarted();
                    }
                }
            }
            // Effect.
            if (CurrentBillReport != null && Active)
            {
                var ext = this.def.GetModExtension<AssemblerDefModExtension>();
                effecter ??=
                    (ext == null ? CurrentBillReport.bill.recipe?.effectWorking : ext.GetEffecter(this.CurrentBillReport.bill.recipe))
                    ?.Spawn();
                sound ??=
                    (ext == null ? CurrentBillReport.bill.recipe?.soundWorking : ext.GetSound(this.CurrentBillReport.bill.recipe))
                    ?.TrySpawnSustainer(this);
                effecter?.EffectTick(this, this);
                sound?.SustainerUpdate();
                if (GetComp<CompGlowerPulse>() != null)
                {
                    GetComp<CompGlowerPulse>().Glows = true;
                }
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
                if (GetComp<CompGlowerPulse>() != null)
                {
                    GetComp<CompGlowerPulse>().Glows = false;
                }
            }
            //Fuel
            if (compRefuelable != null && Active && CurrentBillReport != null)
            {
                compRefuelable.Notify_UsedThisTick();
            }

        }
        // TryGetNextBill returns a new BillReport to start if one is available
        protected BillReport TryGetNextBill()
        {
            var allThings = AllAccessibleThings;
            var allBills = AllBillsShouldDoNow;

            foreach (var bill in allBills)
            {
                List<ThingCount> chosen = [];
                var allAccessibleAllowedThings = allThings.Where(x => bill.IsFixedOrAllowedIngredient(x)).ToList();

                if (allAccessibleAllowedThings.Count <= 0 && bill.ingredientFilter.AllowedThingDefs.Any()) continue;
                if (TryFindBestBillIngredientsInSet(allAccessibleAllowedThings, bill, chosen))
                {
                    return new BillReport(bill, (from ta in chosen select ta.Thing.SplitOff(ta.Count)).ToList());
                }
            }
            return null;
        }

        private ModExtension_Skills ExtensionSkills => this.def.GetModExtension<ModExtension_Skills>();

        static bool TryFindBestBillIngredientsInSet(List<Thing> accessibleThings, Bill b, List<ThingCount> chosen)
        {
            //TryFindBestBillIngredientsInSet Expects a List of Both Available & Allowed Things as "accessibleThings"
            List<IngredientCount> missing = []; // Needed for 1.4
            return (bool)ReflectionUtility.TryFindBestBillIngredientsInSet.Invoke(null, new object[] { accessibleThings, b, chosen, new IntVec3(), false, missing });
        }

        protected virtual void ProduceItems()
        {
            if (CurrentBillReport == null)
            {
                Log.Error("Project RimFactory :: Tried to make products when assembler isn't engaged in a bill.");
                return;
            }
            // GenRecipe handles creating any bonus products
            if (def == PRFDefOf.PRF_Recycler) Patch_Thing_SmeltProducts.RecyclerProducingItems = true;
            Patch_CompFoodPoisonable_Notify_RecipeProduced.AssemblerRefrence = this;
            var products = GenRecipe.MakeRecipeProducts(CurrentBillReport.bill.recipe, BuildingPawn,
                CurrentBillReport.selected, 
                ProjectSAL_Utilities.CalculateDominantIngredient(CurrentBillReport.bill.recipe, CurrentBillReport.selected),
                this);
            
            foreach (var thing in products)
            {
                PostProcessRecipeProduct(thing);
                ThingQueue.Add(thing);
            }
            Patch_CompFoodPoisonable_Notify_RecipeProduced.AssemblerRefrence = null;
            Patch_Thing_SmeltProducts.RecyclerProducingItems = false;
            
            // Consume the Input
            for (var i = 0; i < CurrentBillReport.selected.Count; i++)
            {
                var selected = CurrentBillReport.selected[i];
                TryGetCorpseItems(selected);
                CurrentBillReport.bill.recipe.Worker.ConsumeIngredient(selected, CurrentBillReport.bill.recipe, Map);
            }

            ThingQueue.AddRange(from Thing t in CurrentBillReport.selected where t.Spawned select t);
        }
        
        /// <summary>
        /// Checks if <paramref name="selected"/> is a <see cref="Corpse"/>
        /// If so adds all attached Things to the <see cref="ThingQueue"/>
        /// </summary>
        /// <param name="selected">a processed Item</param>
        private void TryGetCorpseItems(Thing selected)
        {
            if (selected is not Corpse c) return;
            var innerPawn = c.InnerPawn;
            if (innerPawn is null) return;
                    
            if (innerPawn.apparel != null)
            {
                List<Apparel> apparel = [..innerPawn.apparel.WornApparel];
                for (var j = 0; j < apparel.Count; j++)
                {
                    ThingQueue.Add(apparel[j]);
                    innerPawn.apparel.Remove(apparel[j]);
                }
            }
            if (innerPawn.inventory?.innerContainer != null)
            {
                var things = innerPawn.inventory.innerContainer.ToList();
                for (var j = 0; j < things.Count; j++)
                {
                    ThingQueue.Add(things[j]);
                    innerPawn.inventory.innerContainer.Remove(things[j]);
                }
            }
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            if (Active)
            {
                if (CurrentBillReport == null)
                {
                    // assembler is not working despite power:
                    // show why it is not working:
                    stringBuilder.AppendLine(BillStack.AnyShouldDoNow
                        ? "SearchingForIngredients".Translate() // it DOES have bills
                        : "AssemblerNoBills".Translate()); // it DOESN'T have bills:
                }
                else
                { 
                    // assembler is working
                    stringBuilder.AppendLine("SAL3_BillReport".Translate(CurrentBillReport.bill.Label, CurrentBillReport.workLeft.ToStringWorkAmount()));
                }
            }
            // even if it's not active, show any products ready to place:
            //   (we always show this: even if 0 products, it lets new players
            //    know it will hold products until it CAN place them)
            stringBuilder.AppendLine("SAL3_Products".Translate(ThingQueue.Count, MaxThingQueueCount));
            return stringBuilder.ToString().TrimEndNewlines();
        }

        // Settings
        private bool allowForbidden; // internal variable
        public virtual bool AllowForbidden => allowForbidden;
        protected virtual float ProductionSpeedFactor => def.GetModExtension<AssemblerDefModExtension>()?.workSpeedBaseFactor ?? 1f;

        protected virtual bool SatisfiesSkillRequirements(RecipeDef recipe)
        {
            if (BuildingPawn is { skills: not null })
            {
                return recipe.PawnSatisfiesSkillRequirements(this.BuildingPawn);
            }
            return recipe.skillRequirements?.All(s => s.minLevel <= ExtensionSkills.GetExtendedSkillLevel(s.skill, typeof(Building_ProgrammableAssembler))) ?? true;
        }

        public IPowerSupplyMachine RangePowerSupplyMachine => GetComp<CompPowerWorkSetting>();

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (GetComp<CompPowerWorkSetting>() == null)
            {
                GenDraw.DrawFieldEdges(IngredientStackCells.ToList());
            }
        }
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (!DrawStatus || Find.CameraDriver.CurrentZoom >= CameraZoomRange.Middle) return;
            
            var label = string.Empty;
            var label2 = string.Empty;
            // only show overlay status text if has power:
            if (Active)
            {
                if (CurrentBillReport != null) 
                { 
                    // the assembler is actively working
                    // set the status text to the bill's label:
                    label = CurrentBillReport.bill.LabelCap;
                }
                else 
                {
                    // the assembler is NOT working
                    // show why it is not working:
                    label = this.BillStack.AnyShouldDoNow ? 
                        "SearchingForIngredients".Translate() // it DOES have bills
                        : "AssemblerNoBills".Translate();     // it DOESN'T have bills:
                }
            }
            else if (compFlick?.SwitchIsOn == false)
            {
                label = "SwitchedOff".Translate();
            }

            if (!AllowProductionThingQueue)
            {
                label2 = "PRF_OutputBufferWarning".Translate();
            }
            else if (ThingQueue.Count > 1)
            {
                label2 = "SAL3_Products".Translate(ThingQueue.Count, MaxThingQueueCount);
            }
            var vectorPos = GenMapUI.LabelDrawPosFor(this, 0f);

            //Don't show an Empty Line
            if (label != string.Empty)
            {
                GenMapUI.DrawThingLabel(vectorPos, label, Color.white);
            }

            vectorPos.y += Text.CalcSize(label).y;

            //Don't show an Empty Line
            if (label2 != string.Empty)
            {
                GenMapUI.DrawThingLabel(vectorPos, label2, Color.yellow);
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
            return ThingQueue;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            //Spawn Items in Queue on Minify
            for (var i = ThingQueue.Count - 1; i >= 0; i--)
            {
                //some of the things contained are marked as Destroyed for some reason. they should not be Destroyed
                if (ThingQueue[i].Destroyed) ThingQueue[i].ForceSetStateToUnspawned();

                this.PRFTryPlaceThing(ThingQueue[i], this.Position, this.Map, true);
            }

            base.DeSpawn(mode);

            prfGameComp.DeRegisterAssemblerQueue(this);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            //this.Map is null after base.Destroy(mode);
            //Spawn Items in Queue on Deconstruct or Destruction of Assembler
            for (var i = ThingQueue.Count - 1; i >= 0; i--)
            {
                this.PRFTryPlaceThing(ThingQueue[i], Position, Map, true);
            }

            base.Destroy(mode);

            prfGameComp.DeRegisterAssemblerQueue(this);
        }

        public override IntVec3 OutputCell()
        {
            return compOutputAdjustable.CurrentCell;
        }

        // (Some) Internal variables:
        // Logic
        protected BillReport CurrentBillReport;

        // thingQueue is List to save properly
        //   List of produced things, waiting to be placed:
        protected List<Thing> ThingQueue = [];
        //max number of items that can be stored in thingQueue before production is halted
        private const int MaxThingQueueCount = 100;

        protected bool AllowProductionThingQueue => ThingQueue.Count < MaxThingQueueCount;

        Map IAssemblerQueue.Map => Map;

        [Unsaved]
        private Effecter effecter = null;
        [Unsaved]
        private Sustainer sound = null;
    }
}
