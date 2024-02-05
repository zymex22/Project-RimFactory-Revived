using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
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

        private Building_BeltSplitter Splitter { get => (Building_BeltSplitter)this.SelThing; }
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

        private ThingFilterUI.UIState uIState = new ThingFilterUI.UIState();

        private void directionUI(Rect rect, Rot4 rot, ref bool selected, ref bool disabeld, bool isInput)
        {
            Texture2D texture = RS.SplitterArrow_Up; //Default
            Common.CommonColors.CellPattern cellPattern = Common.CommonColors.CellPattern.InputCell;

            if (isInput)
            {
                if (rot == Rot4.North) texture = RS.SplitterArrow_Down;
                else if (rot == Rot4.South) texture = RS.SplitterArrow_Up;
                else if (rot == Rot4.East) texture = RS.SplitterArrow_Left;
                else if (rot == Rot4.West) texture = RS.SplitterArrow_Right;
            }
            else
            {
                if (rot == Rot4.North) texture = RS.SplitterArrow_Up;
                else if (rot == Rot4.South) texture = RS.SplitterArrow_Down;
                else if (rot == Rot4.East) texture = RS.SplitterArrow_Right;
                else if (rot == Rot4.West) texture = RS.SplitterArrow_Left;
                cellPattern = Common.CommonColors.CellPattern.OutputCell;
            }

            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, alphaBlend: true, 0f, Common.CommonColors.GetCellPatternColor(cellPattern), 0f, 0f);


            if (!disabeld && !isInput) GUI.DrawTexture(rect, RS.SplitterDisabeld);
            if (selected)
            {
                Widgets.DrawHighlight(rect);
                Widgets.DrawBox(rect);
            }
            Event cur = Event.current;
            if (cur.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                //Left
                if (Input.GetMouseButton(0) && !isInput)
                {
                    selected = true;
                }
                //Right
                if (Input.GetMouseButton(1) && !isInput)
                {
                    disabeld = !disabeld;
                }
            }



        }

        private Rot4 OppositeRot(Rot4 rot)
        {
            if (rot == Rot4.North) return Rot4.South;
            if (rot == Rot4.South) return Rot4.North;
            if (rot == Rot4.East) return Rot4.West;
            if (rot == Rot4.West) return Rot4.East;
            return Rot4.North; //This cant happen
        }


        protected override void FillTab()
        {
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
            float quadrants_size = 40f; //30 seems small but there is not much space
            rect = list.GetRect(quadrants_size * 3); //Top , middle, Bottom
            float quadrants_middle = rect.x + rect.width / 2;
            pos[Rot4.North] = new Rect(quadrants_middle - quadrants_size / 2, rect.y, quadrants_size, quadrants_size);
            pos[Rot4.West] = new Rect(quadrants_middle - quadrants_size * 3 / 2, rect.y + quadrants_size, quadrants_size, quadrants_size);
            pos[Rot4.East] = new Rect(quadrants_middle + quadrants_size / 2, rect.y + quadrants_size, quadrants_size, quadrants_size);
            pos[Rot4.South] = new Rect(quadrants_middle - quadrants_size / 2, rect.y + quadrants_size * 2, quadrants_size, quadrants_size);

            #endregion
            #region ------ fill dir quadrants ------
            // note to self: Enumerable.Range(0, 4) starts at 0 and gives 4 elements.

            foreach (var dir in Enumerable.Range(0, 4).Select(n => new Rot4(n)))
            {
                bool enabeld = false;
                if (this.Splitter.OutputLinks.ContainsKey(dir))
                {
                    enabeld = Splitter.OutputLinks[dir].Active;
                }

                bool selref = selectedDir == dir;
                bool isInput = false;
                
                foreach (IBeltConveyorLinkable linkable in Splitter.IncomingLinks.Where(l => l.Position == (Splitter.Position + dir.FacingCell)))
                {

                    isInput = Splitter.IsInputtingIntoThis(linkable, dir);
                    if (isInput) break;
                }


                directionUI(pos[dir], dir, ref selref, ref enabeld, isInput);
                if (selref) selectedDir = dir;
                if (this.Splitter.OutputLinks.ContainsKey(dir))
                {
                    Splitter.OutputLinks[dir].Active = enabeld;
                }
                else
                {
                    if (enabeld)
                    {
                        Splitter.AddOutgoingLink(dir);
                    }
                    else
                    {
                        // should never reach here?
                    }
                }

            }
            list.Gap();
            #endregion


            var selectedRot = (Rot4)selectedDir;
            if (selectedDir == null || !Splitter.OutputLinks.ContainsKey(selectedRot))
            {
                list.End();
                return;
            }


            rect = list.GetRect(35 - 6);
            float maxWidth = rect.width;
            float prioritywidth = (maxWidth / 2) - 1; //145 - 160 vanilla
            float selectCopywidth = (maxWidth / 2) - 1; //125
            rect.width = prioritywidth;
            GameFont fontOld = Text.Font;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(rect, "Priority".Translate() + ": " + this.Splitter.OutputLinks[selectedRot].priority.ToText()))
            {
                Find.WindowStack.Add(new FloatMenu(GetEnumValues<DirectionPriority>()
                    .OrderByDescending(k => (int)k).Select(d => new FloatMenuOption(d.ToText(),
                    () => this.Splitter.OutputLinks[selectedRot].priority = d
                    )).ToList()));
            }
            Text.Font = fontOld;

            rect.x = maxWidth - selectCopywidth;
            rect.width = selectCopywidth;
            if (Widgets.ButtonText(rect, "PRF.AutoMachineTool.Conveyor.FilterCopyFrom".Translate()))
            {
                List<FloatMenuOption> menuOpt = groups.Select(g => new FloatMenuOption(g.parent.SlotYielderLabel(),
                    () => this.Splitter.OutputLinks[selectedRot].CopyAllowancesFrom(g.Settings.filter)
                    )).ToList();
                if (menuOpt.Count > 0) Find.WindowStack.Add(new FloatMenu(menuOpt));
            }
            list.Gap();

            list.End();

            var height = list.CurHeight;
            Splitter.OutputLinks[selectedRot].DoThingFilterConfigWindow(
                inRect.BottomPartPixels(inRect.height - height), uIState);
        }
    }
}
