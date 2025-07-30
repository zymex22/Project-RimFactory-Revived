using RimWorld;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.SAL3.Tools;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public class Building_SmartAssembler : Building_ProgrammableAssembler, IRecipeSubscriber
    {
        
        private CompRecipeImportRange compRecipeImportRange;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compRecipeImportRange = GetComp<CompRecipeImportRange>();
            RefreshRecipeImportRange();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            foreach (var holder in subscribedBills.Keys)
            {
                holder.DeregisterRecipeSubscriber(this);
            }
        }

        private readonly Dictionary<Building_RecipeHolder, List<RecipeDef>> subscribedBills = new();
        private HashSet<RecipeDef> effectiveRecipes = [];

        // TODO: This needs to be called when the compRecipeImportRange Changes -> Overclocking and so on
        public void RefreshRecipeImportRange()
        {
            var holders = (compRecipeImportRange?.RangeCells() ?? GenAdj.CellsAdjacent8Way(this))
                .Where(cell => cell.InBounds(Map)).Select(cell => cell.GetFirstThing<Building_RecipeHolder>(Map))
                .Where(holder => holder != null).ToList();

            var missingHolders = subscribedBills.Keys.Where(holder => !holders.Contains(holder)).ToList();

            foreach (var holder in missingHolders)
            {
                holder.DeregisterRecipeSubscriber(this);
            }
            foreach (var holder in holders)
            {
                holder.RegisterRecipeSubscriber(this);
            }
        }
        
        // TODO we need to call this with all buildingRecipeHolder's when The Research level changes depending on the ModExtension_Skills
        // Not an issue for our own setup but for full functionality that is requiered
        public void RecipesChanged(Building_RecipeHolder buildingRecipeHolder)
        {
            if (subscribedBills.Keys.Contains(buildingRecipeHolder))
            {
                subscribedBills[buildingRecipeHolder] = buildingRecipeHolder.Recipes;
            }
            else
            {
                subscribedBills.Add(buildingRecipeHolder, buildingRecipeHolder.Recipes);
            }


            UpdateBills();
        }

        private void UpdateBills()
        {
            HashSet<RecipeDef> newRecipes = new();
            List<RecipeDef> added = new();
            
            foreach (var recipeList in subscribedBills.Values)
            {
                foreach (var recipe in recipeList)
                {
                    if (newRecipes.Contains(recipe)) continue; // No Duplicates
                    if (!SatisfiesSkillRequirements(recipe)) continue; // Only stuff that we can do
                    
                    if (!effectiveRecipes.Contains(recipe)) added.Add(recipe);
                    newRecipes.Add(recipe);
                }
            }

            var removed = effectiveRecipes.Where(recipe => !newRecipes.Contains(recipe)).ToList();


            if (removed.Count > 0)
            {
                def.recipes.RemoveAll(recipe => removed.Contains(recipe));
                
                // ALl Bills + possible current
                BillStack.Bills.RemoveAll(b => removed.Contains(b.recipe));
                if (CurrentBillReport != null && !removed.Contains(CurrentBillReport.Bill.recipe))
                {
                    for (var i = 0; i < CurrentBillReport.Selected.Count; i++)
                    {
                        GenPlace.TryPlaceThing(CurrentBillReport.Selected[i], Position, Map, ThingPlaceMode.Near);
                    }
                    CurrentBillReport = null;
                }
                
                Messages.Message("SAL3Alert_SomeBillsRemoved".Translate(), this, MessageTypeDefOf.NegativeEvent);
            }

            if (added.Count > 0)
            {
                def.recipes.AddRange(added);
            }

            if (added.Count > 0 || removed.Count > 0)
            {
                ReflectionUtility.AllRecipesCached.SetValue(def, null);
            }
            


            // Update the new Effective List
            effectiveRecipes = newRecipes;
        }

        public void RecipeProviderRemoved(Building_RecipeHolder buildingRecipeHolder)
        {
            subscribedBills.Remove(buildingRecipeHolder);
            
            UpdateBills();
        }
    }
}
