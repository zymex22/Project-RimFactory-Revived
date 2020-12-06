﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using ProjectRimFactory.AutoMachineTool;
using static ProjectRimFactory.AutoMachineTool.Ops;
using ProjectRimFactory.Drones;


namespace ProjectRimFactory.Common
{

    interface IPRF_SettingsContent
    {
        abstract public float ITab_Settings_Minimum_x { get; }
        abstract public float ITab_Settings_Additional_y { get; }

        //may need to pass some pos context
        abstract public Listing_Standard ITab_Settings_AppendContent(Listing_Standard list, Rect parrent_rect);



    }

    interface IPRF_SettingsContentLink
    {
        public IPRF_SettingsContent PRF_SettingsContentOb { get; }
    }





    /// <summary>
    /// An ITab that contains multilpe settings, all in one place:
    ///  * whether can output produced things to entire stockpile, or only one cell
    ///  * whether to obey IProductLimitation limits on production/storing
    /// </summary>
    class ITab_ProductionSettings : ITab {

        private Vector2 winSize = new Vector2(400f, 0f);
        private List<SlotGroup> groups;

        public ITab_ProductionSettings() {
            this.labelKey = "PRFSettingsTab";
        }

        public override bool IsVisible {
            get {
                return ShowProductLimt || ShowOutputToEntireStockpile || ShowObeysStorageFilter || ShowAdditionalSettings || ShowAreaSelectButton;
            }
        }
        bool ShowProductLimt => Machine != null;
        bool ShowOutputToEntireStockpile => ( PRFB != null && 
                ((PRFB.SettingsOptions & PRFBSetting.optionOutputToEntireStockpie) > 0) &&
                // Only output to stockpile option if belt is above ground!
                !(PRFB is IBeltConveyorLinkable belt && !belt.CanSendToLevel(ConveyorLevel.Ground)));
        bool ShowObeysStorageFilter => (PRFB != null &&
                (PRFB.SettingsOptions & PRFBSetting.optionObeysStorageFilters) > 0) &&
                !(PRFB is IBeltConveyorLinkable belt && !belt.CanSendToLevel(ConveyorLevel.Ground));

        bool ShowRangeTypeSelectorButton => ShowAreaSelectButton && compPropertiesPowerWork != null && compPropertiesPowerWork.Props.allowManualRangeTypeChange;

        bool ShowAdditionalSettings => pRF_SettingsContent != null;

        bool ShowAreaSelectButton => supplyMachineHolder != null;

        private IProductLimitation Machine { get => this.SelThing as IProductLimitation; }


        private IPowerSupplyMachineHolder supplyMachineHolder { get => this.SelThing as IPowerSupplyMachineHolder; }

        // private CompProperties_PowerWorkSetting compProperties_PowerWorkSetting { get => this.SelThing.GetComp<CompProperties_PowerWorkSetting>(); }


        private IPRF_SettingsContentLink pRF_SettingsContent { get => this.SelThing as IPRF_SettingsContentLink; }


        private PRF_Building PRFB { get => this.SelThing as PRF_Building; }

        private ThingWithComps selThingWithComps => this.SelThing as ThingWithComps;

        private CompPowerWorkSetting compPropertiesPowerWork => selThingWithComps?.GetComp<CompPowerWorkSetting>();

        private static TipSignal rotInputRangeTip = new TipSignal("PRF_SettingsITab_TipSignal_RotInputRange".Translate());


        protected override void UpdateSize() {
            winSize.y = 0;
            winSize.x = 400f;
            if (ShowProductLimt) winSize.y += 270f;
            if (ShowOutputToEntireStockpile) winSize.y += 100f;
            if (ShowObeysStorageFilter) winSize.y += 70f;
            if (pRF_SettingsContent != null) {
                
                winSize.y += pRF_SettingsContent.PRF_SettingsContentOb.ITab_Settings_Additional_y;
                winSize.x = Mathf.Max(winSize.x, pRF_SettingsContent.PRF_SettingsContentOb.ITab_Settings_Minimum_x);
            }
            if(ShowRangeTypeSelectorButton) winSize.y += 100f;

            winSize.y = Mathf.Clamp(winSize.y, 0, Prefs.ScreenHeight - 268); //Support for lower Resulutions (With that the Tab should always fit on the screen) 



            this.size = winSize;
            base.UpdateSize();
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if (Machine != null) {
                this.groups = Find.CurrentMap.haulDestinationManager.AllGroups.ToList();
                this.Machine.TargetSlotGroup = this.Machine.TargetSlotGroup.Where(s => this.groups.Contains(s));
            }
        }

