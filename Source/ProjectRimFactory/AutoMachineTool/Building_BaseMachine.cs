using ProjectRimFactory.Common;
using RimWorld;
using System;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class Building_BaseMachine<T> : Building_Base<T>, IPowerSupplyMachineHolder, IBeltConveyorSender where T : Thing
    {

        public CompPowerWorkSetting powerWorkSetting;

        public IPowerSupplyMachine RangePowerSupplyMachine => powerWorkSetting;

        protected virtual int? SkillLevel => null;

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

        protected override float WorkAmountPerTick => 10 * this.powerWorkSetting.GetSpeedFactor();

        public virtual bool Glowable => false;

        public virtual bool Glow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected override void ReceiveCompSignal(string signal)
        {
            if (signal == CompPowerTrader.PowerTurnedOffSignal || signal == CompPowerTrader.PowerTurnedOnSignal)
            {
                powerWorkSetting.RefreshPowerStatus();
            }
        }
    }
}
