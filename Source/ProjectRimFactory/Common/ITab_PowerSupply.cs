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

        FloatRange FloatRange_Range { get; }
        float CurrentRange { get; }

        FloatRange FloatRange_SpeedFactor { get; }
        float CurrentSpeedFactor { get; }

        float SupplyPowerForSpeed { get; set; }
        float SupplyPowerForRange { get; set; }

        bool Glowable { get; }
        bool Glow { get; set; }
        bool SpeedSetting { get; }
        bool RangeSetting { get; }

        void RefreshPowerStatus();
    }

    class ITab_PowerSupply : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(600f, 130f);

        private static readonly float HeightSpeed = 120 - 25;

        private static readonly float HeightRange = 100 - 25;

        private static readonly float HeightGlow = 30;

        public ITab_PowerSupply()
        {
            this.size = WinSize;
            this.labelKey = "PRF.AutoMachineTool.SupplyPower.TabName";

            this.descriptionForSpeed = "PRF.AutoMachineTool.SupplyPower.Description".Translate();
            this.descriptionForRange = "PRF.AutoMachineTool.SupplyPower.DescriptionForRange".Translate();
        }

        private string descriptionForSpeed;

        private string descriptionForRange;

        private IPowerSupplyMachine Machine => (this.SelThing as IPowerSupplyMachineHolder)?.RangePowerSupplyMachine;

        public override bool IsVisible => this.Machine != null && (this.Machine.SpeedSetting || this.Machine.RangeSetting);

        public override void TabUpdate()
        {
            base.TabUpdate();

            float additionalHeight = (this.Machine.SpeedSetting ? HeightSpeed : 0) + (this.Machine.RangeSetting ? HeightRange : 0) + (this.Machine.Glowable ? HeightGlow : 0);
            this.size = new Vector2(WinSize.x, WinSize.y + additionalHeight);
            this.UpdateSize();
        }

        public override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void FillTab()
        {
            TextAnchor anchor;
            GameFont font;

            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);

            list.Begin(inRect);

            list.Gap();
            var rect = new Rect();

            //Add Power usage Breackdown
            rect = list.GetRect(50f);
            //TODO Use string builder
            string powerUsageBreackdown;
            powerUsageBreackdown = "PRF.AutoMachineTool.SupplyPower.BreakDownLine_Start".Translate(this.Machine.BasePowerConsumption, this.Machine.SupplyPowerForSpeed, this.Machine.SupplyPowerForRange);
            //Add breackdown for additional Power usage if any
            if (this.Machine.AdditionalPowerConsumption != null && this.Machine.AdditionalPowerConsumption.Count > 0)
            {
                foreach (KeyValuePair<string, int> pair in this.Machine.AdditionalPowerConsumption)
                {
                    powerUsageBreackdown += "PRF.AutoMachineTool.SupplyPower.BreakDownLine_Append".Translate(pair.Key, pair.Value);
                }
            }
            //Display the Sum
            powerUsageBreackdown += "PRF.AutoMachineTool.SupplyPower.BreakDownLine_End".Translate(-1 * this.Machine.CurrentPowerConsumption);
            Widgets.Label(rect, powerUsageBreackdown);
            rect = list.GetRect(10f);
            Widgets.DrawLineHorizontal(rect.x, rect.y, WinSize.x);

            //----------------------------

            if (this.Machine.SpeedSetting)
            {
                int minPowerSpeed = 0;
                int maxPowerSpeed = this.Machine.MaxPowerForSpeed;

                string valueLabelForSpeed = "PRF.AutoMachineTool.SupplyPower.ValueLabelForSpeed".Translate(Machine.SupplyPowerForSpeed);

                // for speed
                rect = list.GetRect(30f);
                Widgets.Label(rect, descriptionForSpeed);
                list.Gap();

                rect = list.GetRect(20f);
                var speed = (int)Widgets.HorizontalSlider_NewTemp(rect, (float)this.Machine.SupplyPowerForSpeed, (float)minPowerSpeed, (float)maxPowerSpeed, true, valueLabelForSpeed,
                    "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(minPowerSpeed), "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(maxPowerSpeed), this.Machine.PowerPerStepSpeed);
                this.Machine.SupplyPowerForSpeed = speed;
                //Add info Labels below
                rect = list.GetRect(30f);
                anchor = Text.Anchor;
                font = Text.Font;
                Text.Font = GameFont.Tiny;

                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.PercentLabel".Translate((this.Machine.FloatRange_SpeedFactor.min / this.Machine.FloatRange_SpeedFactor.min) * 100));
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.PercentLabel".Translate((this.Machine.FloatRange_SpeedFactor.max / this.Machine.FloatRange_SpeedFactor.min) * 100));
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CurrentPercent".Translate((this.Machine.CurrentSpeedFactor / this.Machine.FloatRange_SpeedFactor.min) * 100));
                Text.Anchor = anchor;
                Text.Font = font;

                list.Gap();

                //Check if this.Machine.RangeSetting is active to place a Devider line
                if (this.Machine.RangeSetting)
                {
                    rect = list.GetRect(10f);
                    Widgets.DrawLineHorizontal(rect.x, rect.y, WinSize.x);
                }
            }

            if (this.Machine.RangeSetting)
            {
                int minPowerRange = 0;
                int maxPowerRange = this.Machine.MaxPowerForRange;

                string valueLabelForRange = "PRF.AutoMachineTool.SupplyPower.ValueLabelForRange".Translate(Machine.SupplyPowerForRange);

                // for range
                rect = list.GetRect(30f);
                Widgets.Label(rect, descriptionForRange);
                list.Gap();

                rect = list.GetRect(20f);
                var range = Widgets.HorizontalSlider_NewTemp(rect, (float)this.Machine.SupplyPowerForRange, (float)minPowerRange, (float)maxPowerRange, true, valueLabelForRange,
                    "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(minPowerRange), "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(maxPowerRange), this.Machine.PowerPerStepRange);
                this.Machine.SupplyPowerForRange = range;
                //Add info Labels below
                rect = list.GetRect(30f);
                anchor = Text.Anchor;
                font = Text.Font;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CellsLabel".Translate(this.Machine.FloatRange_Range.min));
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CellsLabel".Translate(this.Machine.FloatRange_Range.max));
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CurrentCellRadius".Translate(this.Machine.CurrentRange));
                Text.Anchor = anchor;
                Text.Font = font;
                list.Gap();
            }


            //TODO Maybe move this to the settings tab
            if (this.Machine.Glowable)
            {
                rect = list.GetRect(30f);
                bool glow = this.Machine.Glow;
                Widgets.CheckboxLabeled(rect, "PRF.AutoMachineTool.SupplyPower.SunLampText".Translate(), ref glow);
                this.Machine.Glow = glow;
            }
            list.Gap();

            list.End();
        }
    }
}
