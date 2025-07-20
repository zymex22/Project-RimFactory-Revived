﻿using ProjectRimFactory.AutoMachineTool;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;


namespace ProjectRimFactory.Common
{

    public interface IPRF_SettingsContent
    {
        float ITab_Settings_Minimum_x { get; }
        float ITab_Settings_Additional_y { get; }

        //may need to pass some pos context
        Listing_Standard ITab_Settings_AppendContent(Listing_Standard list, Rect parrent_rect);



    }

    public interface IPRF_SettingsContentLink
    {
        IPRF_SettingsContent PRF_SettingsContentOb { get; }
    }



    /// <summary>
    /// An ITab that contains multilpe settings, all in one place:
    ///  * whether can output produced things to entire stockpile, or only one cell
    ///  * whether to obey IProductLimitation limits on production/storing
    /// </summary>
    class ITab_ProductionSettings : ITab
    {
        static List<Func<Thing, bool>> showITabTests = [];
        static List<Func<Thing, float>> extraHeightRequests = [];
        static List<Action<Thing, Listing_Standard>> windowContentDrawers = [];
        public static void RegisterSetting(Func<Thing, bool> showResultTest, Func<Thing, float> extraHeightRequest,
                                           Action<Thing, Listing_Standard> windowContents)
        {
            showITabTests.Add(showResultTest);
            extraHeightRequests.Add(extraHeightRequest);
            windowContentDrawers.Add(windowContents);
            // onOpen=null, etc.
        }



        private Vector2 winSize = new(400f, 0f);
        private List<SlotGroup> groups;

        public ITab_ProductionSettings()
        {
            labelKey = "PRFSettingsTab";
        }

        public override bool IsVisible
        {
            get
            {
                return showITabTests.FirstOrDefault(t => (t != null && t(SelThing))) != null || ShowProductLimt || ShowOutputToEntireStockpile || ShowObeysStorageFilter || ShowAdditionalSettings || ShowAreaSelectButton || ShowForbidOnPlacingSetting;
            }
        }
        bool ShowProductLimt => Machine is { ProductLimitationDisable: false };
        bool ShowOutputToEntireStockpile => (PRFB != null &&
                ((PRFB.SettingsOptions & PRFBSetting.optionOutputToEntireStockpie) > 0) &&
                // Only output to stockpile option if belt is above ground!
                !(PRFB is IBeltConveyorLinkable belt && !belt.CanSendToLevel(ConveyorLevel.Ground)));
        bool ShowObeysStorageFilter => (PRFB != null &&
                (PRFB.SettingsOptions & PRFBSetting.optionObeysStorageFilters) > 0) &&
                !(PRFB is IBeltConveyorLinkable belt && !belt.CanSendToLevel(ConveyorLevel.Ground));

        bool ShowForbidOnPlacingSetting => pRF_Building != null;

        bool ShowRangeTypeSelectorButton => ShowAreaSelectButton && compPropertiesPowerWork != null && compPropertiesPowerWork.Props.allowManualRangeTypeChange;

        bool ShowAdditionalSettings => pRF_SettingsContent != null;

        bool ShowAreaSelectButton => supplyMachineHolder != null;

        private IProductLimitation Machine => SelThing as IProductLimitation;


        private IPowerSupplyMachineHolder supplyMachineHolder => SelThing as IPowerSupplyMachineHolder;

        // private CompProperties_PowerWorkSetting compProperties_PowerWorkSetting { get => this.SelThing.GetComp<CompProperties_PowerWorkSetting>(); }


        private IPRF_SettingsContentLink pRF_SettingsContent => SelThing as IPRF_SettingsContentLink;

        private IPRF_Building pRF_Building => SelThing as IPRF_Building;

        private PRF_Building PRFB => SelThing as PRF_Building;

        private ThingWithComps selThingWithComps => SelThing as ThingWithComps;

        private CompPowerWorkSetting compPropertiesPowerWork => selThingWithComps?.GetComp<CompPowerWorkSetting>();

        private static TipSignal rotInputRangeTip = new TipSignal("PRF_SettingsITab_TipSignal_RotInputRange".Translate());


