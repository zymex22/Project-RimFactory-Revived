using HarmonyLib;
using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    [StaticConstructorOnStartup]
    internal static class TexButton
    {
        public static readonly Texture2D Paste = ContentFinder<Texture2D>.Get("UI/Buttons/Paste", true);
    }

    public interface ITabBillTable
    {
        ThingDef def { get; }
        BillStack billStack { get; }
        Map Map { get; }
        IntVec3 Position { get; }
        Bill MakeNewBill(RecipeDef recipe);
        IEnumerable<RecipeDef> AllRecipes { get; }
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
        private float viewHeight = 1000f;

        private Vector2 scrollPosition = default(Vector2);

        private Bill mouseoverBill;

        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        [TweakValue("Interface", 0f, 128f)]
        private static float PasteX = 48f;

        [TweakValue("Interface", 0f, 128f)]
        private static float PasteY = 3f;

        [TweakValue("Interface", 0f, 32f)]
        private static float PasteSize = 24f;

        protected ITabBillTable SelTable
        {
            get
            {
                return (ITabBillTable)base.SelThing;
            }
        }

        public ITab_Bills2()
        {
            this.size = ITab_Bills2.WinSize;
            this.labelKey = "TabBills";
            this.tutorTag = "Bills";
        }

        protected override void FillTab()
        {
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            Rect rect = new Rect(ITab_Bills2.WinSize.x - ITab_Bills2.PasteX, ITab_Bills2.PasteY, ITab_Bills2.PasteSize, ITab_Bills2.PasteSize);
            if (BillUtility.Clipboard == null)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegionByKey(rect, "PasteBillTip");
            }
            else if (!this.SelTable.def.AllRecipes.Contains(BillUtility.Clipboard.recipe) || !BillUtility.Clipboard.recipe.AvailableNow)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegionByKey(rect, "ClipboardBillNotAvailableHere");
            }
            else if (this.SelTable.billStack.Count >= 15)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
                GUI.color = Color.white;
                if (Mouse.IsOver(rect))
                {
                    TooltipHandler.TipRegion(rect, "PasteBillTip".Translate() + " (" + "PasteBillTip_LimitReached".Translate() + ")");
                }
            }
            else
            {
                if (Widgets.ButtonImageFitted(rect, TexButton.Paste, Color.white))
                {
                    Bill bill = BillUtility.Clipboard.Clone();
                    bill.InitializeAfterClone();
                    this.SelTable.billStack.AddBill(bill);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                }
                TooltipHandler.TipRegionByKey(rect, "PasteBillTip");
            }
            Rect rect2 = new Rect(0f, 0f, ITab_Bills2.WinSize.x, ITab_Bills2.WinSize.y).ContractedBy(10f);
            Func<List<FloatMenuOption>> recipeOptionsMaker = delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (var recipe in this.SelTable.AllRecipes)
                {
                    if (recipe.AvailableNow)
                    {
                        bool deletable = this.SelTable.IsRemovable(recipe);

                        list.Add(new FloatMenuOption(recipe.LabelCap, delegate
                        {
                            if (!this.SelTable.Map.mapPawns.FreeColonists.Any((Pawn col) => recipe.PawnSatisfiesSkillRequirements(col)))
                            {
                                Bill.CreateNoPawnsWithSkillDialog(recipe);
                            }
                            Bill bill2 = this.SelTable.MakeNewBill(recipe);
                            this.SelTable.billStack.AddBill(bill2);
                            if (recipe.conceptLearned != null)
                            {
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
                            }
                            if (TutorSystem.TutorialMode)
                            {
                                TutorSystem.Notify_Event("AddBill-" + recipe.LabelCap.Resolve());
                            }
                        }, MenuOptionPriority.Default, null, null, deletable ? 58f : 29f, (Rect r) =>
                        {
                            if (deletable)
                            {
                                if (Widgets.ButtonImage(new Rect(r.x + 34f, r.y + (r.height - 24f), 24f, 24f), RS.DeleteX))
                                {
                                    this.SelTable.RemoveRecipe(recipe);
                                    return true;
                                }
                            }
                            return Widgets.InfoCardButton(r.x + 5f, r.y + (r.height - 24f) / 2f, recipe);
                        }, null));
                    }
                }
                if (!list.Any<FloatMenuOption>())
                {
                    list.Add(new FloatMenuOption("NoneBrackets".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                return list;
            };
            this.mouseoverBill = this.SelTable.billStack.DoListing(rect2, recipeOptionsMaker, ref this.scrollPosition, ref this.viewHeight);
        }

        public override void TabUpdate()
        {
            if (this.mouseoverBill != null)
            {
                this.mouseoverBill.TryDrawIngredientSearchRadiusOnMap(this.SelTable.Position);
                this.mouseoverBill = null;
            }
        }
    }
}
