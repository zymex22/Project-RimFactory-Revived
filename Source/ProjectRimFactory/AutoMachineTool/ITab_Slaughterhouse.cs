using System;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class SlaughterSettings : IExposable
    {
        public ThingDef def;
        public bool doSlaughter;
        public bool hasBonds;
        public int keepFemaleAdultCount;
        public int keepFemaleYoungCount;

        public int keepMaleAdultCount;
        public int keepMaleYoungCount;
        public bool pregnancy;
        public bool trained;

        public SlaughterSettings()
        {
            doSlaughter = false;
            hasBonds = false;
            pregnancy = false;
            trained = false;

            keepMaleAdultCount = 10;
            keepMaleYoungCount = 10;
            keepFemaleAdultCount = 10;
            keepFemaleYoungCount = 10;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref doSlaughter, "doSlaughter");
            Scribe_Values.Look(ref hasBonds, "hasBonds");
            Scribe_Values.Look(ref pregnancy, "pregnancy");
            Scribe_Values.Look(ref trained, "trained");

            Scribe_Values.Look(ref keepMaleAdultCount, "keepMaleAdultCount", 10);
            Scribe_Values.Look(ref keepMaleYoungCount, "keepMaleYoungCount", 10);
            Scribe_Values.Look(ref keepFemaleAdultCount, "keepFemaleAdultCount", 10);
            Scribe_Values.Look(ref keepFemaleYoungCount, "keepFemaleYoungCount", 10);

            Scribe_Defs.Look(ref def, "def");
        }

        public int KeepCount(Gender gender, bool adult)
        {
            if (gender == Gender.Male)
            {
                if (adult)
                    return keepMaleAdultCount;
                return keepMaleYoungCount;
            }

            if (adult)
                return keepFemaleAdultCount;
            return keepFemaleYoungCount;
        }
    }

    internal interface ISlaughterhouse
    {
        Dictionary<ThingDef, SlaughterSettings> Settings { get; }
    }

    public class ITab_Slaughterhouse_Def : IPRF_SettingsContent
    {
        private static readonly float[] ColumnWidth = {0.2f, 0.05f, 0.05f, 0.05f, 0.05f, 0.15f, 0.15f, 0.15f, 0.15f};


        private static readonly TipSignal slaughterTip =
            new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.DoSlaughterTip".Translate());

        private static readonly TipSignal hasBondsTip =
            new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.BondsTip".Translate());

        private static readonly TipSignal pregnancyTip =
            new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.PregnancyTip".Translate());

        private static readonly TipSignal trainedTip =
            new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.TrainedTip".Translate());

        private static readonly TipSignal keepMaleChildCountTip =
            new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate(
                "PRF.AutoMachineTool.Male".Translate(), "PRF.AutoMachineTool.Young".Translate()));

        private static readonly TipSignal keepFemaleChildCountTip = new TipSignal(
            "PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate(
                "PRF.AutoMachineTool.Female".Translate(), "PRF.AutoMachineTool.Young".Translate()));

        private static readonly TipSignal keepMaleAdultCountTip =
            new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate(
                "PRF.AutoMachineTool.Male".Translate(), "PRF.AutoMachineTool.Adult".Translate()));

        private static readonly TipSignal keepFemaleAdultCountTip = new TipSignal(
            "PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate(
                "PRF.AutoMachineTool.Female".Translate(), "PRF.AutoMachineTool.Adult".Translate()));

        private static Vector2 sscrollPosition;
        private readonly object caller;

        private List<ThingDef> defs;
        private readonly string description = "PRF.AutoMachineTool.Slaughterhouse.Setting.Description".Translate();

        private Vector2 scrollPosition;

        public ITab_Slaughterhouse_Def(object callero)
        {
            caller = callero;
        }

        private ISlaughterhouse slaughterhouse => caller as ISlaughterhouse;

        public Dictionary<ThingDef, SlaughterSettings> Settings => slaughterhouse.Settings;


        public float ITab_Settings_Minimum_x => 800f;

        //This has some unexpected impact
        public float ITab_Settings_Additional_y => 400f; //Thats more then needed

        public Listing_Standard ITab_Settings_AppendContent(Listing_Standard list, Rect parrent_rect)
        {
            //Get the Variable from the Static - This is needed as you cant pass a static by ref (& by ref is requere in this case)
            scrollPosition = sscrollPosition;
            defs = Find.CurrentMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                .Where(p => p.RaceProps.Animal && p.RaceProps.IsFlesh && p.SpawnedOrAnyParentSpawned).Select(p => p.def)
                .Distinct().ToList();

            //Add header Discription for this settings Section
            var rect = list.GetRect(40f);
            Widgets.Label(rect, description);

            //Need to fix that as step one
            var outRect =
                new Rect(0f, list.CurHeight, ITab_Settings_Minimum_x, ITab_Settings_Additional_y + list.CurHeight)
                    .ContractedBy(10f);
            //Log.Message("ITab_Settings_Additional_y + (int)list.CurHeight: " + (ITab_Settings_Additional_y + (int)list.CurHeight) + " - parrent_rect.height: " + parrent_rect.height + " - list.CurHeight" + list.CurHeight);

            var headerRect = list.GetRect(24f);
            headerRect.width -= 30f;

            var cutLeftHeader = CutLeftFunc(headerRect);

            var colIndex = 0;
            var col = cutLeftHeader(ColumnWidth[colIndex++]);
            Widgets.Label(col, "PRF.AutoMachineTool.RaceName".Translate());

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.SlaughterIcon);
            TooltipHandler.TipRegion(col, slaughterTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.BondIcon);
            TooltipHandler.TipRegion(col, hasBondsTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.PregnantIcon);
            TooltipHandler.TipRegion(col, pregnancyTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.TrainedIcon);
            TooltipHandler.TipRegion(col, trainedTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.MaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.YoungIcon);
            TooltipHandler.TipRegion(col, keepMaleChildCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.FemaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.YoungIcon);
            TooltipHandler.TipRegion(col, keepFemaleChildCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.MaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.AdultIcon);
            TooltipHandler.TipRegion(col, keepMaleAdultCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.FemaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.AdultIcon);
            TooltipHandler.TipRegion(col, keepFemaleAdultCountTip);

            var scrollOutRect = list.GetRect(outRect.height - list.CurHeight);

            var scrollViewRect = new Rect(scrollOutRect.x, 0, scrollOutRect.width - 30f, defs.Count() * 36f);


            //Thats somhow not working
            //Widgets.BeginScrollView(scrollOutRect, ref this.scrollPosition, scrollViewRect);
            var innerlist = new Listing_Standard();
            innerlist.BeginScrollView(scrollOutRect, ref scrollPosition, ref scrollViewRect);
            innerlist.Begin(scrollViewRect);
            defs.ForEach(d =>
            {
                innerlist.GapLine();

                var rowRect = innerlist.GetRect(24f);

                SlaughterSettings s = null;
                Settings.TryGetValue(d, out s);
                if (s == null)
                {
                    s = new SlaughterSettings();
                    s.def = d;
                    Settings[d] = s;
                }

                var cutLeft = CutLeftFunc(rowRect);

                colIndex = 0;
                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Label(col, s.def.label);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.doSlaughter);
                TooltipHandler.TipRegion(col, slaughterTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.hasBonds, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, hasBondsTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.pregnancy, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, pregnancyTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.trained, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, trainedTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                var buf1 = s.keepMaleYoungCount.ToString();
                Widgets.TextFieldNumeric(col, ref s.keepMaleYoungCount, ref buf1, 0, 1000);
                TooltipHandler.TipRegion(col, keepMaleChildCountTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                var buf2 = s.keepFemaleYoungCount.ToString();
                Widgets.TextFieldNumeric(col, ref s.keepFemaleYoungCount, ref buf2, 0, 1000);
                TooltipHandler.TipRegion(col, keepFemaleChildCountTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                var buf3 = s.keepMaleAdultCount.ToString();
                Widgets.TextFieldNumeric(col, ref s.keepMaleAdultCount, ref buf3, 0, 1000);
                TooltipHandler.TipRegion(col, keepMaleAdultCountTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                var buf4 = s.keepFemaleAdultCount.ToString();
                Widgets.TextFieldNumeric(col, ref s.keepFemaleAdultCount, ref buf4, 0, 1000);
            });
            innerlist.EndScrollView(ref scrollViewRect);
            innerlist.End();
            //Update the static variable to keep the data
            sscrollPosition = scrollPosition;
            return list;
        }

        private Func<float, Rect> CutLeftFunc(Rect rect)
        {
            var curX = 0f;
            return pct =>
            {
                var r = new Rect(curX, rect.y, rect.width * pct, rect.height);
                curX += rect.width * pct;
                return r;
            };
        }
    }
}