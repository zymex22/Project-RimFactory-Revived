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
        private Effecter effecter;

        private bool effectOnInt;

        private bool enable = true;

        private Sustainer sound;
        public CompProperties_Effecter Props => (CompProperties_Effecter) props;

        public bool Enable
        {
            get => enable;
            set
            {
                if (enable != value)
                {
                    enable = value;
                    UpdateEffecter();
                }
            }
        }

        protected virtual bool ShouldBeEffectNow
        {
            get
            {
                if (!parent.Spawned) return false;
                if (!FlickUtility.WantsToBeOn(parent)) return false;
                var compPowerTrader = parent.TryGetComp<CompPowerTrader>();
                if (compPowerTrader != null && !compPowerTrader.PowerOn) return false;
                var compRefuelable = parent.TryGetComp<CompRefuelable>();
                if (compRefuelable != null && !compRefuelable.HasFuel) return false;
                var compSendSignalOnCountdown = parent.TryGetComp<CompSendSignalOnCountdown>();
                if (compSendSignalOnCountdown != null && compSendSignalOnCountdown.ticksLeft <= 0) return false;
                var compSendSignalOnPawnProximity = parent.TryGetComp<CompSendSignalOnPawnProximity>();
                return compSendSignalOnPawnProximity == null || !compSendSignalOnPawnProximity.Sent;
            }
        }

        public void Tick()
        {
            if (effectOnInt)
            {
                effecter?.EffectTick(parent, parent);
                sound?.SustainerUpdate();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            InitializeEffecter();

            parent.Map.GetComponent<PRFMapComponent>()?.AddTicker(this);
            UpdateEffecter();
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            FinalizeEffecter();
            map.GetComponent<PRFMapComponent>()?.RemoveTicker(this);
        }

        private void InitializeEffecter()
        {
            effecter = Props?.effect?.Spawn();
            sound = Props?.sound?.TrySpawnSustainer(parent);
        }

        private void FinalizeEffecter()
        {
            effecter?.Cleanup();
            sound?.End();
            effecter = null;
            sound = null;
        }

        private void UpdateEffecter()
        {
            var shouldBeEffectNow = ShouldBeEffectNow && enable;
            if (effectOnInt == shouldBeEffectNow) return;
            effectOnInt = shouldBeEffectNow;
            if (effectOnInt)
                InitializeEffecter();
            else
                FinalizeEffecter();
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
                UpdateEffecter();
        }
    }

    public class CompProperties_Effecter : CompProperties
    {
        public EffecterDef effect;

        public SoundDef sound;

        public CompProperties_Effecter()
        {
            compClass = typeof(CompEffecter);
        }
    }
}