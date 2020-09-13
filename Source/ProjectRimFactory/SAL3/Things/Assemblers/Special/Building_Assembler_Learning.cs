using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using HarmonyLib;
using ProjectRimFactory.Common;
using ProjectRimFactory.Common.HarmonyPatches;

namespace ProjectRimFactory.SAL3.Things.Assemblers.Special
{
    public class Building_Assembler_Learning : Building_SmartAssembler , ILearningAssemblerProgress
    {
        public float FactorOffset
        {
            get
            {
                return 0.5f;
            }
        }
        WorkSpeedFactorManager manager = new WorkSpeedFactorManager();
        protected override float ProductionSpeedFactor
        {
            get
            {
                return currentBillReport == null ? FactorOffset : manager.GetFactorFor(currentBillReport.bill.recipe) + FactorOffset;
            }
        }

        //Used to Provide the ProductionSpeedFactor to the Harmony Patch for our increasing quality code
        float ILearningAssemblerProgress.ProductionSpeedFactor => ProductionSpeedFactor;

        public override void Tick()
        {
            base.Tick();
            if (currentBillReport != null && this.IsHashIntervalTick(60) && this.Active)
            {
                manager.IncreaseWeight(currentBillReport.bill.recipe, 0.001f * currentBillReport.bill.recipe.workSkillLearnFactor);
            }
        }
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            if (currentBillReport != null)
            {
                stringBuilder.AppendLine("SALCurrentProductionSpeed".Translate(currentBillReport.bill.recipe.label,ProductionSpeedFactor.ToStringPercent()));
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        protected override void PostProcessRecipeProduct(Thing thing)
        {
        }

        protected override IEnumerable<FloatMenuOption> GetDebugOptions()
        {
            foreach (FloatMenuOption option in base.GetDebugOptions())
            {
                yield return option;
            }
            yield return new FloatMenuOption("View active skills", () => {
                manager.TrimUnnecessaryFactors();
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Active skills ranked descending:");
                RecipeDef[] keys = manager.factors.Keys.ToArray();
                WorkSpeedFactorEntry[] values = manager.factors.Values.ToArray();
                Array.Sort(values, keys);
                for (int i = keys.Length - 1; i >= 0; i--)
                {
                    stringBuilder.AppendLine($"{keys[i].LabelCap}: {values[i].FactorFinal + FactorOffset}");
                }
                Log.Message(stringBuilder.ToString());
            });
        }
        public override void ExposeData()
        {
            Scribe_Deep.Look(ref manager, "manager");
            base.ExposeData();
        }
    }
}
