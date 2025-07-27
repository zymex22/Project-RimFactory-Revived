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
        private CompProperties_Effecter Props => (CompProperties_Effecter)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            InitializeEffecter();

            parent.Map.GetComponent<PRFMapComponent>()?.AddTicker(this);
            UpdateEffecter();
        }

        private Effecter effecter;

        private Sustainer sound;

        private bool effectOnInt;

        public void Tick()
        {
            if (effectOnInt)
            {
                effecter?.EffectTick(parent, parent);
                sound?.SustainerUpdate();
            }
        }
        
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);

            FinalizeEffecter();
            map.GetComponent<PRFMapComponent>()?.RemoveTicker(this);
        }

        private bool enable = true;

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

        protected virtual bool ShouldBeEffectNow
        {
            get
            {
                if (!parent.Spawned)
                {
                    return false;
                }
                if (!FlickUtility.WantsToBeOn(parent))
                {
                    return false;
                }
                if (parent.TryGetComp<CompPowerTrader>() is { PowerOn: false })
                {
                    return false;
                }
                if (parent.TryGetComp<CompRefuelable>() is { HasFuel: false })
                {
                    return false;
                }
                if (parent.TryGetComp<CompSendSignalOnCountdown>() is { ticksLeft: <= 0 })
                {
                    return false;
                }
                //Todo: Check if we need a replacement (I'm a bit lost here)
                //CompSendSignalOnPawnProximity compSendSignalOnPawnProximity = this.parent.TryGetComp<CompSendSignalOnPawnProximity>();
                //return compSendSignalOnPawnProximity == null || !compSendSignalOnPawnProximity.Sent;
                return true;
            }
        }

        private void UpdateEffecter()
        {
            var shouldBeEffectNow = ShouldBeEffectNow && enable;
            if (effectOnInt == shouldBeEffectNow)
            {
                return;
            }
            effectOnInt = shouldBeEffectNow;
            if (effectOnInt)
            {
                InitializeEffecter();
            }
            else
            {
                FinalizeEffecter();
            }

        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal is CompPowerTrader.PowerTurnedOnSignal or CompPowerTrader.PowerTurnedOffSignal or 
                CompFlickable.FlickedOnSignal or CompFlickable.FlickedOffSignal or CompRefuelable.RefueledSignal or 
                CompRefuelable.RanOutOfFuelSignal or CompSchedule.ScheduledOnSignal or CompSchedule.ScheduledOffSignal)
            {
                UpdateEffecter();
            }
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
