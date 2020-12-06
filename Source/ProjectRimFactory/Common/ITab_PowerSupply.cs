using RimWorld;
using UnityEngine;
using Verse;

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

        void RefreshPowerStatus();
    }

    internal class ITab_PowerSupply : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(600f, 130f);

        private static readonly float HeightSpeed = 120;

        private static readonly float HeightRange = 100;

        private static readonly float HeightGlow = 30;

        private readonly string descriptionForRange;

        private readonly string descriptionForSpeed;

        public ITab_PowerSupply()
        {
            size = WinSize;
            labelKey = "PRF.AutoMachineTool.SupplyPower.TabName";

            descriptionForSpeed = "PRF.AutoMachineTool.SupplyPower.Description".Translate();
            descriptionForRange = "PRF.AutoMachineTool.SupplyPower.DescriptionForRange".Translate();
        }

        private IPowerSupplyMachine Machine => (SelThing as IPowerSupplyMachineHolder)?.RangePowerSupplyMachine;

        public override bool IsVisible => Machine != null && (Machine.SpeedSetting || Machine.RangeSetting);

        public override void TabUpdate()
        {
            base.TabUpdate();

            var additionalHeight = (Machine.SpeedSetting ? HeightSpeed : 0) + (Machine.RangeSetting ? HeightRange : 0) +
                                   (Machine.Glowable ? HeightGlow : 0);
            size = new Vector2(WinSize.x, WinSize.y + additionalHeight);
            UpdateSize();
        }

        public override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void FillTab()
        {
            var list = new Listing_Standard();
            var inRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            list.Begin(inRect);

            list.Gap();
            var rect = new Rect();

            if (Machine.SpeedSetting)
            {
                var minPowerSpeed = Machine.MinPowerForSpeed;
                var maxPowerSpeed = Machine.MaxPowerForSpeed;

                var valueLabelForSpeed = "PRF.AutoMachineTool.SupplyPower.ValueLabelForSpeed".Translate() + " (" +
                                         minPowerSpeed + " to " + maxPowerSpeed + ") ";

                // for speed
                rect = list.GetRect(50f);
                Widgets.Label(rect, descriptionForSpeed);
                list.Gap();

                rect = list.GetRect(50f);
                var speed = (int) Widgets.HorizontalSlider(rect, Machine.SupplyPowerForSpeed, minPowerSpeed,
                    maxPowerSpeed, true, valueLabelForSpeed, minPowerSpeed.ToString(), maxPowerSpeed.ToString(), 50);
                Machine.SupplyPowerForSpeed = speed;
                list.Gap();

                rect = list.GetRect(30f);
                var buf = Machine.SupplyPowerForSpeed.ToString();
                var power = (int) Machine.SupplyPowerForSpeed;
                Widgets.Label(rect.LeftHalf(), valueLabelForSpeed);
                Widgets.TextFieldNumeric(rect.RightHalf(), ref power, ref buf, Machine.SupplyPowerForSpeed,
                    Machine.SupplyPowerForSpeed);
                list.Gap();
                Machine.SupplyPowerForSpeed = power;
            }

            if (Machine.RangeSetting)
            {
                var minPowerRange = Machine.MinPowerForRange;
                var maxPowerRange = Machine.MaxPowerForRange;

                var valueLabelForRange = "PRF.AutoMachineTool.SupplyPower.ValueLabelForRange".Translate() + " (" +
                                         minPowerRange + " to " + maxPowerRange + ") ";

                // for range
                rect = list.GetRect(50f);
                Widgets.Label(rect, descriptionForRange);
                list.Gap();

                rect = list.GetRect(50f);
                var range = Widgets.HorizontalSlider(rect, Machine.SupplyPowerForRange, minPowerRange, maxPowerRange,
                    true, valueLabelForRange, minPowerRange.ToString(), maxPowerRange.ToString(),
                    Machine.RangeInterval);
                Machine.SupplyPowerForRange = range;
                list.Gap();
            }

            if (Machine.Glowable)
            {
                rect = list.GetRect(30f);
                var glow = Machine.Glow;
                Widgets.CheckboxLabeled(rect, "PRF.AutoMachineTool.SupplyPower.SunLampText".Translate(), ref glow);
                Machine.Glow = glow;
            }

            list.Gap();

            list.End();
        }
    }
}