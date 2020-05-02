using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class CompPowerWorkSpeed : ThingComp, IPowerSupplyMachine
    {
        public CompProperties_PowerWorkSpeed Props => (CompProperties_PowerWorkSpeed)this.props;

        public int MinPowerForSpeed => this.Props.minPower;

        public int MaxPowerForSpeed => this.Props.maxPower;

        public float SupplyPowerForSpeed
        {
            get => this.powerForSpeed;
            set
            {
                this.powerForSpeed = value;
                this.AdjustPower();
                this.SetPower();
            }
        }

        private float powerForSpeed = 0;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.powerForSpeed, "powerForSpeed");
        }

        [Unsaved]
        private CompPowerTrader powerComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                this.powerForSpeed = this.Props.minPower;
            }
            this.powerComp = this.parent.TryGetComp<CompPowerTrader>();
            this.AdjustPower();
        }

        protected virtual void AdjustPower()
        {
            if(this.powerForSpeed < this.MinPowerForSpeed)
            {
                this.powerForSpeed = this.MinPowerForSpeed;
            }
            if (this.powerForSpeed > this.MaxPowerForSpeed)
            {
                this.powerForSpeed = this.MaxPowerForSpeed;
            }
        }

        protected virtual void SetPower()
        {
            if(this.powerComp != null)
            {
                this.powerComp.PowerOutput = this.powerComp.Props.basePowerConsumption - this.powerForSpeed;
            }
        }

        public float GetSpeedFactor()
        {
            var f = (this.powerForSpeed - this.Props.minPower) / (this.Props.maxPower - this.powerForSpeed);
            return Mathf.Lerp(this.Props.minSpeedFactor, this.Props.maxSpeedFactor, f);
        }
    }

    public class CompProperties_PowerWorkSpeed : CompProperties
    {
        public int maxPower = 2000;
        public int minPower = 1000;

        public float minSpeedFactor = 1;
        public float maxSpeedFactor = 2;

        public CompProperties_PowerWorkSpeed()
        {
            this.compClass = typeof(CompPowerWorkSpeed);
        }
    }
}
