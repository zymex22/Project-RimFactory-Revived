using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProjectRimFactory.Industry.UI
{
    public class ITab_AtomicReconstruction : ITab
    {
        public ITab_AtomicReconstruction()
        {
            size = new Vector2(400f, 400f);
            labelKey = "PRFAtomicReconstructionTab";
        }

        private Building_AtomicReconstructor SelBuilding => (Building_AtomicReconstructor)SelThing;
        private readonly Comparer<ThingDef> thingDefComparer = Comparer<ThingDef>
            .Create((first, second) => first.LabelCap.RawText.CompareTo(second.LabelCap.RawText));

        private readonly string nowProducing = "AtomicReconstructionTab_NowProducing".Translate().RawText;
        private readonly string paperclipCost = "AtomicReconstructionTab_PaperclipCost".Translate().RawText;
        private readonly string consumptionPerSecond =
            "AtomicReconstructionTab_ConsumptionPerSecond".Translate().RawText;
        private readonly string progress = "AtomicReconstructionTab_Progress".Translate().RawText;
        private readonly string noneBrackets = "NoneBrackets".Translate().RawText;
        
        protected override void FillTab()
        {

            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            var listing = new Listing_Standard(GameFont.Small);
            listing.Begin(rect);
            listing.Label(SelThing.LabelCapNoCount);
            listing.LabelDouble(nowProducing, (SelBuilding.ThingToGenerate?.LabelCap ?? noneBrackets));
            listing.LabelDouble(paperclipCost, SelBuilding.ItemBaseCost.ToStringDecimalIfSmall());
            listing.LabelDouble(consumptionPerSecond, (SelBuilding.FuelConsumptionPerTick * 60f).ToStringDecimalIfSmall());
            listing.LabelDouble(progress, SelBuilding.ProgressToStringPercent + $" ({SelBuilding.EstimatedProductionTimeLeftPeriod})");
            listing.Label("AtomicReconstructionTab_Speed".Translate(SelBuilding.speedFactor, SelBuilding.PaperclipConsumptionFactor));
            SelBuilding.speedFactor = (int)listing.Slider(SelBuilding.speedFactor, 1f, 20f);
            searchQuery = listing.TextEntry(searchQuery);
            var rect2 = new Rect(0, listing.CurHeight, rect.width, rect.height - listing.CurHeight);
            var viewRect = new Rect(0f, 0f, rect2.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(rect2, ref scrollPos, viewRect);
            float curY = 0;
            // Draw reset button
            var resetRect = new Rect(0f, curY, viewRect.width, 28f);
            Widgets.Label(new Rect(36f, resetRect.y, resetRect.width - 36f, resetRect.height), noneBrackets);
            Widgets.DrawHighlightIfMouseover(resetRect);
            if (GUI.Button(resetRect, string.Empty, Widgets.EmptyStyle))
            {
                if (SelBuilding.ThingToGenerate != null)
                {
                    SelBuilding.ThingToGenerate = null;
                }
                SoundDefOf.Click.PlayOneShot(SoundInfo.OnCamera());
            }
            curY += 28f;
            foreach (var tDef in AllAllowedThingDefsColonyCanProduce().OrderBy(d => d, thingDefComparer))
            {
                if (searchQuery == null || tDef.label.ToLower().Contains(searchQuery))
                {
                    try
                    {
                        DrawThingDefRow(ref curY, viewRect.width, tDef);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Project RimFactory :: Exception displaying row for {tDef}:{e}");
                    }
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = curY + 30f;
            }
            Widgets.EndScrollView();
            listing.End();
        }
        private void DrawThingDefRow(ref float y, float width, ThingDef thingDef)
        {
            var rect = new Rect(0f, y, width, 28f);
            if (Mouse.IsOver(rect))
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (thingDef.DrawMatSingle != null && thingDef.DrawMatSingle.mainTexture != null)
            {
                if (thingDef.graphicData != null && GenUI.IconDrawScale(thingDef) <= 1.5f)
                {
                    Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thingDef);
                }
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var rect5 = new Rect(36f, y, rect.width - 36f, rect.height);
            Text.WordWrap = false;
            Widgets.Label(rect5, thingDef.LabelCap.Truncate(rect5.width));
            Text.WordWrap = true;
            var text2 = thingDef.description;
            if (y > -28f)
            {
                TooltipHandler.TipRegion(rect, string.IsNullOrEmpty(text2) ? "PRFNoDesc".Translate().RawText : text2);
            }
            if (GUI.Button(rect, "", Widgets.EmptyStyle))
            {
                if (SelBuilding.ThingToGenerate != thingDef)
                {
                    SelBuilding.ThingToGenerate = thingDef;
                }
                SoundDefOf.Click.PlayOneShot(SoundInfo.OnCamera());
            }
            Text.Anchor = TextAnchor.UpperLeft;
            y += 28f;
        }

        private static IEnumerable<ThingDef> AllAllowedThingDefsColonyCanProduce()
        {
            foreach (var tDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (tDef.MadeFromStuff || tDef.GetModExtension<DefModExtension_AtomicReconstructorDisallow>() != null) continue;
                var ex = tDef.GetModExtension<DefModExtension_AtomicReconstructorResearchPrerequisite>();
                if (ex != null)
                {
                    if (ex.prerequisites?.All(r => r.IsFinished) == false)
                    {
                        continue;
                    }
                    if (ex.ignoreMainPrerequisites)
                    {
                        yield return tDef;
                        continue;
                    }
                }

                if (tDef.thingCategories == null) continue;
                foreach (var cat in from tcd in tDef.thingCategories from child in tcd.ThisAndParents() select child)
                {
                    switch (cat.defName)
                    {
                        case nameof(ThingCategoryDefOf.ResourcesRaw) when PRFDefOf.PRFAtomicReconstruction.IsFinished:
                        case nameof(ThingCategoryDefOf.Foods) when PRFDefOf.PRFEdiblesSynthesis.IsFinished:
                        case nameof(ThingCategoryDefOf.Manufactured) when PRFDefOf.PRFManufacturablesProduction.IsFinished:
                            yield return tDef;
                            break;
                        default:
                            continue;
                    }
                    break;
                }
            }
        }

        private Vector2 scrollPos;
        private float scrollViewHeight;
        private string searchQuery;
    }
}
