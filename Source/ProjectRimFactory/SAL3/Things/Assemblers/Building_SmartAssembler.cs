using RimWorld;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3.Tools;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers
{
    public class Building_SmartAssembler : Building_ProgrammableAssembler, IRecipeSubscriber
    {
        
        private CompRecipeImportRange compRecipeImportRange;
        private List<IntVec3> importCells;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compRecipeImportRange = GetComp<CompRecipeImportRange>();
            RefreshRecipeImportRange();
            Map.GetComponent<PRFMapComponent>().RegisterRecipeSubscriber(this);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var keys = subscribedBills.Keys.ToArray();
            foreach (var holder in keys)
            {
                holder.DeregisterRecipeSubscriber(this);
            }
            
            Map.GetComponent<PRFMapComponent>().DeregisterRecipeSubscriber(this);
            base.DeSpawn(mode);
        }

        private readonly Dictionary<Building_RecipeHolder, List<RecipeDef>> subscribedBills = new();
        private HashSet<RecipeDef> effectiveRecipes = [];

        private void RefreshRecipeImportRange()
        {
            importCells =
                (compRecipeImportRange?.RangeCells() ?? GenAdj.CellsAdjacent8Way(this)).Where(
                    cell => cell.InBounds(Map)).ToList();
            var holders = importCells.Select(cell => cell.GetFirstThing<Building_RecipeHolder>(Map))
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
        // Not an issue for our own setup but for full functionality that is required
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

        public void RecipeProviderSpawnedAt(IntVec3 providerLocation, Building_RecipeHolder buildingRecipeHolder)
        {
            if (!importCells.Contains(providerLocation)) return;
            RecipesChanged(buildingRecipeHolder);
        }
    }
}
