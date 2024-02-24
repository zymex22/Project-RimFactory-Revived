using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3;
using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Industry
{
    [StaticConstructorOnStartup]
    public class CompPaperclipPowerPlant : CompPowerPlant
    {
        public static readonly Texture2D SetTargetFuelLevelCommand = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel", true);

        public int fuelPerSecond = 1;
        public int currentPowerModifierPct = 100;
        int maxPowerModifierPct = 100;

        protected float PowerProductionModifier
        {
            get
            {
                return (currentPowerModifierPct * fuelPerSecond) / 10; // 100W per paperclip per second
            }
        }

        protected override float DesiredPowerOutput
        {
            get
            {
                return -(float)ReflectionUtility.CompProperties_Power_basePowerConsumption.GetValue(Props) * PowerProductionModifier;
            }
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == "RanOutOfFuel" || signal == "FlickedOff")
            {
                currentPowerModifierPct = 100;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(60) && parent.GetComp<CompRefuelable>().HasFuel && parent.GetComp<CompFlickable>().SwitchIsOn)
            {
                if (PRFDefOf.PaperclipGeneratorQuantumFoamManipulation.IsFinished)
                {
                    maxPowerModifierPct = 2500;
                }
                else if (PRFDefOf.PaperclipGeneratorKugelblitz.IsFinished)
                {
                    maxPowerModifierPct = 1000;
                }
                else if (PRFDefOf.PaperclipGeneratorSelfImprovement.IsFinished)
                {
                    maxPowerModifierPct = 500;
                }
                else
                {
                    maxPowerModifierPct = 100;
                }

                if (maxPowerModifierPct < currentPowerModifierPct)
                {
                    currentPowerModifierPct = maxPowerModifierPct;
                }
                else if (currentPowerModifierPct < maxPowerModifierPct)
                {
                    currentPowerModifierPct++;
                }

                parent.GetComp<CompRefuelable>().ConsumeFuel(fuelPerSecond);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
            yield return new Command_Action()
            {
                defaultLabel = "SetPaperclipConsumptionPerSecond".Translate(),
                defaultDesc = "SetPaperclipConsumptionPerSecond_Desc".Translate(),
                icon = SetTargetFuelLevelCommand,
                action = () => Find.WindowStack.Add(new Dialog_Slider(j => "PaperclipFuelConsumption".Translate(j), 1, 100, i => fuelPerSecond = i, fuelPerSecond))
            };
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(base.CompInspectStringExtra());
            builder.AppendLine("PaperclipGeneratorEfficiency".Translate(currentPowerModifierPct, maxPowerModifierPct));
            int runsOutTicks = (int)(parent.GetComp<CompRefuelable>().Fuel / fuelPerSecond * 60f);
            builder.Append("PaperclipsRunOutIn".Translate(runsOutTicks.ToStringTicksToPeriod()));
            return builder.ToString();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref fuelPerSecond, "fuelPerSecond", 1);
            Scribe_Values.Look(ref currentPowerModifierPct, "currentPowerModifier", 100);
        }
    }
}
