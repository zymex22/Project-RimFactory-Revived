using ProjectRimFactory.Common.HarmonyPatches;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers.Special
{
    public class Building_Assembler_Learning : Building_SmartAssembler, ISetQualityDirectly
    {
        private WorkSpeedFactorManager manager;

        private float FactorOffset => ModExtensionLearningAssembler?.MinSpeed ?? 0.5f;

        private float MaxSpeed
        {
            get
            {
                var effectiveMaxSpeed = ModExtensionLearningAssembler?.MaxSpeed ?? float.PositiveInfinity;
                return effectiveMaxSpeed - FactorOffset;
            }
        }

        private float Progress => Mathf.Clamp01(manager.GetFactorFor(CurrentBillReport.Bill.recipe) / MaxSpeed);

        /// <summary>
        /// Maps the 0..1 progress to -.1..1.1 scale
        /// Slightly favors low quality at low progress, and high quality at high progress.
        /// </summary>
        private float NormalizedProgress => (Progress * 1.2f) - 0.1f;
        
        protected override float ProductionSpeedFactor =>
            CurrentBillReport == null ? FactorOffset : manager.GetFactorFor(CurrentBillReport.Bill.recipe) + FactorOffset;

        private ModExtension_LearningAssembler ModExtensionLearningAssembler => def.GetModExtension<ModExtension_LearningAssembler>();

        //Calculate the Item Quality based on the ProductionSpeedFactor (Used by the Harmony Patch; see Patch_GenRecipe_MakeRecipeProducts.cs)
        QualityCategory ISetQualityDirectly.GetQuality(SkillDef relevantSkill)
        {
            if (ModExtensionLearningAssembler == null)
            {
                Log.Error("Got a Building_Assembler_Learning without a modExtension_LearningAssembler, please report this error!");
                return QualityCategory.Normal;
            }
            var maxQualityFactor = (float)ModExtensionLearningAssembler.MaxQuality;
            var centerX = NormalizedProgress * maxQualityFactor;
            var expectedQuality = Rand.Gaussian(centerX, 1.25f);

            expectedQuality = Mathf.Clamp(expectedQuality, (int)ModExtensionLearningAssembler.MinQuality, (int)ModExtensionLearningAssembler.MaxQuality);

            return (QualityCategory)(int)expectedQuality;
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (CurrentBillReport == null || !this.IsHashIntervalTick(60) || !Active) return;
            if (ModExtensionLearningAssembler != null && MaxSpeed <= manager.GetFactorFor(CurrentBillReport.Bill.recipe))
            {
                return;
            }
            
            manager.IncreaseWeight(CurrentBillReport.Bill.recipe, 0.001f * CurrentBillReport.Bill.recipe.workSkillLearnFactor);
        }
        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            if (CurrentBillReport != null)
            {
                stringBuilder.AppendLine("SALCurrentProductionSpeed".Translate(CurrentBillReport.Bill.recipe.label, ProductionSpeedFactor.ToStringPercent()));
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
            yield return new FloatMenuOption("View active skills", () =>
            {
                manager.TrimUnnecessaryFactors();
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Active skills ranked descending:");
                var keys = manager.factors.Keys.ToArray();
                var values = manager.factors.Values.ToArray();
                Array.Sort(values, keys);
                for (var i = keys.Length - 1; i >= 0; i--)
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

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            manager ??= new WorkSpeedFactorManager();
        }
    }
}
