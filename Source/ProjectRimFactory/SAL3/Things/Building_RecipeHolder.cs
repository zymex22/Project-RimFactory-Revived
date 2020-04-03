using ProjectRimFactory.SAL3.Things.Assemblers;
using ProjectRimFactory.SAL3.Tools;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{
    public class Building_RecipeHolder : Building
    {
        static readonly IntVec3 Up = new IntVec3(0, 0, 1);
        //================================ Fields
        protected RecipeDef workingRecipe;
        protected float workAmount;
        public List<RecipeDef> recipes = new List<RecipeDef>();
        //================================ Misc
        public IEnumerable<Building_WorkTable> Tables => from IntVec3 cell in GenAdj.CellsAdjacent8Way(this)
                                                         from Thing t in cell.GetThingList(Map)
                                                         let building = t as Building_WorkTable
                                                         where building != null
                                                         select building;
        public virtual IEnumerable<RecipeDef> GetAllProvidedRecipeDefs()
        {
            HashSet<RecipeDef> result = new HashSet<RecipeDef>();
            foreach (Building_WorkTable table in Tables)
            {
                foreach (RecipeDef recipe in table.def.AllRecipes)
                {
                    if (recipe.AvailableNow && !recipes.Contains(recipe) && !result.Contains(recipe))
                        result.Add(recipe);
                }
            }
            return result;
        }
        protected virtual float GetLearnRecipeWorkAmount(RecipeDef recipe)
        {
            return recipe.WorkAmountTotal(ThingDefOf.Steel);
        }

        //================================ Overrides
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (workingRecipe == null)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "SALDataStartEncoding".Translate(),
                    defaultDesc = "SALDataStartEncoding_Desc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("SAL3/NewDisk"),
                    action = () =>
                    {
                        List<FloatMenuOption> options = GetPossibleOptions().ToList();
                        if (options.Count > 0)
                        {
                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                        else
                        {
                            Messages.Message("SALMessage_NoRecipes".Translate(), MessageTypeDefOf.RejectInput);
                        }
                    }
                };
            }
            else
            {
                yield return new Command_Action()
                {
                    defaultLabel = "SALDataCancelBills".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                    action = ResetProgress
                };
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
            List<FloatMenuOption> list = new List<FloatMenuOption>
            {
                new FloatMenuOption("Insta-finish", () => workAmount = 0f)
            };
            return list;
        }

        protected virtual IEnumerable<FloatMenuOption> GetPossibleOptions()
        {
            foreach (RecipeDef recipe in GetAllProvidedRecipeDefs())
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
            Map mapBefore = Map;
            // Do not remove ToList - It evaluates the enumerable
            List<IntVec3> list = GenAdj.CellsAdjacent8Way(this).ToList();
            base.DeSpawn();
            for (int i = 0; i < list.Count; i++)
            {
                IntVec3 cell = list[i];
                if (cell.GetFirstBuilding(mapBefore) is Building_SmartAssembler building)
                {
                    building.Notify_RecipeHolderRemoved();
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
            StringBuilder stringBuilder = new StringBuilder();
            string baseInspectString = base.GetInspectString();
            if (baseInspectString.Length > 0)
            {
                stringBuilder.AppendLine(baseInspectString);
            }
            if (workingRecipe != null)
            {
                stringBuilder.AppendLine("SALInspect_RecipeReport".Translate(workingRecipe.label, workAmount.ToStringWorkAmount()));
            }
            stringBuilder.AppendLine("SAL3_StoredRecipes".Translate(string.Join(", ", recipes.Select(r => r.label).ToArray())));
            return stringBuilder.ToString().TrimEndNewlines();
        }
    }
}
