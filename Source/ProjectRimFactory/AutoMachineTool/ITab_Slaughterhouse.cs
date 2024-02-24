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
            this.doSlaughter = false;
            this.hasBonds = false;
            this.pregnancy = false;
            this.trained = false;

            this.keepNoneAdultCount = 10;
            this.keepNoneYoungCount = 10;

            this.keepMaleAdultCount = 10;
            this.keepMaleYoungCount = 10;
            this.keepFemaleAdultCount = 10;
            this.keepFemaleYoungCount = 10;
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
            Scribe_Values.Look<bool>(ref this.doSlaughter, "doSlaughter", false);
            Scribe_Values.Look<bool>(ref this.hasBonds, "hasBonds", false);
            Scribe_Values.Look<bool>(ref this.pregnancy, "pregnancy", false);
            Scribe_Values.Look<bool>(ref this.trained, "trained", false);

            Scribe_Values.Look<int>(ref this.keepMaleAdultCount, "keepMaleAdultCount", 10);
            Scribe_Values.Look<int>(ref this.keepMaleYoungCount, "keepMaleYoungCount", 10);
            Scribe_Values.Look<int>(ref this.keepFemaleAdultCount, "keepFemaleAdultCount", 10);
            Scribe_Values.Look<int>(ref this.keepFemaleYoungCount, "keepFemaleYoungCount", 10);

            Scribe_Values.Look<int>(ref this.keepNoneYoungCount, "keepNoneYoungCount", 10);
            Scribe_Values.Look<int>(ref this.keepNoneAdultCount, "keepNoneAdultCount", 10);

            Scribe_Defs.Look<ThingDef>(ref this.def, "def");
        }

        public int KeepCount(Gender gender, bool adult)
        {
            if (gender == Gender.Male)
            {
                if (adult)
                    return keepMaleAdultCount;
                else
                    return keepMaleYoungCount;
            }
            else if (gender == Gender.Female)
            {
                if (adult)
                    return keepFemaleAdultCount;
                else
                    return keepFemaleYoungCount;
            }
            else
            {
                if (adult)
                    return keepNoneAdultCount;
                else
                    return keepNoneYoungCount;
            }
        }
    }

    interface ISlaughterhouse
    {
        Dictionary<ThingDef, SlaughterSettings> Settings { get; }
    }

    public class ITab_Slaughterhouse : ITab
    {

        ISlaughterhouse slaughterhouse => this.SelThing as ISlaughterhouse;

        public Dictionary<ThingDef, SlaughterSettings> Settings { get => slaughterhouse.Settings; }

        public ITab_Slaughterhouse()
        {
            this.labelKey = "PRFSlaughterhouseTab";
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

        private static TipSignal slaughterTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.DoSlaughterTip".Translate());
        private static TipSignal hasBondsTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.BondsTip".Translate());
        private static TipSignal pregnancyTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.PregnancyTip".Translate());
        private static TipSignal trainedTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.TrainedTip".Translate());

        private static TipSignal keepMaleChildCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Male".Translate(), "PRF.AutoMachineTool.Young".Translate()));
        private static TipSignal keepFemaleChildCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Female".Translate(), "PRF.AutoMachineTool.Young".Translate()));
        private static TipSignal keepMaleAdultCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Male".Translate(), "PRF.AutoMachineTool.Adult".Translate()));
        private static TipSignal keepFemaleAdultCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Female".Translate(), "PRF.AutoMachineTool.Adult".Translate()));

        private static TipSignal keepNoneAdultCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.None".Translate(), "PRF.AutoMachineTool.Adult".Translate()));
        private static TipSignal keepNoneChildCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.None".Translate(), "PRF.AutoMachineTool.Young".Translate()));

        private Vector2 scrollPosition;
        private static Vector2 sscrollPosition;
        private string description = "PRF.AutoMachineTool.Slaughterhouse.Setting.Description".Translate();

        private List<ThingDef> defs => Find.CurrentMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal && p.RaceProps.IsFlesh && p.SpawnedOrAnyParentSpawned).Select(p => p.def).Distinct().ToList();


        private Vector2 winSize = new Vector2(560, 400f);

        protected override void UpdateSize()
        {
            winSize.y = 20 + 40 + 20 + 20 + 24 + defs.Count() * 36;

            //Limit max hight to 400
            winSize.y = Mathf.Min(winSize.y, 400f);

            this.size = winSize;
            base.UpdateSize();
        }

        protected override void FillTab()
        {
            var list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, winSize.x, winSize.y).ContractedBy(10f);
            list.Begin(inRect);

            //Get the Variable from the Static - This is needed as you cant pass a static by ref (& by ref is requere in this case)
            scrollPosition = sscrollPosition;

            //Add header Discription for this settings Section
            var rect = list.GetRect(40f);
            Widgets.Label(rect, this.description);

            //Need to fix that as step one
            float maxPossibleY = (winSize.y + (int)list.CurHeight) + 20;
            maxPossibleY = Mathf.Min(maxPossibleY, inRect.height);

            Rect outRect = new Rect(0f, list.CurHeight, winSize.x, maxPossibleY).ContractedBy(10f);
            // Log.Message("ITab_Settings_Additional_y + (int)list.CurHeight: " + (ITab_Settings_Additional_y + (int)list.CurHeight) + " - parrent_rect.height: " + parrent_rect.height + " - list.CurHeight" + list.CurHeight);

            var headerRect = list.GetRect(24f);
            headerRect.width -= 30f;

            var cutLeftHeader = CutLeftFunc(headerRect);

            int colIndex = 0;
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

            var scrollViewRect = new Rect(scrollOutRect.x, 0, scrollOutRect.width - 30f, this.defs.Count() * 36f);

            var innerlist = new Listing_Standard();
            Widgets.BeginScrollView(scrollOutRect, ref scrollPosition, scrollViewRect);
            innerlist.Begin(scrollViewRect);

            this.defs.ForEach(d =>
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

                if (s.def.race.hasGenders)
                {
                    col = cutLeft(ColumnWidth[colIndex++]);
                    string buf1 = s.keepMaleYoungCount.ToString();
                    Widgets.TextFieldNumeric<int>(col, ref s.keepMaleYoungCount, ref buf1, 0, 1000);
                    TooltipHandler.TipRegion(col, keepMaleChildCountTip);

                    col = cutLeft(ColumnWidth[colIndex++]);
                    string buf2 = s.keepFemaleYoungCount.ToString();
                    Widgets.TextFieldNumeric<int>(col, ref s.keepFemaleYoungCount, ref buf2, 0, 1000);
                    TooltipHandler.TipRegion(col, keepFemaleChildCountTip);

                    col = cutLeft(ColumnWidth[colIndex++]);
                    string buf3 = s.keepMaleAdultCount.ToString();
                    Widgets.TextFieldNumeric<int>(col, ref s.keepMaleAdultCount, ref buf3, 0, 1000);
                    TooltipHandler.TipRegion(col, keepMaleAdultCountTip);

                    col = cutLeft(ColumnWidth[colIndex++]);
                    string buf4 = s.keepFemaleAdultCount.ToString();
                    Widgets.TextFieldNumeric<int>(col, ref s.keepFemaleAdultCount, ref buf4, 0, 1000);
                    TooltipHandler.TipRegion(col, keepFemaleAdultCountTip);
                }
                else
                {
                    col = cutLeft(ColumnWidth[colIndex++] * 2);
                    string buf1 = s.keepNoneYoungCount.ToString();
                    Widgets.TextFieldNumeric<int>(col, ref s.keepNoneYoungCount, ref buf1, 0, 1000);
                    TooltipHandler.TipRegion(col, keepNoneChildCountTip);


                    col = cutLeft(ColumnWidth[colIndex++] * 2);
                    string buf3 = s.keepNoneAdultCount.ToString();
                    Widgets.TextFieldNumeric<int>(col, ref s.keepNoneAdultCount, ref buf3, 0, 1000);
                    TooltipHandler.TipRegion(col, keepNoneAdultCountTip);

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
