using System;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{

    interface IRecipeHolderInterface
    {
        //List of Recipes saved on the Recipie Holder
        List<RecipeDef> SavedRecipes { get; set; }
        //List of Recipes queued  up to be Learned / Saved to the Recipe Holder
        List<RecipeDef> QueuedRecipes { get; set; }
        //List of Recipes that could be Learned by the Recipe Holder
        List<RecipeDef> LearnableRecipes { get; }
        
        RecipeDef RecipeLearning { get; set; }

        float ProgressLearning { get; }


    }


    class ITab_RecipeHolder : ITab
    {

        private const int RowHeight = 30;
        private float scrollViewHeight;

        private static readonly Vector2 WinSize = new(520f, 500f);

        private IRecipeHolderInterface ParrentDB => SelThing as IRecipeHolderInterface;


        private enum Enum_RecipeStatus
        {
            Saved,
            Queued,
            Learnable,
            InProgress
        }

        private void RefreshRecipeList()
        {
            recipes = new Dictionary<RecipeDef, Enum_RecipeStatus>();
            foreach (var r in ParrentDB.LearnableRecipes)
            {
                if (ParrentDB.QueuedRecipes.Contains(r))
                {
                    recipes[r] = Enum_RecipeStatus.Queued;
                }
                else if (ParrentDB.RecipeLearning == r)
                {
                    recipes[r] = Enum_RecipeStatus.InProgress;
                }
                else
                {
                    recipes[r] = Enum_RecipeStatus.Learnable;
                }
            }
            foreach (var r in ParrentDB.SavedRecipes)
            {
                recipes[r] = Enum_RecipeStatus.Saved;
            }
        }

        private Dictionary<RecipeDef, Enum_RecipeStatus> recipes;

        public ITab_RecipeHolder()
        {
            size = WinSize;
            labelKey = "PRF_RecipeTab_TabName".Translate(); ;
        }
        private Vector2 scrollPos;

        private bool showSaved = true;
        private bool showLearnable = true;
        private bool showQueued = true;
        private string searchText = string.Empty;

        private bool ShouldDrawRow(RecipeDef recipe, ref float curY, float viewRectHeight, float scrollY, string search)
        {
            if (!showLearnable && recipes[recipe] == Enum_RecipeStatus.Learnable) return false;
            if (!showQueued && recipes[recipe] == Enum_RecipeStatus.Queued) return false;
            if (!showSaved && recipes[recipe] == Enum_RecipeStatus.Saved) return false;
            if (search != string.Empty && !recipe.label.ToLower().Contains(search.ToLower())) return false;

            //The item is above the view (including a safety margin of one item)
            if (curY + RowHeight - scrollY < 0)
            {
                curY += RowHeight;
                return false;
            }

            // the item is above the lower limit (including a safety margin of one item)
            if (curY - RowHeight - scrollY - viewRectHeight < 0)
            {
                return true;
            }

            //Item is below the lower limit
            curY += RowHeight;

            return false;
        }

        
        protected override void FillTab()
        {
            RefreshRecipeList();

            var list = new Listing_Standard();
            var inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            float currY = 0;
            list.Begin(inRect);
            list.Gap();


            var rect = list.GetRect(30);

            rect.width = (WinSize.x / 3) - 20;
            Widgets.CheckboxLabeled(rect, "PRF_RecipeTab_FilterSaved".Translate(), ref showSaved);
            rect.x += rect.width + 10;
            Widgets.CheckboxLabeled(rect, "PRF_RecipeTab_FilterLearnable".Translate(), ref showLearnable);
            rect.x += rect.width + 10;
            Widgets.CheckboxLabeled(rect, "PRF_RecipeTab_FilterQueue".Translate(), ref showQueued);

            currY += 40;
            rect = list.GetRect(10);
            currY += 10;
            Widgets.DrawLineHorizontal(0, rect.y, rect.width);
            rect = list.GetRect(20);
            currY += 20;
            rect.width -= 30 + 16 + 20 + 100;
            rect.x += 20;
            searchText = Widgets.TextField(rect, searchText);
            rect.x -= 20;
            rect.width = 20;
            GUI.DrawTexture(rect, TexButton.Search);


            var outRect = new Rect(5f, currY + 5, WinSize.x - 30, WinSize.y - currY - 30);
            var viewRect = new Rect(0f, 0, outRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect, true);

            currY = 0;


            foreach (var recipe in recipes.Keys)
            {
                if (!ShouldDrawRow(recipe, ref currY, outRect.height, scrollPos.y, searchText)) continue;

                DrawRecipeRow(recipe, ref currY, viewRect.width);

            }
            if (Event.current.type == EventType.Layout) scrollViewHeight = currY + 30f;

            Widgets.EndScrollView();


            list.End();
        }


        private void DrawRecipeRow(RecipeDef recipe, ref float currY, float viewRectWidth)
        {
            var rect = new Rect(0, currY, viewRectWidth, 30);
            currY += RowHeight;

            //Display Image of Product
            if (recipe.products.Count > 0)
            {
                var rect2 = rect;
                rect2.width = 30;
                rect2.height = 30;
                Widgets.DefIcon(rect2, recipe.products[0].thingDef);
            }


            rect.x = 60;
            Widgets.Label(rect, string.Empty + recipe.label);

            rect.width = 100;
            rect.x = 350;


            if (recipes[recipe] == Enum_RecipeStatus.Learnable)
            {
                if (Widgets.ButtonText(rect, "PRF_RecipeTab_Button_Learn".Translate()))
                {
                    ParrentDB.QueuedRecipes.Add(recipe);

                }
            }
            else if (recipes[recipe] == Enum_RecipeStatus.Queued)
            {
                if (Widgets.ButtonText(rect, "PRF_RecipeTab_Button_Cancel".Translate()))
                {
                    if (ParrentDB.QueuedRecipes.Contains(recipe))
                    {
                        ParrentDB.QueuedRecipes.Remove(recipe);
                    }

                }

            }
            /*Temporaly Disabled Forget Recipe Functionality*/
            /*else if (Recipes[recipe] == enum_RecipeStatus.Saved)
            {
                //TODO add a Are you sure? popup
                if (Widgets.ButtonText(rect, "PRF_RecipeTab_Button_Forget".Translate()))
                {
                    parrentDB.Saved_Recipes.Remove(recipe);
                }
            }*/
            else if (recipes[recipe] == Enum_RecipeStatus.InProgress)
            {
                //TODO add a Are you sure? popup
                if (Widgets.ButtonText(rect, "PRF_RecipeTab_Button_Abort".Translate(ParrentDB.ProgressLearning.ToStringWorkAmount())))
                {
                    ParrentDB.RecipeLearning = null;
                }
            }
        }




    }
}
