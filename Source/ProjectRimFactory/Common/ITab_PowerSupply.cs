using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.Common
{
    public interface IPowerSupplyMachineHolder
    {
        IPowerSupplyMachine RangePowerSupplyMachine { get; }
    }

    public interface IPowerSupplyMachine
    {
        int MinPowerForSpeed { get; }
        int MaxPowerForSpeed { get; }
        int MinPowerForRange { get; }
        int MaxPowerForRange { get; }

        float SupplyPowerForSpeed { get; set; }
        float SupplyPowerForRange { get; set; }

        float RangeInterval { get; }

        bool Glowable { get; }
        bool Glow { get; set; }
        bool SpeedSetting { get; }
        bool RangeSetting { get; }
    }

    class ITab_PowerSupply : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(600f, 130f);

        private static readonly float HeightSpeed = 120;

        private static readonly float HeightRange = 100;

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
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);

            list.Begin(inRect);

            list.Gap();
            var rect = new Rect();

            if (this.Machine.SpeedSetting)
            {
                int minPowerSpeed = this.Machine.MinPowerForSpeed;
                int maxPowerSpeed = this.Machine.MaxPowerForSpeed;

                string valueLabelForSpeed = "PRF.AutoMachineTool.SupplyPower.ValueLabelForSpeed".Translate() + " (" + minPowerSpeed + " to " + maxPowerSpeed + ") ";

                // for speed
                rect = list.GetRect(50f);
                Widgets.Label(rect, descriptionForSpeed);
                list.Gap();

                rect = list.GetRect(50f);
                var speed = (int)Widgets.HorizontalSlider(rect, (float)this.Machine.SupplyPowerForSpeed, (float)minPowerSpeed, (float)maxPowerSpeed, true, valueLabelForSpeed, minPowerSpeed.ToString(), maxPowerSpeed.ToString(), 100);
                this.Machine.SupplyPowerForSpeed = speed;
                list.Gap();

                rect = list.GetRect(30f);
                string buf = this.Machine.SupplyPowerForSpeed.ToString();
                int power = (int)this.Machine.SupplyPowerForSpeed;
                Widgets.Label(rect.LeftHalf(), valueLabelForSpeed);
                Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref power, ref buf, this.Machine.SupplyPowerForSpeed, this.Machine.SupplyPowerForSpeed);
                list.Gap();
                this.Machine.SupplyPowerForSpeed = power;
            }

            if (this.Machine.RangeSetting)
            {
                int minPowerRange = this.Machine.MinPowerForRange;
                int maxPowerRange = this.Machine.MaxPowerForRange;

                string valueLabelForRange = "PRF.AutoMachineTool.SupplyPower.ValueLabelForRange".Translate() + " (" + minPowerRange + " to " + maxPowerRange + ") ";

                // for range
                rect = list.GetRect(50f);
                Widgets.Label(rect, descriptionForRange);
                list.Gap();

                rect = list.GetRect(50f);
                var range = (int)Widgets.HorizontalSlider(rect, (float)this.Machine.SupplyPowerForRange, (float)minPowerRange, (float)maxPowerRange, true, valueLabelForRange, minPowerRange.ToString(), maxPowerRange.ToString(), this.Machine.RangeInterval);
                this.Machine.SupplyPowerForRange = range;
                list.Gap();
            }

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
