using ProjectRimFactory.Common;
using RimWorld;
using System;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class Building_BaseMachine<T> : Building_Base<T>, IPowerSupplyMachineHolder, IBeltConveyorSender where T : Thing
    {

        public CompPowerWorkSetting PowerWorkSetting;

        public IPowerSupplyMachine RangePowerSupplyMachine => PowerWorkSetting;

        protected virtual int? SkillLevel => null;

        private CompPowerTrader powerComp;


        public override void ExposeData()
        {
            base.ExposeData();
            PowerWorkSetting = GetComp<CompPowerWorkSetting>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = this.TryGetComp<CompPowerTrader>();
            PowerWorkSetting = GetComp<CompPowerWorkSetting>();

            MapManager.NextAction(PowerWorkSetting.RefreshPowerStatus);
            MapManager.AfterAction(5, PowerWorkSetting.RefreshPowerStatus);
        }

        protected override bool IsActive()
        {
            return powerComp is { PowerOn: true } && base.IsActive();
        }

        protected override float WorkAmountPerTick => 10 * PowerWorkSetting.GetSpeedFactor();

        public virtual bool Glowable => false;

        public virtual bool Glow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected override void ReceiveCompSignal(string signal)
        {
            if (signal is CompPowerTrader.PowerTurnedOffSignal or CompPowerTrader.PowerTurnedOnSignal)
            {
                PowerWorkSetting.RefreshPowerStatus();
            }
        }
    }
}
