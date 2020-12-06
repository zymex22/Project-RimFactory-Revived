using System;
using ProjectRimFactory.Common;
using RimWorld;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class Building_BaseMachine<T> : Building_Base<T>, IPowerSupplyMachineHolder, IPowerSupplyMachine,
        IBeltConveyorSender where T : Thing
    {
        protected CompPowerTrader powerComp;

        [Unsaved] protected bool setInitialMinPower = true;

        private float supplyPowerForSpeed;
        protected virtual float SpeedFactor => WorkSpeedExtension.speedFactor;
        protected virtual int? SkillLevel => null;

        protected ModExtension_WorkSpeed WorkSpeedExtension => def.GetModExtension<ModExtension_WorkSpeed>();

        protected override float WorkAmountPerTick => 0.01f * SpeedFactor * SupplyPowerForSpeed * Factor2();

        public virtual int MinPowerForSpeed => WorkSpeedExtension.minPower;
        public virtual int MaxPowerForSpeed => WorkSpeedExtension.maxPower;

        public virtual float SupplyPowerForSpeed
        {
            get => supplyPowerForSpeed;
            set
            {
                if (supplyPowerForSpeed != value)
                {
                    supplyPowerForSpeed = value;
                    RefreshPowerStatus();
                }
            }
        }

        public virtual void RefreshPowerStatus()
        {
            if (powerComp == null) return;
            if (SupplyPowerForSpeed != powerComp.PowerOutput) powerComp.PowerOutput = -SupplyPowerForSpeed;
        }

        public virtual int MinPowerForRange => throw new NotImplementedException();

        public virtual int MaxPowerForRange => throw new NotImplementedException();

        public virtual float SupplyPowerForRange
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public virtual bool Glowable => false;

        public virtual bool Glow
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public virtual bool SpeedSetting => true;

        public virtual bool RangeSetting => false;

        public virtual float RangeInterval => throw new NotImplementedException();

        public IPowerSupplyMachine RangePowerSupplyMachine => this;


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref supplyPowerForSpeed, "supplyPowerForSpeed", MinPowerForSpeed);
            ReloadSettings(null, null);
        }

        protected virtual void ReloadSettings(object sender, EventArgs e)
        {
            if (SupplyPowerForSpeed < MinPowerForSpeed) SupplyPowerForSpeed = MinPowerForSpeed;
            if (SupplyPowerForSpeed > MaxPowerForSpeed) SupplyPowerForSpeed = MaxPowerForSpeed;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = this.TryGetComp<CompPowerTrader>();

            if (!respawningAfterLoad)
                if (setInitialMinPower)
                    SupplyPowerForSpeed = MinPowerForSpeed;

            MapManager.NextAction(RefreshPowerStatus);
            MapManager.AfterAction(5, RefreshPowerStatus);
        }

        protected override bool IsActive()
        {
            if (powerComp == null || !powerComp.PowerOn) return false;
            return base.IsActive();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn();
        }

        protected virtual float Factor2()
        {
            return 0.1f;
        }

        protected override void ReceiveCompSignal(string signal)
        {
            if (signal == CompPowerTrader.PowerTurnedOffSignal || signal == CompPowerTrader.PowerTurnedOnSignal)
                RefreshPowerStatus();
        }
    }
}