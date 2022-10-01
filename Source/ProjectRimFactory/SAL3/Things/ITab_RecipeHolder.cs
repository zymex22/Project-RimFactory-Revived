using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{

    interface IRecipeHolderInterface
    {
        //List of Recipes saved on the Recipie Holder
        List<RecipeDef> Saved_Recipes { get; set; }
        //List of Recipes Quered up to be Learnd / Saved to the Recipie Holder
        List<RecipeDef> Quered_Recipes { get; set; }
        //List of Recipes that could be Learnd by the Recipie Holder
        List<RecipeDef> Learnable_Recipes { get; }
        //
        RecipeDef Recipe_Learning { get; set; }

        float Progress_Learning { get; }


    }


    class ITab_RecipeHolder : ITab
    {

        private const int ROW_HIGHT = 30;
        private float scrollViewHeight;

        private static readonly Vector2 WinSize = new Vector2(520f, 500f);

        private IRecipeHolderInterface parrentDB => this.SelThing as IRecipeHolderInterface;


        enum enum_RecipeStatus
        {
            Saved,
            Quered,
            Learnable,
            InPorogress
        }



        private void RefreshRecipeList()
        {
            Recipes = new Dictionary<RecipeDef, enum_RecipeStatus>();
            foreach (RecipeDef r in parrentDB.Learnable_Recipes)
            {
                if (parrentDB.Quered_Recipes.Contains(r))
                {
                    Recipes[r] = enum_RecipeStatus.Quered;
                }
                else if (parrentDB.Recipe_Learning == r)
                {
                    Recipes[r] = enum_RecipeStatus.InPorogress;
                }
                else
                {
                    Recipes[r] = enum_RecipeStatus.Learnable;
                }
            }
            foreach (RecipeDef r in parrentDB.Saved_Recipes)
            {
                Recipes[r] = enum_RecipeStatus.Saved;
            }
        }

        private Dictionary<RecipeDef, enum_RecipeStatus> Recipes;






        public ITab_RecipeHolder()
        {
            this.size = WinSize;
            this.labelKey = "PRF_RecipeTab_TabName".Translate(); ;
        }
        private Vector2 scrollPos;

        private bool showSaved = true;
        private bool showLearnable = true;
        private bool showQuered = true;

        private bool ShouldDrawRow(RecipeDef recipe, ref float curY, float ViewRecthight, float scrollY)
        {
            if (!showLearnable && Recipes[recipe] == enum_RecipeStatus.Learnable) return false;
            if (!showQuered && Recipes[recipe] == enum_RecipeStatus.Quered) return false;
            if (!showSaved && Recipes[recipe] == enum_RecipeStatus.Saved) return false;

            //The item is above the view (including a safty margin of one item)
            if ((curY + ROW_HIGHT - scrollY) < 0)
            {
                curY += ROW_HIGHT;
                return false;
            }

            // the item is above the lower limit (including a safty margin of one item)
            if ((curY - ROW_HIGHT - scrollY - ViewRecthight) < 0)
            {
                return true;
            }

            //Item is below the lower limit
            curY += ROW_HIGHT;

            return false;
        }


        protected override void FillTab()
        {
            RefreshRecipeList();

            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Rect rect;



            float currY = 0;
            list.Begin(inRect);
            list.Gap();


            rect = list.GetRect(30);

            rect.width = (WinSize.x / 3) - 20;
            Widgets.CheckboxLabeled(rect, "PRF_RecipeTab_FilterSaved".Translate(), ref showSaved);
            rect.x += rect.width + 10;
            Widgets.CheckboxLabeled(rect, "PRF_RecipeTab_FilterLearnable".Translate(), ref showLearnable);
            rect.x += rect.width + 10;
            Widgets.CheckboxLabeled(rect, "PRF_RecipeTab_FilterQueue".Translate(), ref showQuered);

            currY += 40;
            rect = list.GetRect(10);
            currY += 10;
            Widgets.DrawLineHorizontal(0, rect.y, rect.width);


            var outRect = new Rect(5f, currY + 5, WinSize.x - 30, WinSize.y - currY - 30);
            var viewRect = new Rect(0f, 0, outRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect, true);

            currY = 0;


            foreach (RecipeDef recipe in Recipes.Keys)
            {
                if (!ShouldDrawRow(recipe, ref currY, outRect.height, scrollPos.y)) continue;

                DrawRecipeRow(recipe, ref currY, viewRect.width);

            }
            if (Event.current.type == EventType.Layout) scrollViewHeight = currY + 30f;

            Widgets.EndScrollView();


            list.End();
        }


        private void DrawRecipeRow(RecipeDef recipe, ref float currY, float viewRect_width)
        {
            Rect rect2;
            Rect rect;

            rect = new Rect(0, currY, viewRect_width, 30);
            currY += ROW_HIGHT;

            //Display Image of Product
            if (recipe.products.Count > 0)
            {
                rect2 = rect;
                rect2.width = 30;
                rect2.height = 30;
                Widgets.DefIcon(rect2, recipe.products[0].thingDef);
            }


            rect.x = 60;
            Widgets.Label(rect, "" + recipe.label);

            rect.width = 100;
            rect.x = 350;


            if (Recipes[recipe] == enum_RecipeStatus.Learnable)
            {
                if (Widgets.ButtonText(rect, "PRF_RecipeTab_Button_Learn".Translate()))
                {
                    parrentDB.Quered_Recipes.Add(recipe);

                }
            }
            else if (Recipes[recipe] == enum_RecipeStatus.Quered)
            {
                if (Widgets.ButtonText(rect, "PRF_RecipeTab_Button_Cancel".Translate()))
                {
                    if (parrentDB.Quered_Recipes.Contains(recipe))
                    {
                        parrentDB.Quered_Recipes.Remove(recipe);
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
            else if (Recipes[recipe] == enum_RecipeStatus.InPorogress)
            {
                //TODO add a Are you sure? popup
                if (Widgets.ButtonText(rect, "PRF_RecipeTab_Button_Abort".Translate(parrentDB.Progress_Learning.ToStringWorkAmount())))
                {
                    parrentDB.Recipe_Learning = null;
                }
            }
        }




    }
}
