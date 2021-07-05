using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    class ITab_ConveyorFilter : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(300f, 500f);

        //TODO: switch XML to use vanilla direction names as keys
        //   (in some languages *to* a direction is not the same as 
        //    *at* a direction, so still needs translation)
        private static readonly Dictionary<Rot4, string> RotStrings = new Dictionary<Rot4, string>() { { Rot4.North, "N" }, { Rot4.East, "E" }, { Rot4.South, "S" }, { Rot4.West, "W" } };

        public ITab_ConveyorFilter()
        {
            this.size = WinSize;
            this.labelKey = "PRF.AutoMachineTool.Conveyor.OutputItemFilter.TabName";

            this.description = "PRF.AutoMachineTool.Conveyor.OutputItemFilter.Description".Translate();
        }

        private string description;

        private Building_BeltSplitter Splitter { get => (Building_BeltSplitter)this.SelThing;}
        private Rot4? selectedDir;

        public override bool IsVisible => true; //TODO: do this.Conveyor.Filters.Count > 1;?
                                                //currently, if no filters allow going in a 
                                                //direction....??

        public override void OnOpen()
        {
            base.OnOpen();

            this.groups = this.Splitter.Map.haulDestinationManager.AllGroups.ToList();
            selectedDir = this.Splitter.OutputLinks.Keys.FirstOrDefault();
        }

        private List<SlotGroup> groups;

        private ThingFilterUI.UIState uIState;

        protected override void FillTab() {
            if (selectedDir == null) selectedDir = // in case something kills it while ITab
                  this.Splitter.OutputLinks.Keys.FirstOrDefault(null);  //  is already open

            /*            if (!this.rotSelectedDic.ContainsKey(this.Splitter))
                        {
                            Dictionary<Rot4, bool> newDic = Enumerable.Range(0, 4).ToDictionary(k => new Rot4(k), v => false);newDic[this.Conveyor.Filters.First().Key] = true;
                            this.rotSelectedDic[this.Conveyor] = newDic;
                        }
            //            Dictionary<Rot4, bool> dic = this.rotSelectedDic[this.Conveyor];
                        if (!this.Conveyor.Filters.ContainsKey(dic.Where(kv => kv.Value).First().Key))
                        {
                            new Dictionary<Rot4, bool>(dic).ForEach(x => dic[x.Key] = false);
                            dic[this.Conveyor.Filters.First().Key] = true;
                        }
                        */
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            list.Begin(inRect);
            list.Gap();

            #region  ------ bonus title ------
            var rect = list.GetRect(40f);
            Widgets.Label(rect, this.description);
            list.Gap();
            #endregion
            #region ------ directions quadrants ------
            Dictionary<Rot4, Rect> pos = new Dictionary<Rot4, Rect>();
            rect = list.GetRect(30f);
            pos[Rot4.North] = new Rect(rect.x + rect.width / 4, rect.y, rect.width / 2, rect.height);
            rect = list.GetRect(30f);
            pos[Rot4.West] = rect.LeftHalf();
            pos[Rot4.East] = rect.RightHalf();
            rect = list.GetRect(30f);
            pos[Rot4.South] = new Rect(rect.x + rect.width / 4, rect.y, rect.width / 2, rect.height);
            #endregion
            #region ------ fill dir quadrants ------
            // note to self: Enumerable.Range(0, 4) starts at 0 and gives 4 elements.
            foreach (var dir in Enumerable.Range(0, 4).Select(n => new Rot4(n))) {
                var dirName = ("PRF.AutoMachineTool.OutputDirection." + RotStrings[dir]).Translate();
                if (Widgets.RadioButtonLabeled(pos[dir].LeftHalf(), dirName, dir == selectedDir))
                    //                    if (Widgets.RadioButton(pos[dir].RightHalf().position, dir == selectedDir))
                    selectedDir = dir;
                //              Text.Anchor = TextAnchor.MiddleRight;
                //              Widgets.Label(pos[dir].LeftHalf(), dirName);
                bool nowActive = false;
                if (this.Splitter.OutputLinks.ContainsKey(dir)) {
                    nowActive = Splitter.OutputLinks[dir].Active;
                }
                bool origValue = nowActive;
                //TODO: translate
                Widgets.CheckboxLabeled(pos[dir].RightHalf(), "Active? ", ref nowActive);
                if (nowActive != origValue) {
                    if (this.Splitter.OutputLinks.ContainsKey(dir)) {
                        Splitter.OutputLinks[dir].Active = nowActive;
                    } else {
                        if (nowActive) {
                            Splitter.AddOutgoingLink(dir);
                        } else {
                            // should never reach here?
                        }
                    }
                    /*                } else {
                                        bool nowActive = false;
                                        Text.Anchor = TextAnchor.MiddleLeft;
                                        GameFont tmp = Text.Font;
                                        Text.Font = GameFont.Tiny;
                                        //TODO: Change this to "No link"
                                        Widgets.Label(pos[dir].RightHalf(), "PRF.AutoMachineTool.Conveyor.OutputItemFilter.NothingOutputDestination".Translate());
                                        Text.Font = tmp;
                                        */
                }

            }
            list.Gap();
            #endregion
            var selectedRot = (Rot4)selectedDir;
            if (selectedDir == null || !Splitter.OutputLinks.ContainsKey(selectedRot)) {
                list.End();
                return;
            }
            #region ------ Priority ------
            rect = list.GetRect(30f);
            Widgets.Label(rect.LeftHalf(), "PRF.AutoMachineTool.Priority".Translate());
            if (Widgets.ButtonText(rect.RightHalf(), this.Splitter.OutputLinks[selectedRot]
                .priority.ToText())) {
                Find.WindowStack.Add(new FloatMenu(GetEnumValues<DirectionPriority>()
                    .OrderByDescending(k => (int)k).Select(d => new FloatMenuOption(d.ToText(),
                    () => this.Splitter.OutputLinks[selectedRot].priority = d
                    )).ToList()));
            }
            list.Gap();
            #endregion
            rect = list.GetRect(30f);
            if (Widgets.ButtonText(rect, "PRF.AutoMachineTool.Conveyor.FilterCopyFrom".Translate())) {
                Find.WindowStack.Add(new FloatMenu(groups.Select(g => new FloatMenuOption(g.parent.SlotYielderLabel(),
                    () => this.Splitter.OutputLinks[selectedRot].CopyAllowancesFrom(g.Settings.filter)
                    )).ToList()));
            }
            list.Gap();
            list.End();
            var height = list.CurHeight;
            Splitter.OutputLinks[selectedRot].DoThingFilterConfigWindow(
                inRect.BottomPartPixels(inRect.height - height), uIState);
        }
    }
}
