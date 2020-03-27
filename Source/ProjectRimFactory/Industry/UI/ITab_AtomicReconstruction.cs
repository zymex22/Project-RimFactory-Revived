using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using ProjectRimFactory.Storage.UI;
using Verse.Sound;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.Industry.UI
{
    public class ITab_AtomicReconstruction : ITab
    {
        public ITab_AtomicReconstruction()
        {
            size = new Vector2(400f, 400f);
            labelKey = "PRFAtomicReconstructionTab";
        }
        public Building_AtomicReconstructor SelBuilding => (Building_AtomicReconstructor)SelThing;
        public Comparer<ThingDef> ThingDefComparer = Comparer<ThingDef>.Create((first, second) => first.LabelCap.RawText.CompareTo(second.LabelCap.RawText));
        protected override void FillTab()
        {
           
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Listing_Standard listing = new Listing_Standard(GameFont.Small);
            listing.Begin(rect);
            listing.Label(SelThing.LabelCapNoCount);
            listing.LabelDouble("AtomicReconstructionTab_NowProducing".Translate().RawText, (SelBuilding.ThingToGenerate?.LabelCap ?? "NoneBrackets".Translate().RawText));
            listing.LabelDouble("AtomicReconstructionTab_PaperclipCost".Translate().RawText, SelBuilding.ItemBaseCost.ToStringDecimalIfSmall());
            listing.LabelDouble("AtomicReconstructionTab_ConsumptionPerSecond".Translate().RawText, (SelBuilding.FuelConsumptionPerTick * 60f).ToStringDecimalIfSmall());
            listing.LabelDouble("AtomicReconstructionTab_Progress".Translate().RawText, SelBuilding.ProgressToStringPercent + $" ({SelBuilding.EstimatedProductionTimeLeftPeriod})");
            listing.Label("AtomicReconstructionTab_Speed".Translate(SelBuilding.speedFactor, SelBuilding.PaperclipConsumptionFactor));
            SelBuilding.speedFactor = (int)listing.Slider(SelBuilding.speedFactor, 1f, 20f);
            searchQuery = listing.TextEntry(searchQuery);
            Rect rect2 = new Rect(0, listing.CurHeight, rect.width, rect.height - listing.CurHeight);
            Rect viewRect = new Rect(0f, 0f, rect2.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(rect2, ref scrollPos, viewRect);
            float curY = 0;
            // Draw reset button
            Rect resetRect = new Rect(0f, curY, viewRect.width, 28f);
            Widgets.Label(new Rect(36f, resetRect.y, resetRect.width - 36f, resetRect.height), "NoneBrackets".Translate().RawText);
            Widgets.DrawHighlightIfMouseover(resetRect);
            if (GUI.Button(resetRect, "", Widgets.EmptyStyle))
            {
                if (SelBuilding.ThingToGenerate != null)
                {
                    SelBuilding.ThingToGenerate = null;
                }
                SoundDefOf.Click.PlayOneShot(SoundInfo.OnCamera());
            }
            curY += 28f;
            foreach (ThingDef tDef in AllAllowedThingDefsColonyCanProduce().OrderBy(d=>d, ThingDefComparer))
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
            Rect rect = new Rect(0f, y, width, 28f);
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
            Rect rect5 = new Rect(36f, y, rect.width - 36f, rect.height);
            Text.WordWrap = false;
            Widgets.Label(rect5, thingDef.LabelCap.Truncate(rect5.width, null));
            Text.WordWrap = true;
            string text2 = thingDef.description;
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

        public static IEnumerable<ThingDef> AllAllowedThingDefsColonyCanProduce()
        {
            foreach (ThingDef tDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (!tDef.MadeFromStuff && tDef.GetModExtension<DefModExtension_AtomicReconstructorDisallow>() == null)
                {
                    DefModExtension_AtomicReconstructorResearchPrerequisite ex = tDef.GetModExtension<DefModExtension_AtomicReconstructorResearchPrerequisite>();
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
                    if (tDef.thingCategories != null)
                    {
                        foreach (ThingCategoryDef cat in from tcd in tDef.thingCategories from child in tcd.ThisAndParents() select child)
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
            }
        }
        Vector2 scrollPos;
        float scrollViewHeight;
        string searchQuery;
    }
}
