using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using ProjectRimFactory.SAL3.Tools;
using UnityEngine;
using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3.Exposables;
using Verse.Sound;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public abstract class Building_ProgrammableAssembler : Building_DynamicBillGiver, IPowerSupplyMachineHolder
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

        public override Graphic Graphic => (this.GetComp<CompPowerTrader>()?.PowerOn ?? true) 
            ? (this.currentBillReport == null 
                ? base.Graphic 
                : this.def.GetModExtension<AssemblerDefModExtension>()?.WorkingGrahic ?? base.Graphic) 
            : this.def.GetModExtension<AssemblerDefModExtension>()?.PowerOffGrahic ?? base.Graphic;

        public bool DrawStatus => this.def.GetModExtension<AssemblerDefModExtension>()?.drawStatus ?? true;

        // Pawn

        public Pawn buildingPawn;
        
        public virtual void DoPawn()
        {
            try
            {

                Pawn p = PawnGenerator.GeneratePawn(PRFDefOf.PRFSlavePawn, Faction.OfPlayer);
                p.Name = new NameTriple("...", "SAL_Name".Translate(), "...");
                //Assign skills
                foreach (var s in p.skills.skills)
                {
                    s.Level = s.def == SkillDefOf.Artistic ? this.ArtSkillLevel : this.SkillLevel;
                }
                //Assign Pawn's mapIndexOrState to building's mapIndexOrState
                ReflectionUtility.mapIndexOrState.SetValue(p, ReflectionUtility.mapIndexOrState.GetValue(this));
              
                //Assign Pawn's position without nasty errors
                p.SetPositionDirect(PositionHeld);
              
                //Clear pawn relations
                p.relations.ClearAllRelations();
              
                //Set backstories
                SetBackstoryAndSkills(p);
             
                //Pawn work-related stuffs
                for (int i = 0; i < 24; i++)
                {
                    p.timetable.SetAssignment(i, TimeAssignmentDefOf.Work);
                }
                buildingPawn = p;
            }
            catch (Exception ex) { 
            Log.Error("ERROR=: "+ex.ToString());
            }
        }

        private static void SetBackstoryAndSkills(Pawn p)
        {
            if (BackstoryDatabase.TryGetWithIdentifier("ChildSpy47", out Backstory bs))
            {
                p.story.childhood = bs;
            }
            else
            {
                Log.Error("Tried to assign child backstory ChildSpy47, but not found");
            }
            if (BackstoryDatabase.TryGetWithIdentifier("ColonySettler53", out Backstory bstory))
            {
          
                p.story.adulthood = bstory;
            }
            else
            {

                Log.Error("Tried to assign child backstory ColonySettler53, but not found");
            }
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
            if (buildingPawn == null)
                DoPawn();
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
            yield return new FloatMenuOption("View selected things", () => {
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
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (buildingPawn == null)
                DoPawn();

            this.buildingPawn?.skills.skills.ForEach(s =>
            {
                s.Level = s.def == SkillDefOf.Artistic ? this.ArtSkillLevel : this.SkillLevel;
                ReflectionUtility.cachedTotallyDisabled.SetValue(s, BoolUnknown.Unknown);
            });

            //Assign Pawn's mapIndexOrState to building's mapIndexOrState
            ReflectionUtility.mapIndexOrState.SetValue(buildingPawn, ReflectionUtility.mapIndexOrState.GetValue(this));
            //Assign Pawn's position without nasty errors
            buildingPawn.SetPositionDirect(Position);
        }

        // Logic
        protected BillReport currentBillReport;

        // thingQueue is List to save properly
        protected List<Thing> thingQueue = new List<Thing>();

        protected virtual bool Active => GetComp<CompPowerTrader>()?.PowerOn ?? true;

        protected IEnumerable<Thing> AllAccessibleThings => from c in IngredientStackCells
                                                            from t in Map.thingGrid.ThingsListAt(c)
                                                            where (AllowForbidden || !t.IsForbidden(Faction)) && t.def.category == ThingCategory.Item
                                                            select t;
        protected IEnumerable<Bill> AllBillsShouldDoNow => from b in billStack.Bills
                                                           where b.ShouldDoNow()
                                                           select b;
        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(10) && Active)
            {
              
                if (thingQueue.Count > 0 && OutputComp.CurrentCell.Walkable(Map) && 
                    (OutputComp.CurrentCell.GetFirstItem(Map)?.TryAbsorbStack(thingQueue[0], true) ?? GenPlace.TryPlaceThing(thingQueue[0], OutputComp.CurrentCell, Map, ThingPlaceMode.Direct)))
                {
                    thingQueue.RemoveAt(0);
                }
                if (currentBillReport != null)
                {
                    currentBillReport.workLeft -= 10f * ProductionSpeedFactor * (this.TryGetComp<CompPowerWorkSetting>()?.GetSpeedFactor() ?? 1f);
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
                else if (this.IsHashIntervalTick(60))
                {
                    if ((currentBillReport = CheckBills()) != null)
                    {
                        Notify_BillStarted();
                    }
                }
            }
            // Effect.
            if (currentBillReport != null)
            {
                var ext = this.def.GetModExtension<AssemblerDefModExtension>();
                if (this.effecter == null)
                {
                    this.effecter = (ext == null ? currentBillReport.bill.recipe?.effectWorking : ext.GetEffecter(this.currentBillReport.bill.recipe))?.Spawn();
                }
                if(this.sound == null)
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
                if(this.sound != null)
                {
                    this.sound.End();
                    this.sound = null;
                }
                if (this.GetComp<CompGlowerPulse>() != null)
                {
                    this.GetComp<CompGlowerPulse>().Glows = false;
                }
            }
        }

        [Unsaved]
        private Effecter effecter = null;
        [Unsaved]
        private Sustainer sound = null;

        protected virtual BillReport CheckBills()
        {
            foreach (Bill b in AllBillsShouldDoNow)
            {
                List<ThingCount> chosen = new List<ThingCount>();
                if (TryFindBestBillIngredientsInSet(AllAccessibleThings.ToList(), b, chosen))
                {
                  
                    return new BillReport(b, (from ta in chosen select ta.Thing.SplitOff(ta.Count)).ToList());
                }
            }
            return null;
        }

        bool TryFindBestBillIngredientsInSet(List<Thing> accessibleThings, Bill b, List<ThingCount> chosen)
        {
            ReflectionUtility.MakeIngredientsListInProcessingOrder.Invoke(null, new object[] { ReflectionUtility.ingredientsOrdered.GetValue(null), b });
             return (bool)ReflectionUtility.TryFindBestBillIngredientsInSet.Invoke(null, new object[] { accessibleThings, b, chosen, new IntVec3(), false });
        }

        protected virtual void ProduceItems()
        {
            if (currentBillReport == null)
            {
                Log.Error("Project RimFactory :: Tried to make products when assembler isn't engaged in a bill.");
                return;
            }
            IEnumerable<Thing> products = GenRecipe.MakeRecipeProducts(currentBillReport.bill.recipe, buildingPawn, currentBillReport.selected, ProjectSAL_Utilities.CalculateDominantIngredient(currentBillReport.bill.recipe, currentBillReport.selected), this);
            foreach (Thing thing in products)
            {
                PostProcessRecipeProduct(thing);
                thingQueue.Add(thing);
            }
            for (int i = 0; i < currentBillReport.selected.Count; i++)
            {
                if (currentBillReport.selected[i] is Corpse c && c.InnerPawn?.apparel != null)
                {
                    List<Apparel> apparel = new List<Apparel>(c.InnerPawn.apparel.WornApparel);
                    for (int j = 0; j < apparel.Count; j++)
                    {
                        thingQueue.Add(apparel[j]);
                        c.InnerPawn.apparel.Remove(apparel[j]);
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
            if (currentBillReport == null)
            {
                stringBuilder.AppendLine("SearchingForIngredients".Translate());
            }
            else
            {
                stringBuilder.AppendLine("SAL3_BillReport".Translate(currentBillReport.bill.Label.ToString(), currentBillReport.workLeft.ToStringWorkAmount()));
            }
            stringBuilder.AppendLine("SAL3_Products".Translate(thingQueue.Count));
            return stringBuilder.ToString().TrimEndNewlines();
        }

        // Settings
        public bool allowForbidden;
        public virtual bool AllowForbidden => allowForbidden;
        protected virtual float ProductionSpeedFactor => def.GetModExtension<AssemblerDefModExtension>()?.workSpeedBaseFactor ?? 1f;

        protected virtual int SkillLevel => def.GetModExtension<AssemblerDefModExtension>()?.skillLevel ?? 0;

        protected virtual int ArtSkillLevel => def.GetModExtension<AssemblerDefModExtension>()?.artSkillLevel ?? 10;

        protected virtual bool SatisfiesSkillRequirements(RecipeDef recipe)
        {
            if (this.buildingPawn != null && this.buildingPawn.skills != null)
            {
                return recipe.PawnSatisfiesSkillRequirements(this.buildingPawn);
            }
            else
            {
                return recipe.skillRequirements?.All(s => s.minLevel <= (s.skill == SkillDefOf.Artistic ? this.ArtSkillLevel : this.SkillLevel)) ?? true;
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
                string label;
                if (currentBillReport != null) // the assembler is actively working
                { // set the status text to the bill's label:
                    label = currentBillReport.bill.LabelCap;
                }
                else // the assmelber is NOT working
                {
                  // show why it is not working:
                    if (this.BillStack.AnyShouldDoNow) { // it DOES have bills
                        label = "SearchingForIngredients".Translate();
                    } else { // it DOESN'T have bills:
                        label = "AssemblerNoBills".Translate();    
                    }
                }
                // draw the label on the screen:
                GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(this, 0f), label, Color.white);
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
    }
}
