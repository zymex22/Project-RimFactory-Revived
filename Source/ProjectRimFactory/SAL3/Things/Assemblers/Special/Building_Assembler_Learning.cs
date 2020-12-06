using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectRimFactory.Common.HarmonyPatches;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers.Special
{
    public class Building_Assembler_Learning : Building_SmartAssembler, ISetQualityDirectly
    {
        private WorkSpeedFactorManager manager = new WorkSpeedFactorManager();

        public float FactorOffset => modExtension_LearningAssembler?.MinSpeed ?? 0.5f;

        protected override float ProductionSpeedFactor => currentBillReport == null
            ? FactorOffset
            : manager.GetFactorFor(currentBillReport.bill.recipe) + FactorOffset;

        private ModExtension_LearningAssembler modExtension_LearningAssembler =>
            def.GetModExtension<ModExtension_LearningAssembler>();

        //Calculate the Item Quality based on the ProductionSpeedFactor (Used by the Harmony Patch; see Patch_GenRecipe_MakeRecipeProducts.cs)
        QualityCategory ISetQualityDirectly.GetQuality(SkillDef relevantSkill)
        {
            var centerX = ProductionSpeedFactor * 2f;
            var num = Rand.Gaussian(centerX, 1.25f);
            num = Mathf.Clamp(num, 0f, QualityUtility.AllQualityCategories.Count - 0.5f);

            //TODO maybe modify the gaussian to be within the range
            if (modExtension_LearningAssembler != null)
                num = Mathf.Clamp(num, (int) modExtension_LearningAssembler.MinQuality,
                    (int) modExtension_LearningAssembler.MaxQuality);

            return (QualityCategory) (int) num;
        }

        public override void Tick()
        {
            base.Tick();
            if (currentBillReport != null && this.IsHashIntervalTick(60) && Active)
            {
                if (modExtension_LearningAssembler != null && modExtension_LearningAssembler?.MaxSpeed <=
                    manager.GetFactorFor(currentBillReport.bill.recipe)) return;


                manager.IncreaseWeight(currentBillReport.bill.recipe,
                    0.001f * currentBillReport.bill.recipe.workSkillLearnFactor);
            }
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            if (currentBillReport != null)
                stringBuilder.AppendLine("SALCurrentProductionSpeed".Translate(currentBillReport.bill.recipe.label,
                    ProductionSpeedFactor.ToStringPercent()));
            return stringBuilder.ToString().TrimEndNewlines();
        }

        protected override void PostProcessRecipeProduct(Thing thing)
        {
        }

        protected override IEnumerable<FloatMenuOption> GetDebugOptions()
        {
            foreach (var option in base.GetDebugOptions()) yield return option;
            yield return new FloatMenuOption("View active skills", () =>
            {
                manager.TrimUnnecessaryFactors();
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Active skills ranked descending:");
                var keys = manager.factors.Keys.ToArray();
                var values = manager.factors.Values.ToArray();
                Array.Sort(values, keys);
                for (var i = keys.Length - 1; i >= 0; i--)
                    stringBuilder.AppendLine($"{keys[i].LabelCap}: {values[i].FactorFinal + FactorOffset}");
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