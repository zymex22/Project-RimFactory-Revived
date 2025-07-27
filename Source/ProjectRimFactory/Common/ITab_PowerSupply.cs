using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public interface IPowerSupplyMachineHolder
    {
        IPowerSupplyMachine RangePowerSupplyMachine { get; }
    }

    public interface IAdditionalPowerConsumption
    {
        Dictionary<string, int> AdditionalPowerConsumption { get; }
    }

    public interface IPowerSupplyMachine
    {
        float BasePowerConsumption { get; }
        int CurrentPowerConsumption { get; }

        //Strig will be formated for the overview and the value will hold the additional consumption
        Dictionary<string, int> AdditionalPowerConsumption { get; }

        int MaxPowerForSpeed { get; }
        int MaxPowerForRange { get; }

        float PowerPerStepSpeed { get; }
        float PowerPerStepRange { get; }

        FloatRange FloatRangeRange { get; }
        float CurrentRange { get; }

        FloatRange FloatRangeSpeedFactor { get; }
        float CurrentSpeedFactor { get; }

        float SupplyPowerForSpeed { get; set; }
        float SupplyPowerForRange { get; set; }

        bool Glowable { get; }
        bool Glow { get; set; }
        bool SpeedSetting { get; }
        bool RangeSetting { get; }

        void RefreshPowerStatus();
    }

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    class ITab_PowerSupply : ITab
    {
        private static readonly Vector2 WinSize = new(600f, 130f);

        private const float HeightSpeed = 120 - 25;

        private const float HeightRange = 100 - 25;

        private const float HeightGlow = 30;

        public ITab_PowerSupply()
        {
            size = WinSize;
            labelKey = "PRF.AutoMachineTool.SupplyPower.TabName";

            descriptionForSpeed = "PRF.AutoMachineTool.SupplyPower.Description".Translate();
            descriptionForRange = "PRF.AutoMachineTool.SupplyPower.DescriptionForRange".Translate();
        }

        private readonly string descriptionForSpeed;

        private readonly string descriptionForRange;

        private IPowerSupplyMachine Machine => (SelThing as IPowerSupplyMachineHolder)?.RangePowerSupplyMachine;

        public override bool IsVisible => Machine != null && (Machine.SpeedSetting || Machine.RangeSetting);

        public override void TabUpdate()
        {
            base.TabUpdate();

            var additionalHeight = (Machine.SpeedSetting ? HeightSpeed : 0) + (Machine.RangeSetting ? HeightRange : 0) + (Machine.Glowable ? HeightGlow : 0);
            size = new Vector2(WinSize.x, WinSize.y + additionalHeight);
            UpdateSize();
        }

        protected override void FillTab()
        {
            TextAnchor anchor;
            GameFont font;

            var list = new Listing_Standard();
            var inRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            list.Begin(inRect);

            list.Gap();

            //Add Power usage Breackdown
            var rect = list.GetRect(50f);
            //TODO Use string builder
            string powerUsageBreackdown;
            powerUsageBreackdown = "PRF.AutoMachineTool.SupplyPower.BreakDownLine_Start".Translate(Machine.BasePowerConsumption, Machine.SupplyPowerForSpeed, Machine.SupplyPowerForRange);
            //Add breakdown for additional Power usage if any
            if (Machine.AdditionalPowerConsumption != null && Machine.AdditionalPowerConsumption.Count > 0)
            {
                foreach (var pair in Machine.AdditionalPowerConsumption)
                {
                    powerUsageBreackdown += "PRF.AutoMachineTool.SupplyPower.BreakDownLine_Append".Translate(pair.Key, pair.Value);
                }
            }
            //Display the Sum
            powerUsageBreackdown += "PRF.AutoMachineTool.SupplyPower.BreakDownLine_End".Translate(-1 * Machine.CurrentPowerConsumption);
            Widgets.Label(rect, powerUsageBreackdown);
            rect = list.GetRect(10f);
            Widgets.DrawLineHorizontal(rect.x, rect.y, WinSize.x);

            //----------------------------

            if (Machine.SpeedSetting)
            {
                var minPowerSpeed = 0;
                var maxPowerSpeed = Machine.MaxPowerForSpeed;

                string valueLabelForSpeed = "PRF.AutoMachineTool.SupplyPower.ValueLabelForSpeed".Translate(Machine.SupplyPowerForSpeed);

                // for speed
                rect = list.GetRect(30f);
                Widgets.Label(rect, descriptionForSpeed);
                list.Gap();

                rect = list.GetRect(20f);
                var speed = (int)Widgets.HorizontalSlider(rect, Machine.SupplyPowerForSpeed, minPowerSpeed, maxPowerSpeed, true, valueLabelForSpeed,
                    "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(minPowerSpeed), "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(maxPowerSpeed), Machine.PowerPerStepSpeed);
                Machine.SupplyPowerForSpeed = speed;
                //Add info Labels below
                rect = list.GetRect(30f);
                anchor = Text.Anchor;
                font = Text.Font;
                Text.Font = GameFont.Tiny;

                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.PercentLabel".Translate((Machine.FloatRangeSpeedFactor.min / Machine.FloatRangeSpeedFactor.min) * 100));
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.PercentLabel".Translate((Machine.FloatRangeSpeedFactor.max / Machine.FloatRangeSpeedFactor.min) * 100));
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CurrentPercent".Translate((Machine.CurrentSpeedFactor / Machine.FloatRangeSpeedFactor.min) * 100));
                Text.Anchor = anchor;
                Text.Font = font;

                list.Gap();

                //Check if this.Machine.RangeSetting is active to place a Devider line
                if (Machine.RangeSetting)
                {
                    rect = list.GetRect(10f);
                    Widgets.DrawLineHorizontal(rect.x, rect.y, WinSize.x);
                }
            }

            if (Machine.RangeSetting)
            {
                int minPowerRange = 0;
                int maxPowerRange = Machine.MaxPowerForRange;

                string valueLabelForRange = "PRF.AutoMachineTool.SupplyPower.ValueLabelForRange".Translate(Machine.SupplyPowerForRange);

                // for range
                rect = list.GetRect(30f);
                Widgets.Label(rect, descriptionForRange);
                list.Gap();

                rect = list.GetRect(20f);
                var range = Widgets.HorizontalSlider(rect, Machine.SupplyPowerForRange, minPowerRange, maxPowerRange, true, valueLabelForRange,
                    "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(minPowerRange), "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(maxPowerRange), Machine.PowerPerStepRange);
                Machine.SupplyPowerForRange = range;
                //Add info Labels below
                rect = list.GetRect(30f);
                anchor = Text.Anchor;
                font = Text.Font;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CellsLabel".Translate(Machine.FloatRangeRange.min));
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CellsLabel".Translate(Machine.FloatRangeRange.max));
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CurrentCellRadius".Translate(Machine.CurrentRange));
                Text.Anchor = anchor;
                Text.Font = font;
                list.Gap();
            }


            //TODO Maybe move this to the settings tab
            if (Machine.Glowable)
            {
                rect = list.GetRect(30f);
                bool glow = Machine.Glow;
                Widgets.CheckboxLabeled(rect, "PRF.AutoMachineTool.SupplyPower.SunLampText".Translate(), ref glow);
                Machine.Glow = glow;
            }
            list.Gap();

            list.End();
        }
    }
}
