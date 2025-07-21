using ProjectRimFactory.SAL3.Things.Assemblers;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{
    public class Building_RecipeHolder : Building, IRecipeHolderInterface
    {
        private static readonly IntVec3 Up = new IntVec3(0, 0, 1);
        //================================ Fields
        private RecipeDef workingRecipe;
        private float workAmount;
        public List<RecipeDef> Recipes = [];
        //================================ Misc
        public IEnumerable<Building_WorkTable> Tables => from IntVec3 cell in this.GetComp<CompRecipeImportRange>()?.RangeCells() ?? GenAdj.CellsAdjacent8Way(this)
                                                         where cell.InBounds(this.Map)
                                                         from Thing t in cell.GetThingList(Map)
                                                         let building = t as Building_WorkTable
                                                         where building != null
                                                         select building;

        private List<RecipeDef> quered_recipes;

        public List<RecipeDef> Quered_Recipes
        {
            get => quered_recipes;
            set => quered_recipes = value;
        }

        public List<RecipeDef> Learnable_Recipes => GetAllProvidedRecipeDefs().ToList();

        public float Progress_Learning => workAmount;

        RecipeDef IRecipeHolderInterface.Recipe_Learning { get => workingRecipe; set => workingRecipe = value; }
        List<RecipeDef> IRecipeHolderInterface.Saved_Recipes { get => Recipes; set => Recipes = value; }

        // Used To detect if this is of type Bill_Mech
        // This is the same Logic as in RimWorld.BillUtility:MakeNewBill()
        private static bool IsBill_Mech(RecipeDef recipeDef)
        {
            return recipeDef.mechResurrection || recipeDef.gestationCycles > 0;
        }

        public virtual IEnumerable<RecipeDef> GetAllProvidedRecipeDefs()
        {
            HashSet<RecipeDef> result = [];
            foreach (var table in Tables)
            {
                foreach (var recipe in table.def.AllRecipes)
                {
                    if (recipe.AvailableNow && !Recipes.Contains(recipe) && !result.Contains(recipe) && !IsBill_Mech(recipe))
                        result.Add(recipe);
                }
            }
            return result;
        }
        protected virtual float GetLearnRecipeWorkAmount(RecipeDef recipe)
        {
            return recipe.WorkAmountForStuff(ThingDefOf.Steel);
        }

        //================================ Overrides
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "Debug actions",
                    action = () => Find.WindowStack.Add(new FloatMenu(GetDebugOptions()))
                };
            }
        }

        List<FloatMenuOption> GetDebugOptions()
        {
            List<FloatMenuOption> list = [new FloatMenuOption("Insta-finish", () => workAmount = 0f)];
            return list;
        }

        protected virtual IEnumerable<FloatMenuOption> GetPossibleOptions()
        {
            foreach (var recipe in GetAllProvidedRecipeDefs())
            {
                yield return new FloatMenuOption(recipe.LabelCap, () =>
                {
                    workingRecipe = recipe;
                    workAmount = GetLearnRecipeWorkAmount(recipe);
                });
            }
        }

        private void ResetProgress()
        {
            workAmount = 0f;
            workingRecipe = null;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            ResetProgress();
            var mapBefore = Map;
            // Do not remove ToList - It evaluates the enumerable
            var cells = GenAdj.CellsAdjacent8Way(this).ToList();
            base.DeSpawn(mode);
            for (var i = 0; i < cells.Count; i++)
            {
                var things = mapBefore.thingGrid.ThingsListAt(cells[i]);
                
                for (var j = things.Count - 1; j >= 0; j--)
                {
                    if (things[j] is not Building_SmartAssembler assembler) continue;
                    assembler.Notify_RecipeHolderRemoved();
                    // break; // We can afford to be silly and check everything in this one cell.
                    // despawning does not happen often, right?
                    // maybe?
                    break; // maybe not, who knows.
                }
            }
        }

        protected override void Tick()
        {
            if (!Spawned) return;
            if (this.IsHashIntervalTick(60) && GetComp<CompPowerTrader>()?.PowerOn != false)
            {

                if (workingRecipe != null)
                {
                    workAmount -= 60f;
                    if (workAmount < 0)
                    {
                        // Encode recipe
                        Recipes.Add(workingRecipe);
                        ResetProgress();
                    }
                }
                else if (Quered_Recipes.Count >= 1)
                {
                    workingRecipe = Quered_Recipes[0];
                    workAmount = GetLearnRecipeWorkAmount(workingRecipe);
                    Quered_Recipes.RemoveAt(0);
                }
            }
            base.Tick();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref workingRecipe, "workingRecipe");
            Scribe_Collections.Look(ref Recipes, "recipes", LookMode.Def);
            Scribe_Values.Look(ref workAmount, "workAmount");

            Scribe_Collections.Look(ref quered_recipes, "quered_recipes");

            quered_recipes ??= [];
        }
        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            var baseInspectString = base.GetInspectString();
            if (baseInspectString.Length > 0)
            {
                stringBuilder.AppendLine(baseInspectString);
            }
            if (workingRecipe != null)
            {
                stringBuilder.AppendLine("SALInspect_RecipeReport".Translate(workingRecipe.label, workAmount.ToStringWorkAmount()));
            }
            stringBuilder.AppendLine("SAL3_StoredRecipes".Translate(string.Join(", ", Recipes.Select(r => r.label).ToArray())));
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void PostMake()
        {
            base.PostMake();
            quered_recipes ??= [];
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            
            // Check for Null Ref issues in Saved or Queued recipes
            // (That can happen if Mods are Removed mid-Save or some mod makes a breaking change)
            Recipes.RemoveAll(recipeDef => recipeDef is null);
            quered_recipes.RemoveAll(recipeDef => recipeDef is null);
        }
    }
}
