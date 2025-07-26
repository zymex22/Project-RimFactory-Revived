using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        private static readonly Dictionary<Rot4, string> RotStrings = new() { { Rot4.North, "N" }, { Rot4.East, "E" }, { Rot4.South, "S" }, { Rot4.West, "W" } };

        public ITab_ConveyorFilter()
        {
            size = WinSize;
            labelKey = "PRF.AutoMachineTool.Conveyor.OutputItemFilter.TabName";

            description = "PRF.AutoMachineTool.Conveyor.OutputItemFilter.Description".Translate();
        }

        private string description;

        private Building_BeltSplitter Splitter => (Building_BeltSplitter)SelThing;
        private Rot4? selectedDir;

        public override bool IsVisible => true; //TODO: do this.Conveyor.Filters.Count > 1;?
                                                //currently, if no filters allow going in a 
                                                //direction....??

        public override void OnOpen()
        {
            base.OnOpen();
            groups = Splitter.Map.haulDestinationManager.AllGroups.ToList();
            selectedDir = Splitter.OutputLinks.Keys.FirstOrDefault();
        }

        private List<SlotGroup> groups;

        private ThingFilterUI.UIState uIState = new();

        private void directionUI(Rect rect, Rot4 rot, ref bool selected, ref bool disabeld, bool isInput)
        {
            var texture = RS.SplitterArrow_Up; //Default
            var cellPattern = Common.CommonColors.CellPattern.InputCell;

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
            var cur = Event.current;
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
            selectedDir ??= Splitter.OutputLinks.Keys.FirstOrDefault(null);

            var list = new Listing_Standard();
            var inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            list.Begin(inRect);
            list.Gap();

            #region  ------ bonus title ------
            var rect = list.GetRect(40f);
            Widgets.Label(rect, description);
            list.Gap();
            #endregion
            #region ------ directions quadrants ------
            var pos = new Dictionary<Rot4, Rect>();
            const float quadrantsSize = 40f; //30 seems small but there is not much space
            rect = list.GetRect(quadrantsSize * 3); //Top , middle, Bottom
            var quadrants_middle = rect.x + rect.width / 2;
            pos[Rot4.North] = new Rect(quadrants_middle - quadrantsSize / 2, rect.y, quadrantsSize, quadrantsSize);
            pos[Rot4.West] = new Rect(quadrants_middle - quadrantsSize * 3 / 2, rect.y + quadrantsSize, quadrantsSize, quadrantsSize);
            pos[Rot4.East] = new Rect(quadrants_middle + quadrantsSize / 2, rect.y + quadrantsSize, quadrantsSize, quadrantsSize);
            pos[Rot4.South] = new Rect(quadrants_middle - quadrantsSize / 2, rect.y + quadrantsSize * 2, quadrantsSize, quadrantsSize);

            #endregion
            #region ------ fill dir quadrants ------
            // note to self: Enumerable.Range(0, 4) starts at 0 and gives 4 elements.

            foreach (var dir in Enumerable.Range(0, 4).Select(n => new Rot4(n)))
            {
                var enabled = false;
                if (Splitter.OutputLinks.ContainsKey(dir))
                {
                    enabled = Splitter.OutputLinks[dir].Active;
                }

                var selfRef = selectedDir == dir;
                var isInput = false;
                
                foreach (var linkable in Splitter.IncomingLinks.Where(l => l.Position == (Splitter.Position + dir.FacingCell)))
                {
                    isInput = Building_BeltSplitter.IsInputtingIntoThis(linkable, dir);
                    if (isInput) break;
                }


                directionUI(pos[dir], dir, ref selfRef, ref enabled, isInput);
                if (selfRef) selectedDir = dir;
                if (Splitter.OutputLinks.ContainsKey(dir))
                {
                    Splitter.OutputLinks[dir].Active = enabled;
                }
                else
                {
                    if (enabled)
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
            var maxWidth = rect.width;
            var priorityWidth = (maxWidth / 2) - 1; //145 - 160 vanilla
            var selectCopyWidth = (maxWidth / 2) - 1; //125
            rect.width = priorityWidth;
            var fontOld = Text.Font;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(rect, "Priority".Translate() + ": " + Splitter.OutputLinks[selectedRot].Priority.ToText()))
            {
                Find.WindowStack.Add(new FloatMenu(GetEnumValues<DirectionPriority>()
                    .OrderByDescending(k => (int)k).Select(d => new FloatMenuOption(d.ToText(),
                    () => Splitter.OutputLinks[selectedRot].Priority = d
                    )).ToList()));
            }
            Text.Font = fontOld;

            rect.x = maxWidth - selectCopyWidth;
            rect.width = selectCopyWidth;
            if (Widgets.ButtonText(rect, "PRF.AutoMachineTool.Conveyor.FilterCopyFrom".Translate()))
            {
                var menuOpt = groups.Select(g => new FloatMenuOption(g.parent.SlotYielderLabel(),
                    () => Splitter.OutputLinks[selectedRot].CopyAllowancesFrom(g.Settings.filter))).ToList();
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
