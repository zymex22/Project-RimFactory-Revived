using System.Text;
using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Industry
{
    public class Building_AtomicReconstructor : Building
    {
        private CompOutputAdjustable outputComp;
        private CompPowerTrader powerComp;
        public int progressTicks;
        private CompRefuelable refuelableComp;
        public int speedFactor = 1; // fuel consumption = efficiencyFactor^2
        private ThingDef thingToGenerate;

        public int PaperclipConsumptionFactor => speedFactor * speedFactor;

        public int TotalWorkRequired
        {
            get
            {
                if (ThingToGenerate == null)
                    return 0;
                return Mathf.RoundToInt(StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(ThingToGenerate, null)) *
                                        100 * 2); // 2 work per $0.01
            }
        }

        public float ItemBaseCost
        {
            get
            {
                if (ThingToGenerate == null)
                    return 0;
                return ThingToGenerate.PaperclipAmount() * PaperclipConsumptionFactor;
            }
        }

        public float FuelConsumptionPerTick
        {
            get
            {
                if (ThingToGenerate == null)
                    return 0;
                return ItemBaseCost * speedFactor / TotalWorkRequired;
            }
        }

        public string ProgressToStringPercent => ThingToGenerate == null
            ? 0f.ToStringPercent()
            : (progressTicks / (float) TotalWorkRequired).ToStringPercent();

        public string EstimatedProductionTimeLeftPeriod =>
            ((TotalWorkRequired - progressTicks) / speedFactor).ToStringTicksToPeriod();

        public ThingDef ThingToGenerate
        {
            get => thingToGenerate;
            set
            {
                thingToGenerate = value;
                progressTicks = 0;
            }
        }

        public override void PostMake()
        {
            base.PostMake();
            powerComp = GetComp<CompPowerTrader>();
            refuelableComp = GetComp<CompRefuelable>();
            outputComp = GetComp<CompOutputAdjustable>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            refuelableComp = GetComp<CompRefuelable>();
            outputComp = GetComp<CompOutputAdjustable>();
        }

        public override void Tick()
        {
            base.Tick();
            if (powerComp.PowerOn)
                if (ThingToGenerate != null)
                {
                    var fuel = refuelableComp.Fuel;
                    if (fuel >= FuelConsumptionPerTick)
                    {
                        refuelableComp.ConsumeFuel(FuelConsumptionPerTick);
                        progressTicks += speedFactor;
                        if (progressTicks >= TotalWorkRequired)
                        {
                            var thing = ThingMaker.MakeThing(ThingToGenerate);
                            GenPlace.TryPlaceThing(thing, outputComp.CurrentCell, Map, ThingPlaceMode.Near);
                            progressTicks = 0;
                        }
                    }
                }
        }

        public override string GetInspectString()
        {
            var builder = new StringBuilder();
            var str = base.GetInspectString();
            if (!string.IsNullOrEmpty(str)) builder.AppendLine(str);
            builder.Append("AtomicReconstructorProgress".Translate(ProgressToStringPercent));
            return builder.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref thingToGenerate, "thingToGenerate");
            Scribe_Values.Look(ref progressTicks, "progressTicks");
            Scribe_Values.Look(ref speedFactor, "speedFactor", 1);
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (Find.CameraDriver.CurrentZoom < CameraZoomRange.Middle)
                GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(this, 0f),
                    thingToGenerate?.LabelCap ?? "AssemblerIdle".Translate(), Color.white);
        }
    }
}