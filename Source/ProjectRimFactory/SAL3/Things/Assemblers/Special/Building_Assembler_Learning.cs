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
        WorkSpeedFactorManager manager;
        
        public float FactorOffset => modExtension_LearningAssembler?.MinSpeed ?? 0.5f;

        public float MaxSpeed
        {
            get
            {
                var effectiveMaxSpeed = modExtension_LearningAssembler?.MaxSpeed ?? float.PositiveInfinity;

                return effectiveMaxSpeed - FactorOffset;
            }
        }

        public float Progress => Mathf.Clamp01(manager.GetFactorFor(CurrentBillReport.bill.recipe) / MaxSpeed);

        /// <summary>
        /// Maps the 0..1 progress to -.1..1.1 scale
        /// Slightly favors low quality at low progress, and high quality at high progress.
        /// </summary>
        public float NormalizedProgress => (Progress * 1.2f) - 0.1f;
        
        protected override float ProductionSpeedFactor =>
            CurrentBillReport == null ? FactorOffset : manager.GetFactorFor(CurrentBillReport.bill.recipe) + FactorOffset;

        private ModExtension_LearningAssembler modExtension_LearningAssembler => this.def.GetModExtension<ModExtension_LearningAssembler>();

        //Calculate the Item Quality based on the ProductionSpeedFactor (Used by the Harmony Patch; see Patch_GenRecipe_MakeRecipeProducts.cs)
        QualityCategory ISetQualityDirectly.GetQuality(SkillDef relevantSkill)
        {
            if (modExtension_LearningAssembler == null)
            {
                Log.Error("Got a Building_Assembler_Learning without a modExtension_LearningAssembler, please report this error!");
                return QualityCategory.Normal;
            }
            float maxQualityFactor = (float)modExtension_LearningAssembler.MaxQuality;
            float centerX = NormalizedProgress * maxQualityFactor;
            float expectedQuality = Rand.Gaussian(centerX, 1.25f);

            expectedQuality = Mathf.Clamp(expectedQuality, (int)modExtension_LearningAssembler.MinQuality, (int)modExtension_LearningAssembler.MaxQuality);

            return (QualityCategory)((int)expectedQuality);
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (CurrentBillReport != null && this.IsHashIntervalTick(60) && this.Active)
            {
                if (modExtension_LearningAssembler != null && MaxSpeed <= manager.GetFactorFor(CurrentBillReport.bill.recipe))
                {
                    return;
                }


                manager.IncreaseWeight(CurrentBillReport.bill.recipe, 0.001f * CurrentBillReport.bill.recipe.workSkillLearnFactor);
            }
        }
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            if (CurrentBillReport != null)
            {
                stringBuilder.AppendLine("SALCurrentProductionSpeed".Translate(CurrentBillReport.bill.recipe.label, ProductionSpeedFactor.ToStringPercent()));
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

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            manager ??= new WorkSpeedFactorManager();
        }
    }
}
