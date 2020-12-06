using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectRimFactory.SAL3.Things.Assemblers;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{
    public class Building_RecipeHolder : Building
    {
        private static readonly IntVec3 Up = new IntVec3(0, 0, 1);
        public List<RecipeDef> recipes = new List<RecipeDef>();

        protected float workAmount;

        //================================ Fields
        protected RecipeDef workingRecipe;

        //================================ Misc
        public IEnumerable<Building_WorkTable> Tables =>
            from IntVec3 cell in GetComp<CompRecipeImportRange>()?.RangeCells() ?? GenAdj.CellsAdjacent8Way(this)
            from Thing t in cell.GetThingList(Map)
            let building = t as Building_WorkTable
            where building != null
            select building;

        public virtual IEnumerable<RecipeDef> GetAllProvidedRecipeDefs()
        {
            var result = new HashSet<RecipeDef>();
            foreach (var table in Tables)
            foreach (var recipe in table.def.AllRecipes)
                if (recipe.AvailableNow && !recipes.Contains(recipe) && !result.Contains(recipe))
                    result.Add(recipe);
            return result;
        }

        protected virtual float GetLearnRecipeWorkAmount(RecipeDef recipe)
        {
            return recipe.WorkAmountTotal(ThingDefOf.Steel);
        }

        //================================ Overrides
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
            if (workingRecipe == null)
                yield return new Command_Action
                {
                    defaultLabel = "SALDataStartEncoding".Translate(),
                    defaultDesc = "SALDataStartEncoding_Desc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("SAL3/NewDisk"),
                    action = () =>
                    {
                        var options = GetPossibleOptions().ToList();
                        if (options.Count > 0)
                            Find.WindowStack.Add(new FloatMenu(options));
                        else
                            Messages.Message("SALMessage_NoRecipes".Translate(), MessageTypeDefOf.RejectInput);
                    }
                };
            else
                yield return new Command_Action
                {
                    defaultLabel = "SALDataCancelBills".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                    action = ResetProgress
                };
            if (Prefs.DevMode)
                yield return new Command_Action
                {
                    defaultLabel = "Debug actions",
                    action = () => Find.WindowStack.Add(new FloatMenu(GetDebugOptions()))
                };
        }

        private List<FloatMenuOption> GetDebugOptions()
        {
            var list = new List<FloatMenuOption>
            {
                new FloatMenuOption("Insta-finish", () => workAmount = 0f)
            };
            return list;
        }

        protected virtual IEnumerable<FloatMenuOption> GetPossibleOptions()
        {
            foreach (var recipe in GetAllProvidedRecipeDefs())
                yield return new FloatMenuOption(recipe.LabelCap, () =>
                {
                    workingRecipe = recipe;
                    workAmount = GetLearnRecipeWorkAmount(recipe);
                });
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
            base.DeSpawn();
            for (var i = 0; i < cells.Count; i++)
            {
                var things = mapBefore.thingGrid.ThingsListAt(cells[i]);
                for (var j = things.Count - 1; j >= 0; j--)
                    if (things[j] is Building_SmartAssembler)
                    {
                        (things[j] as Building_SmartAssembler).Notify_RecipeHolderRemoved();
                        // break; // We can afford to be silly and check everything in this one cell.
                        // despawning does not happen often, right?
                        // maybe?
                        break; // maybe not, who knows.
                    }
            }
        }

        public override void Tick()
        {
            if (this.IsHashIntervalTick(60) && GetComp<CompPowerTrader>()?.PowerOn != false && workingRecipe != null)
            {
                workAmount -= 60f;
                if (workAmount < 0)
                {
                    // Encode recipe
                    recipes.Add(workingRecipe);
                    ResetProgress();
                }
            }

            base.Tick();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref workingRecipe, "workingRecipe");
            Scribe_Collections.Look(ref recipes, "recipes", LookMode.Def);
            Scribe_Values.Look(ref workAmount, "workAmount");
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            var baseInspectString = base.GetInspectString();
            if (baseInspectString.Length > 0) stringBuilder.AppendLine(baseInspectString);
            if (workingRecipe != null)
                stringBuilder.AppendLine(
                    "SALInspect_RecipeReport".Translate(workingRecipe.label, workAmount.ToStringWorkAmount()));
            stringBuilder.AppendLine(
                "SAL3_StoredRecipes".Translate(string.Join(", ", recipes.Select(r => r.label).ToArray())));
            return stringBuilder.ToString().TrimEndNewlines();
        }
    }
}