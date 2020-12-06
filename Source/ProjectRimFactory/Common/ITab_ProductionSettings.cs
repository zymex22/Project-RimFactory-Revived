using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.AutoMachineTool;
using RimWorld;
using UnityEngine;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;


namespace ProjectRimFactory.Common
{
    internal interface IPRF_SettingsContent
    {
        public abstract float ITab_Settings_Minimum_x { get; }
        public abstract float ITab_Settings_Additional_y { get; }

        //may need to pass some pos context
        public abstract Listing_Standard ITab_Settings_AppendContent(Listing_Standard list, Rect parrent_rect);
    }

    internal interface IPRF_SettingsContentLink
    {
        public IPRF_SettingsContent PRF_SettingsContentOb { get; }
    }


    /// <summary>
    ///     An ITab that contains multilpe settings, all in one place:
    ///     * whether can output produced things to entire stockpile, or only one cell
    ///     * whether to obey IProductLimitation limits on production/storing
    /// </summary>
    internal class ITab_ProductionSettings : ITab
    {
        private List<SlotGroup> groups;

        private Vector2 winSize = new Vector2(400f, 0f);

        public ITab_ProductionSettings()
        {
            labelKey = "PRFSettingsTab";
        }

        public override bool IsVisible => ShowProductLimt || ShowOutputToEntireStockpile || ShowObeysStorageFilter ||
                                          ShowAdditionalSettings || ShowAreaSelectButton;

        private bool ShowProductLimt => Machine != null;

        private bool ShowOutputToEntireStockpile => PRFB != null &&
                                                    (PRFB.SettingsOptions & PRFBSetting.optionOutputToEntireStockpie) >
                                                    0 &&
                                                    // Only output to stockpile option if belt is above ground!
                                                    !(PRFB is IBeltConveyorLinkable belt &&
                                                      !belt.CanSendToLevel(ConveyorLevel.Ground));

        private bool ShowObeysStorageFilter => PRFB != null &&
                                               (PRFB.SettingsOptions & PRFBSetting.optionObeysStorageFilters) > 0 &&
                                               !(PRFB is IBeltConveyorLinkable belt &&
                                                 !belt.CanSendToLevel(ConveyorLevel.Ground));

        private bool ShowRangeTypeSelectorButton => ShowAreaSelectButton && compPropertiesPowerWork != null &&
                                                    compPropertiesPowerWork.Props.allowManualRangeTypeChange;

        private bool ShowAdditionalSettings => pRF_SettingsContent != null;

        private bool ShowAreaSelectButton => supplyMachineHolder != null;

        private IProductLimitation Machine => SelThing as IProductLimitation;


        private IPowerSupplyMachineHolder supplyMachineHolder => SelThing as IPowerSupplyMachineHolder;

        // private CompProperties_PowerWorkSetting compProperties_PowerWorkSetting { get => this.SelThing.GetComp<CompProperties_PowerWorkSetting>(); }


        private IPRF_SettingsContentLink pRF_SettingsContent => SelThing as IPRF_SettingsContentLink;


        private PRF_Building PRFB => SelThing as PRF_Building;

        private ThingWithComps selThingWithComps => SelThing as ThingWithComps;

        private CompPowerWorkSetting compPropertiesPowerWork => selThingWithComps?.GetComp<CompPowerWorkSetting>();

        protected override void UpdateSize()
        {
            winSize.y = 0;
            winSize.x = 400f;
            if (ShowProductLimt) winSize.y += 270f;
            if (ShowOutputToEntireStockpile) winSize.y += 100f;
            if (ShowObeysStorageFilter) winSize.y += 70f;
            if (pRF_SettingsContent != null)
            {
                winSize.y += pRF_SettingsContent.PRF_SettingsContentOb.ITab_Settings_Additional_y;
                winSize.x = Mathf.Max(winSize.x, pRF_SettingsContent.PRF_SettingsContentOb.ITab_Settings_Minimum_x);
            }

            if (ShowRangeTypeSelectorButton) winSize.y += 100f;

            winSize.y = Mathf.Clamp(winSize.y, 0,
                Prefs.ScreenHeight -
                268); //Support for lower Resulutions (With that the Tab should always fit on the screen) 


            size = winSize;
            base.UpdateSize();
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if (Machine != null)
            {
                groups = Find.CurrentMap.haulDestinationManager.AllGroups.ToList();
                Machine.TargetSlotGroup = Machine.TargetSlotGroup.Where(s => groups.Contains(s));
            }
        }