        protected override void FillTab()
        {
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, winSize.x, winSize.y).ContractedBy(10f);
            Rect inRect2;
            bool doneSection = false;

            list.Begin(inRect);
            list.Gap(24);
            if (ShowOutputToEntireStockpile) {
                if (doneSection) list.GapLine();
                doneSection = true;
                var description = "PRF.Common.OutputToStockpileDesc".Translate();
                var label = "PRF.Common.OutputToStockpile".Translate();
                bool tmpB = PRFB.OutputToEntireStockpile;
                list.CheckboxLabeled(label, ref tmpB, description);
                if (tmpB != PRFB.OutputToEntireStockpile)
                    PRFB.OutputToEntireStockpile = tmpB;
            }
            if (ShowObeysStorageFilter) {
                if (doneSection) list.GapLine();
                doneSection = true;
                bool tmpB = PRFB.ObeysStorageFilters;
                list.CheckboxLabeled("PRF.Common.ObeysStorageFilters".Translate(), ref tmpB,
                    "PRF.Common.ObeysStorageFiltersDesc".Translate());
                if (tmpB != PRFB.ObeysStorageFilters)
                    PRFB.ObeysStorageFilters = tmpB;
            }
            if (Machine != null) {
                if (doneSection) list.GapLine();
                doneSection = true;
                var description = "PRF.AutoMachineTool.ProductLimitation.Description".Translate();
                var label = "PRF.AutoMachineTool.ProductLimitation.ValueLabel".Translate();
                var checkBoxLabel = "PRF.AutoMachineTool.ProductLimitation.CheckBoxLabel".Translate();
                var stackCountLabel = "PRF.AutoMachineTool.ProductLimitation.CountStacks".Translate();

                // Why did the OP decide to make labels in rects instead of using the Listing_Standard?
                //   If a language ever makes this too long for 70f, use list.Label() instead and make
                //   everything in a scrollview, eh?
                var rect = list.GetRect(70f);
                Widgets.Label(rect, description);
                list.Gap();

                rect = list.GetRect(30f);
                bool limitation = this.Machine.ProductLimitation;
                Widgets.CheckboxLabeled(rect, checkBoxLabel, ref limitation);
                this.Machine.ProductLimitation = limitation;
                list.Gap();

                rect = list.GetRect(30f);
                string buf = this.Machine.ProductLimitCount.ToString();
                int limit = this.Machine.ProductLimitCount;
                Widgets.Label(rect.LeftHalf(), label);
                Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref limit, ref buf, 1, 1000000);
                list.Gap();

                rect = list.GetRect(30f);
                bool countStacks = this.Machine.CountStacks;
                Widgets.CheckboxLabeled(rect, stackCountLabel, ref countStacks);
                this.Machine.CountStacks = countStacks;
                list.Gap();

                rect = list.GetRect(30f);
                Widgets.Label(rect.LeftHalf(), "PRF.AutoMachineTool.CountZone".Translate());
                if (Widgets.ButtonText(rect.RightHalf(), this.Machine.TargetSlotGroup.Fold("PRF.AutoMachineTool.EntierMap".Translate())(s => s.parent.SlotYielderLabel()))) {
                    Find.WindowStack.Add(new FloatMenu(groups
                        .Select(g => new FloatMenuOption(g.parent.SlotYielderLabel(), () => this.Machine.TargetSlotGroup = Option(g)))
                        .ToList()
                        .Head(new FloatMenuOption("PRF.AutoMachineTool.EntierMap".Translate(), () => this.Machine.TargetSlotGroup = Nothing<SlotGroup>()))));
                }
                this.Machine.ProductLimitCount = limit;
            }
            
            if (pRF_SettingsContent != null)
            {

                list = pRF_SettingsContent.PRF_SettingsContentOb.ITab_Settings_AppendContent(list, inRect);

            }
            if (ShowRangeTypeSelectorButton)
            {

                
                inRect = list.GetRect(30f);
                Widgets.DrawLineHorizontal(inRect.x, inRect.y - 5, inRect.width);

                Widgets.Label(inRect.LeftHalf(), "PRF_SettingsTab_RangeType_Label".Translate());
                if (Widgets.ButtonText(inRect.RightHalf(), ( compPropertiesPowerWork.rangeCells as IRangeCells).ToText() ))
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
