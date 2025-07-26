using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Archo
{
    public class CompUnstable : ThingComp
    {
        public int ticksLeft;
        private CompProperties_Unstable Props => (CompProperties_Unstable)props;

        public override void CompTick()
        {
            base.CompTick();
            ticksLeft--;
            if (ticksLeft <= 0)
            {
                Messages.Message("PRF_DisintegrationMessage".Translate(parent.LabelCap), new GlobalTargetInfo(parent.Position, parent.Map), MessageTypeDefOf.NegativeEvent);
                parent.Destroy();
            }
        }
        public override string CompInspectStringExtra()
        {
            return "PRF_TimeLeftToDisintegrate".Translate(ticksLeft.ToStringTicksToPeriod());
        }
        public override void Initialize(CompProperties compProperties)
        {
            base.Initialize(compProperties);
            ticksLeft = Props.ticksToDisintegrate;
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
        }
        public override string TransformLabel(string label)
        {
            var ratio = (float)ticksLeft / Props.ticksToDisintegrate;
            var green = Mathf.RoundToInt(ratio * 255);
            var red = Mathf.RoundToInt((1 - ratio) * 255);
            return $"<color=#{red:x2}{green:x2}00>{label}</color>";
        }
    }
}