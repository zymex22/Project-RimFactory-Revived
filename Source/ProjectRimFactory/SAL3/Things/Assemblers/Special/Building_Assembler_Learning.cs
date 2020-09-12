using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using HarmonyLib;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.SAL3.Things.Assemblers.Special
{
    public class Building_Assembler_Learning : Building_SmartAssembler
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
        public QualityCategory GetRandomProductionQuality()
        {
            float centerX = ProductionSpeedFactor * 2f;
            float num = Rand.Gaussian(centerX, 1.25f);
            num = Mathf.Clamp(num, 0f, QualityUtility.AllQualityCategories.Count - 0.5f);
            return (QualityCategory)((int)num);
        }
        protected override void PostProcessRecipeProduct(Thing thing)
        {
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(GetRandomProductionQuality(), ArtGenerationContext.Colony);
            }
        }

        //this patch prevents the execution of SendCraftNotification and all other calls the affect the quality. This is done caue the quality will be set with  ProjectRimFactory.SAL3.Things.Assemblers.Special.PostProcessRecipeProduct
        [HarmonyPatch(typeof(Verse.GenRecipe), "PostProcessProduct")]
        class Patch_GenRecipe_PostProcessProduct
        {
            static bool Prefix(ref Thing __result, Thing product, RecipeDef recipeDef, Pawn worker)
            {
                if (worker.kindDef == PRFDefOf.PRFSlavePawn)
                {
                    CompQuality compQuality = product.TryGetComp<CompQuality>();
                    if (compQuality != null)
                    {
                        if (recipeDef.workSkill == null)
                        {
                            Log.Error(string.Concat(recipeDef, " needs workSkill because it creates a product with a quality."));
                        }
                        QualityCategory q = QualityUtility.GenerateQualityCreatedByPawn(worker, recipeDef.workSkill);
                        compQuality.SetQuality(q, ArtGenerationContext.Colony); 
                        //QualityUtility.SendCraftNotification(product, worker);
                    }
                    CompArt compArt = product.TryGetComp<CompArt>();
                    if (compArt != null)
                    {
                        compArt.JustCreatedBy(worker);
                        //if (compQuality != null && (int)compQuality.Quality >= 4)
                        //{
                        //    TaleRecorder.RecordTale(TaleDefOf.CraftedArt, worker, product);
                        //}
                    }
                    if (product.def.Minifiable)
                    {
                        product = product.MakeMinified();
                    }
                    __result = product;
                    return false; // do not run vanilla
                }
                return true; // run vanilla
            }
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