        protected override void UpdateSize()
        {
            winSize.y = 10 + 24;
            winSize.x = 400f;

            if (ShowOutputToEntireStockpile || ShowObeysStorageFilter || ShowForbidOnPlacingSetting) winSize.y += 24f + 12f;
            if (ShowOutputToEntireStockpile) winSize.y += 24f;
            if (ShowObeysStorageFilter) winSize.y += 24f;
            if (ShowForbidOnPlacingSetting) winSize.y += 24f;

            for (int i = 0; i < showITabTests.Count; i++)
            {
                if (showITabTests[i]?.Invoke(SelThing) == true)
                {
                    winSize.y += (extraHeightRequests[i]?.Invoke(SelThing) ?? 0);
                }
            }

            if (ShowProductLimt) winSize.y += 200f; //270

            if (pRF_SettingsContent != null)
            {

                winSize.y += pRF_SettingsContent.PRF_SettingsContentOb.ITab_Settings_Additional_y;
                winSize.x = Mathf.Max(winSize.x, pRF_SettingsContent.PRF_SettingsContentOb.ITab_Settings_Minimum_x);
            }
            if (ShowRangeTypeSelectorButton) winSize.y += 30f;

            float maxHeight = 900f;
            float minHeight = 70f; // if this starts too large, the window will be too high
            float inspectWindowHeight = 268f; // Note: at least one mod makes this larger - this may not be enough.
            if (UI.screenHeight > minHeight - inspectWindowHeight) maxHeight = (float)UI.screenHeight - inspectWindowHeight;
            winSize.y = Mathf.Clamp(winSize.y, minHeight, maxHeight); //Support for lower Resulutions (With that the Tab should always fit on the screen) 

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

        private string GetSlotGroupName(ISlotGroupParent slotGroup)
        {
            string appendGroup = "";
            if (slotGroup is IStorageGroupMember storageGroupMember)
            {
                if (storageGroupMember.Group != null && storageGroupMember.Group.RenamableLabel != "")
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
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, winSize.x, winSize.y).ContractedBy(10f);
            Rect inRect2;
            bool doneSection = false;

            list.Begin(inRect);

            if (ShowOutputToEntireStockpile || ShowObeysStorageFilter || ShowForbidOnPlacingSetting)
            {
                doneSection = true;
                list.Label("PRF_ITab_ProductionSettings_OutputSettings_Header".Translate());

                list.Gap(12);
            }



            if (ShowOutputToEntireStockpile)
            {
                var description = "PRF.Common.OutputToStockpileDesc".Translate();
                var label = "PRF.Common.OutputToStockpile".Translate();
                bool tmpB = PRFB.OutputToEntireStockpile;
                list.CheckboxLabeled(label, ref tmpB, description);
                if (tmpB != PRFB.OutputToEntireStockpile)
                    PRFB.OutputToEntireStockpile = tmpB;
            }
            if (ShowObeysStorageFilter)
            {
                bool tmpB = PRFB.ObeysStorageFilters;
                list.CheckboxLabeled("PRF.Common.ObeysStorageFilters".Translate(), ref tmpB,
                    "PRF.Common.ObeysStorageFiltersDesc".Translate());
                if (tmpB != PRFB.ObeysStorageFilters)
                    PRFB.ObeysStorageFilters = tmpB;
            }
            if (ShowForbidOnPlacingSetting)
            {
                bool tmpB = pRF_Building.ForbidOnPlacingDefault;
                list.CheckboxLabeled("PRF.Common.ForbidOnPlacingDefault".Translate(), ref tmpB,
                    "PRF.Common.ForbidOnPlacingDefaultDesc".Translate());
                if (tmpB != pRF_Building.ForbidOnPlacingDefault)
                    pRF_Building.ForbidOnPlacingDefault = tmpB;
            }

            //Registerd Settings
            //Whats that?
            for (int i = 0; i < showITabTests.Count; i++)
            {
                if (showITabTests[i]?.Invoke(SelThing) == true)
                {
                    if (windowContentDrawers[i] != null)
                    {
                        if (doneSection) list.GapLine();
                        windowContentDrawers[i](SelThing, list);
                        doneSection = true;
                    }
                }
            }

            //ProductLimitation
            if (Machine != null && !Machine.ProductLimitationDisable)
            {
                if (doneSection) list.GapLine();
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
                Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref limit, ref buf, 1, 1000000);
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

            //Other Registerd settings (Drone Station)
            if (pRF_SettingsContent != null)
            {
                list = pRF_SettingsContent.PRF_SettingsContentOb.ITab_Settings_AppendContent(list, inRect);
            }

            //Range Type
            if (ShowRangeTypeSelectorButton)
            {
                list.GapLine();

                inRect = list.GetRect(30f);

                Widgets.Label(inRect.LeftHalf(), "PRF_SettingsTab_RangeType_Label".Translate());
                if (Widgets.ButtonText(inRect.RightHalf(), (compPropertiesPowerWork.rangeCells as IRangeCells).ToText()))
                {
                    Find.WindowStack.Add(new FloatMenu(compPropertiesPowerWork.rangeTypes
                      .Select(d => new FloatMenuOption(d.ToText(),
                      () => compPropertiesPowerWork.rangeCells = d
                      )).ToList()));

                }
                if ((compPropertiesPowerWork.rangeCells as IRangeCells).NeedsRotate)
                {

                    inRect2 = inRect;
                    inRect2.width = 30;
                    inRect2.height = 30;
                    // - 10 as a spacer
                    inRect2.x = inRect.RightHalf().x - inRect2.width - 10;
                    //Add Rotate Button
                    if (Widgets.ButtonImage(inRect2, TexUI.RotRightTex))
                    {
                        compPropertiesPowerWork.RangeTypeRot.Rotate(RotationDirection.Clockwise);
                    }
                    TooltipHandler.TipRegion(inRect2, rotInputRangeTip);

                }
                list.Gap();
            }

            list.Gap();
            list.End();
        }
    }
}
