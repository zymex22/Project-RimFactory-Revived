using ProjectRimFactory.AutoMachineTool;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;


namespace ProjectRimFactory.Common
{
    /// <summary>
    /// An ITab that contains multiple settings, all in one place:
    ///  * whether it can output produced things to entire stockpile, or only one cell
    ///  * whether to obey IProductLimitation limits on production/storing
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once InconsistentNaming
    class ITab_ProductionSettings : ITab
    {
        private static readonly List<Func<Thing, bool>> ShowITabTests = [];
        private static readonly List<Func<Thing, float>> ExtraHeightRequests = [];
        private static readonly List<Action<Thing, Listing_Standard>> WindowContentDrawers = [];
        
        private IProductionSettingsUser ProductionSettingsUser => SelThing as IProductionSettingsUser;
        private IPRF_SettingsContentLink PRfSettingsContent => SelThing as IPRF_SettingsContentLink;
        private IPRF_Building PRfBuilding => SelThing as IPRF_Building;
        private CompPowerWorkSetting CompPropertiesPowerWork => (SelThing as ThingWithComps)?.GetComp<CompPowerWorkSetting>();
        private IProductLimitation Machine => SelThing as IProductLimitation;
        private IPowerSupplyMachineHolder SupplyMachineHolder => SelThing as IPowerSupplyMachineHolder;
        
        private Vector2 winSize = new(400f, 0f);
        private List<SlotGroup> groups;
        
        public override bool IsVisible
        {
            get
            {
                return ShowITabTests.FirstOrDefault(t => (t != null && t(SelThing))) != null || ShowProductLimit || ShowOutputToEntireStockpile || ShowObeysStorageFilter || ShowAdditionalSettings || ShowAreaSelectButton || ShowForbidOnPlacingSetting;
            }
        }

        private bool ShowProductLimit => Machine is { ProductLimitationDisable: false };

        private bool ShowOutputToEntireStockpile => (ProductionSettingsUser != null &&
                                                     ((ProductionSettingsUser.SettingsOptions & PRFBSetting.optionOutputToEntireStockpie) > 0) &&
                                                     // Only output to stockpile option if belt is above ground!
                                                     !(ProductionSettingsUser is IBeltConveyorLinkable belt && !belt.CanSendToLevel(ConveyorLevel.Ground)));

        private bool ShowObeysStorageFilter => (ProductionSettingsUser != null &&
                                                (ProductionSettingsUser.SettingsOptions & PRFBSetting.optionObeysStorageFilters) > 0) &&
                                               !(ProductionSettingsUser is IBeltConveyorLinkable belt && !belt.CanSendToLevel(ConveyorLevel.Ground));

        private bool ShowForbidOnPlacingSetting => PRfBuilding != null;

        private bool ShowRangeTypeSelectorButton => ShowAreaSelectButton && CompPropertiesPowerWork != null && CompPropertiesPowerWork.Props.allowManualRangeTypeChange;

        private bool ShowAdditionalSettings => PRfSettingsContent != null;

        private bool ShowAreaSelectButton => SupplyMachineHolder != null;

        
        public static void RegisterSetting(Func<Thing, bool> showResultTest, Func<Thing, float> extraHeightRequest,
                                           Action<Thing, Listing_Standard> windowContents)
        {
            ShowITabTests.Add(showResultTest);
            ExtraHeightRequests.Add(extraHeightRequest);
            WindowContentDrawers.Add(windowContents);
            // onOpen=null, etc.
        }
        
        public ITab_ProductionSettings()
        {
            labelKey = "PRFSettingsTab";
        }

        private static readonly TipSignal RotInputRangeTip = new("PRF_SettingsITab_TipSignal_RotInputRange".Translate());