        protected override void FillTab()
        {
            var list = new Listing_Standard();
            var inRect = new Rect(0f, 0f, winSize.x, winSize.y).ContractedBy(10f);
            var doneSection = false;

            list.Begin(inRect);
            list.Gap(24);
            if (ShowOutputToEntireStockpile)
            {
                if (doneSection) list.GapLine();
                doneSection = true;
                var description = "PRF.Common.OutputToStockpileDesc".Translate();
                var label = "PRF.Common.OutputToStockpile".Translate();
                var tmpB = PRFB.OutputToEntireStockpile;
                list.CheckboxLabeled(label, ref tmpB, description);
                if (tmpB != PRFB.OutputToEntireStockpile)
                    PRFB.OutputToEntireStockpile = tmpB;
            }

            if (ShowObeysStorageFilter)
            {
                if (doneSection) list.GapLine();
                doneSection = true;
                var tmpB = PRFB.ObeysStorageFilters;
                list.CheckboxLabeled("PRF.Common.ObeysStorageFilters".Translate(), ref tmpB,
                    "PRF.Common.ObeysStorageFiltersDesc".Translate());
                if (tmpB != PRFB.ObeysStorageFilters)
                    PRFB.ObeysStorageFilters = tmpB;
            }

            if (Machine != null)
            {
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
                var limitation = Machine.ProductLimitation;
                Widgets.CheckboxLabeled(rect, checkBoxLabel, ref limitation);
                Machine.ProductLimitation = limitation;
                list.Gap();

                rect = list.GetRect(30f);
                var buf = Machine.ProductLimitCount.ToString();
                var limit = Machine.ProductLimitCount;
                Widgets.Label(rect.LeftHalf(), label);
                Widgets.TextFieldNumeric(rect.RightHalf(), ref limit, ref buf, 1, 1000000);
                list.Gap();

                rect = list.GetRect(30f);
                var countStacks = Machine.CountStacks;
                Widgets.CheckboxLabeled(rect, stackCountLabel, ref countStacks);
                Machine.CountStacks = countStacks;
                list.Gap();

                rect = list.GetRect(30f);
                Widgets.Label(rect.LeftHalf(), "PRF.AutoMachineTool.CountZone".Translate());
                if (Widgets.ButtonText(rect.RightHalf(),
                    Machine.TargetSlotGroup.Fold("PRF.AutoMachineTool.EntierMap".Translate())(s =>
                        s.parent.SlotYielderLabel())))
                    Find.WindowStack.Add(new FloatMenu(groups
                        .Select(g =>
                            new FloatMenuOption(g.parent.SlotYielderLabel(), () => Machine.TargetSlotGroup = Option(g)))
                        .ToList()
                        .Head(new FloatMenuOption("PRF.AutoMachineTool.EntierMap".Translate(),
                            () => Machine.TargetSlotGroup = Nothing<SlotGroup>()))));
                Machine.ProductLimitCount = limit;
            }

            if (pRF_SettingsContent != null)
                list = pRF_SettingsContent.PRF_SettingsContentOb.ITab_Settings_AppendContent(list, inRect);
            if (ShowRangeTypeSelectorButton)
            {
                inRect = list.GetRect(30f);
                Widgets.Label(inRect.LeftHalf(), "PRF_SettingsTab_RangeType_Label".Translate());
                if (Widgets.ButtonText(inRect.RightHalf(), compPropertiesPowerWork.rangeCells.ToText()))
                    Find.WindowStack.Add(new FloatMenu(compPropertiesPowerWork.rangeTypes
                        .Select(d => new FloatMenuOption(d.ToText(),
                            () => compPropertiesPowerWork.rangeCells = d
                        )).ToList()));
                list.Gap();
            }


            list.Gap();
            list.End();
        }
    }
}