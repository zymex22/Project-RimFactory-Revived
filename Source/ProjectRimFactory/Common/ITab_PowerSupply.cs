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
        IPowerSupplyMachine PowerSupplyMachine { get; }
    }

    public interface IPowerSupplyMachine
    {
        int MinPowerForSpeed { get; }
        int MaxPowerForSpeed { get; }
        float SupplyPowerForSpeed { get; set; }
    }

    class ITab_PowerSupply : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(600f, 250f);

        public ITab_PowerSupply()
        {
            this.size = WinSize;
            this.labelKey = "PRF.AutoMachineTool.SupplyPower.TabName";

            this.description = "PRF.AutoMachineTool.SupplyPower.Description".Translate();
        }
        
        private string description;
        
        public IPowerSupplyMachine Machine => (this.SelThing as IPowerSupplyMachineHolder)?.PowerSupplyMachine;

        public override bool IsVisible => this.Machine != null;

        protected override void FillTab()
        {
            int round = this.Machine.MinPowerForSpeed < 1000 ? 100 : this.Machine.MinPowerForSpeed < 10000 ? 500 : 1000;
            if (this.Machine.MinPowerForSpeed % round != 0 || this.Machine.MaxPowerForSpeed % round != 0)
            {
                round = 1;
            }

            string valueLabel = "PRF.AutoMachineTool.SupplyPower.ValueLabel".Translate() + " (" + this.Machine.MinPowerForSpeed + " to " + this.Machine.MaxPowerForSpeed + ") ";

            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            list.Begin(inRect);
            list.Gap();

            var rect = list.GetRect(50f);
            Widgets.Label(rect, description);
            list.Gap();

            rect = list.GetRect(50f);
            var val = (int)Widgets.HorizontalSlider(rect, (float)this.Machine.SupplyPowerForSpeed, (float)this.Machine.MinPowerForSpeed, (float)this.Machine.MaxPowerForSpeed, true, valueLabel, this.Machine.MinPowerForSpeed.ToString(), this.Machine.MaxPowerForSpeed.ToString(), round);
            this.Machine.SupplyPowerForSpeed = val;
            list.Gap();

            rect = list.GetRect(30f);
            string buf = this.Machine.SupplyPowerForSpeed.ToString();
            int power = (int)this.Machine.SupplyPowerForSpeed;
            Widgets.Label(rect.LeftHalf(), valueLabel);
            Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref power, ref buf, this.Machine.MinPowerForSpeed, this.Machine.MaxPowerForSpeed);
            list.Gap();

            list.End();
            this.Machine.SupplyPowerForSpeed = power;
        }
    }
}