        protected override void UpdateSize()
        {
            winSize.y = 10 + 24;
            winSize.x = 400f;

            if (ShowOutputToEntireStockpile || ShowObeysStorageFilter || ShowForbidOnPlacingSetting) winSize.y += 24f + 12f;
            if (ShowOutputToEntireStockpile) winSize.y += 24f;
            if (ShowObeysStorageFilter) winSize.y += 24f;
            if (ShowForbidOnPlacingSetting) winSize.y += 24f;

            for (int i = 0; i < ShowITabTests.Count; i++)
            {
                if (ShowITabTests[i]?.Invoke(SelThing) == true)
                {
                    winSize.y += (ExtraHeightRequests[i]?.Invoke(SelThing) ?? 0);
                }
            }

            if (ShowProductLimit) winSize.y += 200f; //270

            if (PRfSettingsContent != null)
            {

                winSize.y += PRfSettingsContent.PRF_SettingsContentOb.ITab_Settings_Additional_y;
                winSize.x = Mathf.Max(winSize.x, PRfSettingsContent.PRF_SettingsContentOb.ITab_Settings_Minimum_x);
            }
            if (ShowRangeTypeSelectorButton) winSize.y += 30f;

            float maxHeight = 900f;
            float minHeight = 70f; // if this starts too large, the window will be too high
            float inspectWindowHeight = 268f; // Note: at least one mod makes this larger - this may not be enough.
            if (UI.screenHeight > minHeight - inspectWindowHeight) maxHeight = UI.screenHeight - inspectWindowHeight;
            winSize.y = Mathf.Clamp(winSize.y, minHeight, maxHeight); //Support for lower Resolutions (With that the Tab should always fit on the screen) 

            size = winSize;
            //Log.Message("Size is currently: " + size.x + "," + size.y);
            base.UpdateSize();
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if (Machine != null)
            {
                groups = Find.CurrentMap.haulDestinationManager.AllGroups.ToList();
            }
        }

        private static string GetSlotGroupName(ISlotGroupParent slotGroup)
        {
            var appendGroup = string.Empty;
            if (slotGroup is IStorageGroupMember storageGroupMember)
            {
                if (storageGroupMember.Group != null && storageGroupMember.Group.RenamableLabel != string.Empty)
                {
                    appendGroup = $" ({storageGroupMember.Group.RenamableLabel})";
                }
            }
            
            if (slotGroup is IRenameable renameable)
            {
                return $"{renameable.RenamableLabel}{appendGroup}";
            }
            
            return $"{slotGroup.SlotYielderLabel()}{appendGroup}";
        }
        
