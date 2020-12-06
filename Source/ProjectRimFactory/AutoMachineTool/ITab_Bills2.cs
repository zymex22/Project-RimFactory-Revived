using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    [StaticConstructorOnStartup]
    internal static class TexButton
    {
        public static readonly Texture2D Paste = ContentFinder<Texture2D>.Get("UI/Buttons/Paste");
    }

    public interface ITabBillTable
    {
        ThingDef def { get; }
        BillStack billStack { get; }
        Map Map { get; }
        IntVec3 Position { get; }
        IEnumerable<RecipeDef> AllRecipes { get; }
        Bill MakeNewBill(RecipeDef recipe);
        bool IsRemovable(RecipeDef recipe);
        void RemoveRecipe(RecipeDef recipe);
    }

    [StaticConstructorOnStartup]
    public class ITab_Bill2_Patcher
    {
        static ITab_Bill2_Patcher()
        {
            var method = AccessTools.Method(typeof(ITab_Bills2), "FillTab");
            var harmony = LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().HarmonyInstance;
            LoadedModManager.RunningMods.Where(c => c.PackageId.ToLower() == "Dubwise.DubsMintMenus".ToLower())
                .FirstOption()
                .ForEach(mint =>
                    mint.assemblies.loadedAssemblies
                        .SelectMany(a => Option(a.GetType("DubsMintMenus.HarmonyPatches")))
                        .SelectMany(a => Option(a.GetMethod("Prefix")))
                        .FirstOption()
                        .ForEach(m => harmony.Patch(method, new HarmonyMethod(m)))
                );
        }
    }

    // TODO:本体更新時に合わせる.
    public class ITab_Bills2 : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        [TweakValue("Interface", 0f, 128f)] private static readonly float PasteX = 48f;

        [TweakValue("Interface", 0f, 128f)] private static readonly float PasteY = 3f;

        [TweakValue("Interface", 0f, 32f)] private static readonly float PasteSize = 24f;

        private Bill mouseoverBill;

        private Vector2 scrollPosition;
        private float viewHeight = 1000f;

        public ITab_Bills2()
        {
            size = WinSize;
            labelKey = "TabBills";
            tutorTag = "Bills";
        }

        protected ITabBillTable SelTable => (ITabBillTable) SelThing;

        protected override void FillTab()
        {
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            var rect = new Rect(WinSize.x - PasteX, PasteY, PasteSize, PasteSize);
            if (BillUtility.Clipboard == null)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegionByKey(rect, "PasteBillTip");
            }
            else if (!SelTable.def.AllRecipes.Contains(BillUtility.Clipboard.recipe) ||
                     !BillUtility.Clipboard.recipe.AvailableNow)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegionByKey(rect, "ClipboardBillNotAvailableHere");
            }
            else if (SelTable.billStack.Count >= 15)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
                GUI.color = Color.white;
                if (Mouse.IsOver(rect))
                    TooltipHandler.TipRegion(rect,
                        "PasteBillTip".Translate() + " (" + "PasteBillTip_LimitReached".Translate() + ")");
            }
            else
            {
                if (Widgets.ButtonImageFitted(rect, TexButton.Paste, Color.white))
                {
                    var bill = BillUtility.Clipboard.Clone();
                    bill.InitializeAfterClone();
                    SelTable.billStack.AddBill(bill);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }

                TooltipHandler.TipRegionByKey(rect, "PasteBillTip");
            }

            var rect2 = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Func<List<FloatMenuOption>> recipeOptionsMaker = delegate
            {
                var list = new List<FloatMenuOption>();
                foreach (var recipe in SelTable.AllRecipes)
                    if (recipe.AvailableNow)
                    {
                        var deletable = SelTable.IsRemovable(recipe);

                        list.Add(new FloatMenuOption(recipe.LabelCap, delegate
                        {
                            if (!SelTable.Map.mapPawns.FreeColonists.Any(col =>
                                recipe.PawnSatisfiesSkillRequirements(col))) Bill.CreateNoPawnsWithSkillDialog(recipe);
                            var bill2 = SelTable.MakeNewBill(recipe);
                            SelTable.billStack.AddBill(bill2);
                            if (recipe.conceptLearned != null)
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned,
                                    KnowledgeAmount.Total);
                            if (TutorSystem.TutorialMode)
                                TutorSystem.Notify_Event("AddBill-" + recipe.LabelCap.Resolve());
                        }, MenuOptionPriority.Default, null, null, deletable ? 58f : 29f, r =>
                        {
                            if (deletable)
                                if (Widgets.ButtonImage(new Rect(r.x + 34f, r.y + (r.height - 24f), 24f, 24f),
                                    RS.DeleteX))
                                {
                                    SelTable.RemoveRecipe(recipe);
                                    return true;
                                }

                            return Widgets.InfoCardButton(r.x + 5f, r.y + (r.height - 24f) / 2f, recipe);
                        }));
                    }

                if (!list.Any()) list.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
                return list;
            };
            mouseoverBill = SelTable.billStack.DoListing(rect2, recipeOptionsMaker, ref scrollPosition, ref viewHeight);
        }

        public override void TabUpdate()
        {
            if (mouseoverBill != null)
            {
                mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelTable.Position);
                mouseoverBill = null;
            }
        }
    }
}