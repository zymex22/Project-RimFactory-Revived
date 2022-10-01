using ProjectRimFactory.SAL3.Things.Assemblers;
using ProjectRimFactory.SAL3.Tools;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProjectRimFactory.SAL3.UI
{
    [StaticConstructorOnStartup]
    public class ITab_SAL3Bills : ITab
    {
        private float viewHeight = 1000f;

        private Vector2 scrollPosition = default(Vector2);

        private Bill mouseoverBill;

        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        public static readonly FieldInfo PasteXField = typeof(ITab_Bills).GetField("PasteX", BindingFlags.NonPublic | BindingFlags.Static);

        public static readonly FieldInfo PasteYField = typeof(ITab_Bills).GetField("PasteY", BindingFlags.NonPublic | BindingFlags.Static);

        public static readonly FieldInfo PasteSizeField = typeof(ITab_Bills).GetField("PasteSize", BindingFlags.NonPublic | BindingFlags.Static);

        protected Building_DynamicBillGiver SelAssembler
        {
            get
            {
                return (Building_DynamicBillGiver)SelThing;
            }
        }

        public ITab_SAL3Bills()
        {
            size = WinSize;
            labelKey = "SAL3_BillsTabLabel";
        }

        public override bool IsVisible => SelThing is Building_DynamicBillGiver;

        protected override void FillTab()
        {
            float pasteX = (float)PasteXField.GetValue(null);
            float pasteY = (float)PasteYField.GetValue(null);
            float pasteSize = (float)PasteSizeField.GetValue(null);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            Rect rect = new Rect(WinSize.x - pasteX, pasteY, pasteSize, pasteSize);
            if (BillUtility.Clipboard == null)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, Textures.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(rect, "PasteBillTip".Translate());
            }
            else if (!SelAssembler.GetAllRecipes().Contains(BillUtility.Clipboard.recipe) || !BillUtility.Clipboard.recipe.AvailableNow)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, Textures.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(rect, "ClipboardBillNotAvailableHere".Translate());
            }
            else if (SelAssembler.BillStack.Count >= 15)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, Textures.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(rect, "PasteBillTip".Translate() + " (" + "PasteBillTip_LimitReached".Translate() + ")");
            }
            else
            {
                if (Widgets.ButtonImageFitted(rect, Textures.Paste, Color.white))
                {
                    Bill bill = BillUtility.Clipboard.Clone();
                    bill.InitializeAfterClone();
                    SelAssembler.BillStack.AddBill(bill);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                }
                TooltipHandler.TipRegion(rect, "PasteBillTip".Translate());
            }
            Rect rect2 = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Func<List<FloatMenuOption>> recipeOptionsMaker = delegate ()
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (RecipeDef recipe in SelAssembler.GetAllRecipes())
                {
                    if (recipe.AvailableNow)
                    {
                        list.Add(new FloatMenuOption(recipe.LabelCap, delegate ()
                        {
                            if (!SelAssembler.Map.mapPawns.FreeColonists.Any((Pawn col) => recipe.PawnSatisfiesSkillRequirements(col)))
                            {
                                Bill.CreateNoPawnsWithSkillDialog(recipe);
                            }
                            Bill bill2 = recipe.MakeNewBill();
                            SelAssembler.BillStack.AddBill(bill2);
                            if (recipe.conceptLearned != null)
                            {
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
                            }
                        }, MenuOptionPriority.Default, null, null, 29f, (Rect r) => Widgets.InfoCardButton(r.x + 5f, r.y + (r.height - 24f) / 2f, recipe), null));
                    }
                }
                if (!list.Any())
                {
                    list.Add(new FloatMenuOption("NoneBrackets".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                return list;
            };
            mouseoverBill = SelAssembler.BillStack.DoListing(rect2, recipeOptionsMaker, ref scrollPosition, ref viewHeight);
            //
            //Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            //Func<List<FloatMenuOption>> recipeOptionsMaker = delegate
            //{
            //    List<FloatMenuOption> list = new List<FloatMenuOption>();
            //    foreach (RecipeDef recipe in SelAssembler.GetAllRecipes())
            //    {
            //        if (recipe.AvailableNow)
            //        {
            //            list.Add(new FloatMenuOption(recipe.LabelCap, delegate
            //            {
            //                Bill bill = recipe.MakeNewBill();
            //                SelAssembler.BillStack.AddBill(bill);
            //            }, MenuOptionPriority.Default, null, null, 29f, (Rect r) => Widgets.InfoCardButton(r.x + 5f, r.y + (r.height - 24f) / 2f, recipe), null));
            //        }
            //    }
            //    if (list.Count == 0)
            //    {
            //        list.Add(new FloatMenuOption("NoneBrackets".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
            //    }
            //    return list;
            //};
            //mouseoverBill = SelAssembler.BillStack.DoListing(rect, recipeOptionsMaker, ref this.scrollPosition, ref this.viewHeight);
        }

        public override void TabUpdate()
        {
            if (mouseoverBill != null)
            {
                mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelAssembler.Position);
                mouseoverBill = null;
            }
        }
    }
}