        protected override void FillTab()
        {
            var list = new Listing_Standard();
            var inRect = new Rect(0f, 0f, winSize.x, winSize.y).ContractedBy(10f);
            bool doneSection = false;

            list.Begin(inRect);

            if (ShowOutputToEntireStockpile || ShowObeysStorageFilter || ShowForbidOnPlacingSetting)
            {
                doneSection = true;
                list.Label("PRF_ITab_ProductionSettings_OutputSettings_Header".Translate());

                list.Gap();
            }



            if (ShowOutputToEntireStockpile)
            {
                var description = "PRF.Common.OutputToStockpileDesc".Translate();
                var label = "PRF.Common.OutputToStockpile".Translate();
                bool tmpB = ProductionSettingsUser.OutputToEntireStockpile;
                list.CheckboxLabeled(label, ref tmpB, description);
                if (tmpB != ProductionSettingsUser.OutputToEntireStockpile)
                    ProductionSettingsUser.OutputToEntireStockpile = tmpB;
            }
            if (ShowObeysStorageFilter)
            {
                bool tmpB = ProductionSettingsUser.ObeysStorageFilters;
                list.CheckboxLabeled("PRF.Common.ObeysStorageFilters".Translate(), ref tmpB,
                    "PRF.Common.ObeysStorageFiltersDesc".Translate());
                if (tmpB != ProductionSettingsUser.ObeysStorageFilters)
                    ProductionSettingsUser.ObeysStorageFilters = tmpB;
            }
            if (ShowForbidOnPlacingSetting)
            {
                bool tmpB = PRfBuilding.ForbidOnPlacingDefault;
                list.CheckboxLabeled("PRF.Common.ForbidOnPlacingDefault".Translate(), ref tmpB,
                    "PRF.Common.ForbidOnPlacingDefaultDesc".Translate());
                if (tmpB != PRfBuilding.ForbidOnPlacingDefault)
                    PRfBuilding.ForbidOnPlacingDefault = tmpB;
            }

            //Registered Settings
            //What's that?
            for (int i = 0; i < ShowITabTests.Count; i++)
            {
                if (ShowITabTests[i]?.Invoke(SelThing) == true)
                {
                    if (WindowContentDrawers[i] != null)
                    {
                        if (doneSection) list.GapLine();
                        WindowContentDrawers[i](SelThing, list);
                        doneSection = true;
                    }
                }
            }

            //ProductLimitation
            if (Machine is { ProductLimitationDisable: false })
            {
                if (doneSection) list.GapLine();
                // ReSharper disable once RedundantAssignment
                doneSection = true;
                var label = "PRF.AutoMachineTool.ProductLimitation.ValueLabel".Translate();
                var labelTip = "PRF.AutoMachineTool.ProductLimitation.ValueLabelTip".Translate();
                var checkBoxLabel = "PRF.AutoMachineTool.ProductLimitation.CheckBoxLabel".Translate();
                var checkBoxLabelTip = "PRF.AutoMachineTool.ProductLimitation.CheckBoxLabelTip".Translate();
                var stackCountLabel = "PRF.AutoMachineTool.ProductLimitation.CountStacks".Translate();
                var stackCountLabelTip = "PRF.AutoMachineTool.ProductLimitation.CountStacksTip".Translate();
                var selectAreaTip = "PRF.AutoMachineTool.ProductLimitation.SelectAreaTip".Translate();

                list.Label("PRF_ITab_ProductionSettings_ProductLimitation_Header".Translate());
                list.Gap();

                // Why did the OP decide to make labels in rects instead of using the Listing_Standard?
                //   If a language ever makes this too long for 70f, use list.Label() instead and make
                //   everything in a scrollview, eh?
                bool limitation = Machine.ProductLimitation;
                list.CheckboxLabeled(checkBoxLabel, ref limitation, checkBoxLabelTip);
                Machine.ProductLimitation = limitation;
                list.Gap();



                var rect = list.GetRect(30f);
                string buf = Machine.ProductLimitCount.ToString();
                int limit = Machine.ProductLimitCount;
                if (!labelTip.NullOrEmpty())
                {
                    if (Mouse.IsOver(rect))
                    {
                        Widgets.DrawHighlight(rect);
                    }
                    TooltipHandler.TipRegion(rect, labelTip);
                }
                Widgets.Label(rect.LeftHalf(), label);
                Widgets.TextFieldNumeric(rect.RightHalf(), ref limit, ref buf, 1, 1000000);
                list.Gap();

                bool countStacks = Machine.CountStacks;
                list.CheckboxLabeled(stackCountLabel, ref countStacks, stackCountLabelTip);
                Machine.CountStacks = countStacks;
                list.Gap();

                rect = list.GetRect(30f);
                if (!selectAreaTip.NullOrEmpty())
                {
                    if (Mouse.IsOver(rect))
                    {
                        Widgets.DrawHighlight(rect);
                    }
                    TooltipHandler.TipRegion(rect, selectAreaTip);
                }
                Widgets.Label(rect.LeftHalf(), "PRF.AutoMachineTool.CountZone".Translate());
                
                if (Widgets.ButtonText(rect.RightHalf(), Machine.TargetSlotGroup?.parent?.SlotYielderLabel() ?? "PRF.AutoMachineTool.EntierMap".Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(groups
                        .Select(g => new FloatMenuOption( GetSlotGroupName(g.parent), () => Machine.TargetSlotGroup =g))
                        .ToList()
                        .Head(new FloatMenuOption("PRF.AutoMachineTool.EntierMap".Translate(), () => Machine.TargetSlotGroup = null))));
                }
                Machine.ProductLimitCount = limit;
            }

            //Other Registered settings (Drone Station)
            if (PRfSettingsContent != null)
            {
                list = PRfSettingsContent.PRF_SettingsContentOb.ITab_Settings_AppendContent(list, inRect);
            }

            //Range Type
            if (ShowRangeTypeSelectorButton)
            {
                list.GapLine();

                inRect = list.GetRect(30f);

                Widgets.Label(inRect.LeftHalf(), "PRF_SettingsTab_RangeType_Label".Translate());
                if (Widgets.ButtonText(inRect.RightHalf(), CompPropertiesPowerWork.rangeCells.ToText()))
                {
                    Find.WindowStack.Add(new FloatMenu(CompPropertiesPowerWork.rangeTypes
                      .Select(d => new FloatMenuOption(d.ToText(),
                      () => CompPropertiesPowerWork.rangeCells = d
                      )).ToList()));

                }
                if (CompPropertiesPowerWork.rangeCells.NeedsRotate)
                {

                    var inRect2 = inRect;
                    inRect2.width = 30;
                    inRect2.height = 30;
                    // - 10 as a spacer
                    inRect2.x = inRect.RightHalf().x - inRect2.width - 10;
                    //Add Rotate Button
                    if (Widgets.ButtonImage(inRect2, TexUI.RotRightTex))
                    {
                        CompPropertiesPowerWork.RangeTypeRot.Rotate(RotationDirection.Clockwise);
                    }
                    TooltipHandler.TipRegion(inRect2, RotInputRangeTip);

                }
                list.Gap();
            }

            list.Gap();
            list.End();
        }
    }
}
