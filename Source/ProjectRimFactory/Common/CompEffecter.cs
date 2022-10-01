using RimWorld;
using Verse;
using Verse.Sound;

namespace ProjectRimFactory.Common
{
    // This holds an effecter for the possessing ThingWithComps
    //  (Vanilla effecters include pawn actions (e.g., vomiting
    //   or playing poker), damage effects (bullet glanced off,
    //   etc), or visual effects like firefoam being popped and
    //   the ubiquitous progress bars)
    // This effector will Tick() every tick, independently of
    //   the Thing's Tick status.  So the Thing can TickLong,
    //   and the effecter will still Tick() every tick.
    public class CompEffecter : ThingComp, ITicker
    {
        public CompProperties_Effecter Props => (CompProperties_Effecter)this.props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            this.InitializeEffecter();

            this.parent.Map.GetComponent<PRFMapComponent>()?.AddTicker(this);
            this.UpdateEffecter();
        }

        private Effecter effecter;

        private Sustainer sound;

        private bool effectOnInt;

        public void Tick()
        {
            if (this.effectOnInt)
            {
                this.effecter?.EffectTick(this.parent, this.parent);
                this.sound?.SustainerUpdate();
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            this.FinalizeEffecter();
            map.GetComponent<PRFMapComponent>()?.RemoveTicker(this);
        }

        private bool enable = true;

        private void InitializeEffecter()
        {
            this.effecter = this.Props?.effect?.Spawn();
            this.sound = this.Props?.sound?.TrySpawnSustainer(this.parent);
        }

        private void FinalizeEffecter()
        {
            this.effecter?.Cleanup();
            this.sound?.End();
            this.effecter = null;
            this.sound = null;
        }

        public bool Enable
        {
            get
            {
                return this.enable;
            }
            set
            {
                if (this.enable != value)
                {
                    this.enable = value;
                    this.UpdateEffecter();
                }
            }
        }

        protected virtual bool ShouldBeEffectNow
        {
            get
            {
                if (!this.parent.Spawned)
                {
                    return false;
                }
                if (!FlickUtility.WantsToBeOn(this.parent))
                {
                    return false;
                }
                CompPowerTrader compPowerTrader = this.parent.TryGetComp<CompPowerTrader>();
                if (compPowerTrader != null && !compPowerTrader.PowerOn)
                {
                    return false;
                }
                CompRefuelable compRefuelable = this.parent.TryGetComp<CompRefuelable>();
                if (compRefuelable != null && !compRefuelable.HasFuel)
                {
                    return false;
                }
                CompSendSignalOnCountdown compSendSignalOnCountdown = this.parent.TryGetComp<CompSendSignalOnCountdown>();
                if (compSendSignalOnCountdown != null && compSendSignalOnCountdown.ticksLeft <= 0)
                {
                    return false;
                }
                CompSendSignalOnPawnProximity compSendSignalOnPawnProximity = this.parent.TryGetComp<CompSendSignalOnPawnProximity>();
                return compSendSignalOnPawnProximity == null || !compSendSignalOnPawnProximity.Sent;
            }
        }

        private void UpdateEffecter()
        {
            var shouldBeEffectNow = this.ShouldBeEffectNow && this.enable;
            if (this.effectOnInt == shouldBeEffectNow)
            {
                return;
            }
            this.effectOnInt = shouldBeEffectNow;
            if (effectOnInt)
            {
                this.InitializeEffecter();
            }
            else
            {
                this.FinalizeEffecter();
            }

        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == CompPowerTrader.PowerTurnedOnSignal ||
                signal == CompPowerTrader.PowerTurnedOffSignal ||
                signal == CompFlickable.FlickedOnSignal ||
                signal == CompFlickable.FlickedOffSignal ||
                signal == CompRefuelable.RefueledSignal ||
                signal == CompRefuelable.RanOutOfFuelSignal ||
                signal == CompSchedule.ScheduledOnSignal ||
                signal == CompSchedule.ScheduledOffSignal)
            //                signal == MechClusterUtility.DefeatedSignal)
            {
                this.UpdateEffecter();
            }
        }
    }

    public class CompProperties_Effecter : CompProperties
    {
        public EffecterDef effect;

        public SoundDef sound;

        public CompProperties_Effecter()
        {
            this.compClass = typeof(CompEffecter);
        }
    }
}
