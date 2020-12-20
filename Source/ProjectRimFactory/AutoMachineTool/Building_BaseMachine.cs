using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using ProjectRimFactory.Common;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class Building_BaseMachine<T> : Building_Base<T>, IPowerSupplyMachineHolder, IPowerSupplyMachine, IBeltConveyorSender where T : Thing
    {

        public CompPowerWorkSetting powerWorkSetting;

        protected virtual float SpeedFactor => powerWorkSetting.SupplyPowerForSpeed;
        protected virtual int? SkillLevel { get => null; }

        public virtual int MaxPowerForSpeed => powerWorkSetting.MaxPowerForSpeed;

        public IPowerSupplyMachine RangePowerSupplyMachine => this;

        [Unsaved]
        protected bool setInitialMinPower = true;

        protected CompPowerTrader powerComp;

        public virtual float SupplyPowerForSpeed
        {
            get => powerWorkSetting.SupplyPowerForSpeed;
            set
            {
                powerWorkSetting.SupplyPowerForSpeed = value;
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            powerWorkSetting = this.GetComp<CompPowerWorkSetting>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.powerComp = this.TryGetComp<CompPowerTrader>();
            powerWorkSetting = this.GetComp<CompPowerWorkSetting>();

            if (!respawningAfterLoad)
            {
                if (setInitialMinPower)
                    this.SupplyPowerForSpeed = 0;
            }

            this.MapManager.NextAction(this.RefreshPowerStatus);
            this.MapManager.AfterAction(5, this.RefreshPowerStatus);
        }

        protected override bool IsActive()
        {
            if (this.powerComp == null || !this.powerComp.PowerOn)
            {
                return false;
            }
            return base.IsActive();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn();
        }

        public virtual void RefreshPowerStatus()
        {
            powerWorkSetting.RefreshPowerStatus();
        }

        protected override float WorkAmountPerTick => 0.01f * this.SpeedFactor * this.SupplyPowerForSpeed * this.Factor2();

        public virtual int MaxPowerForRange => powerWorkSetting.MaxPowerForRange;

        public virtual float SupplyPowerForRange { get => powerWorkSetting.SupplyPowerForRange; set => powerWorkSetting.SupplyPowerForRange = value; }

        public virtual bool Glowable => false;

        public virtual bool Glow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual bool SpeedSetting => true;

        public virtual bool RangeSetting => false;

        public virtual float RangeInterval => powerWorkSetting.RangeInterval;

        public int BasePowerConsumption => powerWorkSetting.BasePowerConsumption;

        public int CurrentPowerConsumption => powerWorkSetting.CurrentPowerConsumption;

        public Dictionary<string, int> AdditionalPowerConsumption => null;

        protected virtual float Factor2()
        {
            return 0.1f;
        }

        protected override void ReceiveCompSignal(string signal)
        {
            if (signal == CompPowerTrader.PowerTurnedOffSignal || signal == CompPowerTrader.PowerTurnedOnSignal)
            {
                this.RefreshPowerStatus();
            }
        }
    }
}
