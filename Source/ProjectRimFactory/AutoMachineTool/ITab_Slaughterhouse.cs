using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class SlaughterSettings : IExposable
    {
        public SlaughterSettings()
        {
            doSlaughter = false;
            hasBonds = false;
            pregnancy = false;
            trained = false;

            keepNoneAdultCount = 10;
            keepNoneYoungCount = 10;

            keepMaleAdultCount = 10;
            keepMaleYoungCount = 10;
            keepFemaleAdultCount = 10;
            keepFemaleYoungCount = 10;
        }

        public ThingDef def;
        public bool doSlaughter;
        public bool hasBonds;
        public bool pregnancy;
        public bool trained;


        public int keepNoneYoungCount;
        public int keepNoneAdultCount;

        public int keepMaleAdultCount;
        public int keepMaleYoungCount;
        public int keepFemaleAdultCount;
        public int keepFemaleYoungCount;

        public void ExposeData()
        {
            Scribe_Values.Look<bool>(ref doSlaughter, "doSlaughter", false);
            Scribe_Values.Look<bool>(ref hasBonds, "hasBonds", false);
            Scribe_Values.Look<bool>(ref pregnancy, "pregnancy", false);
            Scribe_Values.Look<bool>(ref trained, "trained", false);

            Scribe_Values.Look<int>(ref keepMaleAdultCount, "keepMaleAdultCount", 10);
            Scribe_Values.Look<int>(ref keepMaleYoungCount, "keepMaleYoungCount", 10);
            Scribe_Values.Look<int>(ref keepFemaleAdultCount, "keepFemaleAdultCount", 10);
            Scribe_Values.Look<int>(ref keepFemaleYoungCount, "keepFemaleYoungCount", 10);

            Scribe_Values.Look<int>(ref keepNoneYoungCount, "keepNoneYoungCount", 10);
            Scribe_Values.Look<int>(ref keepNoneAdultCount, "keepNoneAdultCount", 10);

            Scribe_Defs.Look<ThingDef>(ref def, "def");
        }

        public int KeepCount(Gender gender, bool adult)
        {
            switch (gender)
            {
                case Gender.Male when adult:
                    return keepMaleAdultCount;
                case Gender.Male:
                    return keepMaleYoungCount;
                case Gender.Female when adult:
                    return keepFemaleAdultCount;
                case Gender.Female:
                    return keepFemaleYoungCount;
                default:
                {
                    return adult ? keepNoneAdultCount : keepNoneYoungCount;
                }
            }
        }
    }

    interface ISlaughterhouse
    {
        Dictionary<ThingDef, SlaughterSettings> Settings { get; }
    }

    public class ITab_Slaughterhouse : ITab
    {

        ISlaughterhouse slaughterhouse => SelThing as ISlaughterhouse;

        public Dictionary<ThingDef, SlaughterSettings> Settings => slaughterhouse.Settings;

        public ITab_Slaughterhouse()
        {
            labelKey = "PRFSlaughterhouseTab";
        }

        //Needs to be in sync with winSize.x
        private static readonly float[] ColumnWidth = new float[] { 0.28f ,
            0.07f , 0.07f, 0.07f, 0.07f,
            0.11f, 0.11f, 0.11f, 0.11f };

        private Func<float, Rect> CutLeftFunc(Rect rect)
        {
            var curX = 0f;
            return (pct) =>
            {
                var r = new Rect(curX, rect.y, rect.width * pct, rect.height);
                curX += rect.width * pct;
                return r;
            };
        }

        private static readonly TipSignal SlaughterTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.DoSlaughterTip".Translate());
        private static readonly TipSignal HasBondsTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.BondsTip".Translate());
        private static readonly TipSignal PregnancyTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.PregnancyTip".Translate());
        private static readonly TipSignal TrainedTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.TrainedTip".Translate());

        private static readonly TipSignal KeepMaleChildCountTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Male".Translate(), "PRF.AutoMachineTool.Young".Translate()));
        private static readonly TipSignal KeepFemaleChildCountTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Female".Translate(), "PRF.AutoMachineTool.Young".Translate()));
        private static readonly TipSignal KeepMaleAdultCountTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Male".Translate(), "PRF.AutoMachineTool.Adult".Translate()));
        private static readonly TipSignal KeepFemaleAdultCountTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Female".Translate(), "PRF.AutoMachineTool.Adult".Translate()));

        private static readonly TipSignal KeepNoneAdultCountTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.None".Translate(), "PRF.AutoMachineTool.Adult".Translate()));
        private static readonly TipSignal KeepNoneChildCountTip = new("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.None".Translate(), "PRF.AutoMachineTool.Young".Translate()));

        private Vector2 scrollPosition;
        private static Vector2 sscrollPosition;
        private readonly string description = "PRF.AutoMachineTool.Slaughterhouse.Setting.Description".Translate();

        private static List<ThingDef> Defs => Find.CurrentMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
            .Where(p => p.RaceProps.Animal && p.RaceProps.IsFlesh && p.SpawnedOrAnyParentSpawned)
            .Select(p => p.def).Distinct().ToList();


        private Vector2 winSize = new Vector2(560, 400f);

        protected override void UpdateSize()
        {
            winSize.y = 20 + 40 + 20 + 20 + 24 + Defs.Count() * 36;

            //Limit max hight to 400
            winSize.y = Mathf.Min(winSize.y, 400f);

            size = winSize;
            base.UpdateSize();
        }

        protected override void FillTab()
        {
            var list = new Listing_Standard();
            var inRect = new Rect(0f, 0f, winSize.x, winSize.y).ContractedBy(10f);
            list.Begin(inRect);

            //Get the Variable from the Static - This is needed as you cant pass a static by ref (& by ref is requere in this case)
            scrollPosition = sscrollPosition;

            //Add header Discription for this settings Section
            var rect = list.GetRect(40f);
            Widgets.Label(rect, description);

            //Need to fix that as step one
            var maxPossibleY = (winSize.y + (int)list.CurHeight) + 20;
            maxPossibleY = Mathf.Min(maxPossibleY, inRect.height);

            var outRect = new Rect(0f, list.CurHeight, winSize.x, maxPossibleY).ContractedBy(10f);
            // Log.Message("ITab_Settings_Additional_y + (int)list.CurHeight: " + (ITab_Settings_Additional_y + (int)list.CurHeight) + " - parrent_rect.height: " + parrent_rect.height + " - list.CurHeight" + list.CurHeight);

            var headerRect = list.GetRect(24f);
            headerRect.width -= 30f;

            var cutLeftHeader = CutLeftFunc(headerRect);

            var colIndex = 0;
            var col = cutLeftHeader(ColumnWidth[colIndex++]);
            Widgets.Label(col, "PRF.AutoMachineTool.RaceName".Translate());

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.SlaughterIcon);
            TooltipHandler.TipRegion(col, SlaughterTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.BondIcon);
            TooltipHandler.TipRegion(col, HasBondsTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.PregnantIcon);
            TooltipHandler.TipRegion(col, PregnancyTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.TrainedIcon);
            TooltipHandler.TipRegion(col, TrainedTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.MaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.YoungIcon);
            TooltipHandler.TipRegion(col, KeepMaleChildCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.FemaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.YoungIcon);
            TooltipHandler.TipRegion(col, KeepFemaleChildCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.MaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.AdultIcon);
            TooltipHandler.TipRegion(col, KeepMaleAdultCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.FemaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.AdultIcon);
            TooltipHandler.TipRegion(col, KeepFemaleAdultCountTip);

            var scrollOutRect = list.GetRect(outRect.height - list.CurHeight);

            var scrollViewRect = new Rect(scrollOutRect.x, 0, scrollOutRect.width - 30f, Defs.Count() * 36f);

            var innerlist = new Listing_Standard();
            Widgets.BeginScrollView(scrollOutRect, ref scrollPosition, scrollViewRect);
            innerlist.Begin(scrollViewRect);

            Defs.ForEach(d =>
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
                TooltipHandler.TipRegion(col, SlaughterTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.hasBonds, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, HasBondsTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.pregnancy, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, PregnancyTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.trained, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, TrainedTip);

                if (s.def.race.hasGenders)
                {
                    col = cutLeft(ColumnWidth[colIndex++]);
                    var buf1 = s.keepMaleYoungCount.ToString();
                    Widgets.TextFieldNumeric(col, ref s.keepMaleYoungCount, ref buf1, 0, 1000);
                    TooltipHandler.TipRegion(col, KeepMaleChildCountTip);

                    col = cutLeft(ColumnWidth[colIndex++]);
                    var buf2 = s.keepFemaleYoungCount.ToString();
                    Widgets.TextFieldNumeric(col, ref s.keepFemaleYoungCount, ref buf2, 0, 1000);
                    TooltipHandler.TipRegion(col, KeepFemaleChildCountTip);

                    col = cutLeft(ColumnWidth[colIndex++]);
                    var buf3 = s.keepMaleAdultCount.ToString();
                    Widgets.TextFieldNumeric(col, ref s.keepMaleAdultCount, ref buf3, 0, 1000);
                    TooltipHandler.TipRegion(col, KeepMaleAdultCountTip);

                    col = cutLeft(ColumnWidth[colIndex++]);
                    var buf4 = s.keepFemaleAdultCount.ToString();
                    Widgets.TextFieldNumeric(col, ref s.keepFemaleAdultCount, ref buf4, 0, 1000);
                    TooltipHandler.TipRegion(col, KeepFemaleAdultCountTip);
                }
                else
                {
                    col = cutLeft(ColumnWidth[colIndex++] * 2);
                    var buf1 = s.keepNoneYoungCount.ToString();
                    Widgets.TextFieldNumeric(col, ref s.keepNoneYoungCount, ref buf1, 0, 1000);
                    TooltipHandler.TipRegion(col, KeepNoneChildCountTip);


                    col = cutLeft(ColumnWidth[colIndex++] * 2);
                    var buf3 = s.keepNoneAdultCount.ToString();
                    Widgets.TextFieldNumeric(col, ref s.keepNoneAdultCount, ref buf3, 0, 1000);
                    TooltipHandler.TipRegion(col, KeepNoneAdultCountTip);

                    colIndex += 2;
                }

            });
            Widgets.EndScrollView();
            innerlist.End();
            //Update the static variable to keep the data
            sscrollPosition = scrollPosition;
            list.End();

        }
    }
}
