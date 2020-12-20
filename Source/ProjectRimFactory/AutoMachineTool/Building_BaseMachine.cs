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
    public abstract class Building_BaseMachine<T> : Building_Base<T>, IPowerSupplyMachineHolder, IBeltConveyorSender where T : Thing
    {

        public CompPowerWorkSetting powerWorkSetting;

        public IPowerSupplyMachine RangePowerSupplyMachine => powerWorkSetting;

        protected virtual float SpeedFactor => powerWorkSetting.SupplyPowerForSpeed;
        protected virtual int? SkillLevel { get => null; }

        [Unsaved]
        protected bool setInitialMinPower = true;

        protected CompPowerTrader powerComp;


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
                    powerWorkSetting.SupplyPowerForSpeed = 0;
            }

            this.MapManager.NextAction(powerWorkSetting.RefreshPowerStatus);
            this.MapManager.AfterAction(5, powerWorkSetting.RefreshPowerStatus);
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

        protected override float WorkAmountPerTick => 0.01f * this.SpeedFactor * powerWorkSetting.SupplyPowerForSpeed * this.Factor2();


        public virtual bool Glowable => false;

        public virtual bool Glow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected virtual float Factor2()
        {
            return 0.1f;
        }

        protected override void ReceiveCompSignal(string signal)
        {
            if (signal == CompPowerTrader.PowerTurnedOffSignal || signal == CompPowerTrader.PowerTurnedOnSignal)
            {
                powerWorkSetting.RefreshPowerStatus();
            }
        }
    }
}
