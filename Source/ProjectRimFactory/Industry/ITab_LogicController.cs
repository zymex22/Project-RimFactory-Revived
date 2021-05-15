using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.Industry
{

    #region Drag_Logic



    abstract class SnapPoint
    {
        private Vector2 position;

        /// <summary>
        /// This Position is not absolute but relative to the Position of the Hitbox / Dragable_LogicBlock
        /// </summary>
        public Vector2 Position { get => position; set => position = value; }

        public virtual bool Accepts(SnapPoint block)
        {
            return false;
        }

    }

    class SnapPoint_Input : SnapPoint
    {
        public override bool Accepts(SnapPoint block)
        {
            if (block is SnapPoint_Input) return false;
            return true;
        }
        public SnapPoint_Input(Vector2 pos)
        {
            Position = pos;
        }
    }
    class SnapPoint_Output : SnapPoint
    {
        public override bool Accepts(SnapPoint block)
        {
            if (block is SnapPoint_Output) return false;
            return true;
        }
        public SnapPoint_Output(Vector2 pos)
        {
            Position = pos;
        }
    }







    class LogicHitbox
    {
        public Vector2 Position;
        public Vector2 Size;

        public List<SnapPoint> SnapPoints;

        public LogicHitbox(Vector2 position, Vector2 size, List<SnapPoint> snapPoints)
        {
            SnapPoints = snapPoints;
            Size = size;
            Position = position;
        }
    }



    static class GUI_HitBox_Register
    {
        public static Dictionary<Dragable_LogicBlock, LogicHitbox> Hitboxes = new Dictionary<Dragable_LogicBlock, LogicHitbox>();

        public static Rect Boundary;

        public static LogicHitbox FindPotentialSnapPoint(SnapPoint point)
        {



            return null; // default
        }


    }


    abstract class Dragable_LogicBlock
    {
        protected virtual Texture2D image => null;

        public Vector2 size;
        public Vector2 Position;


        public LogicHitbox Hitbox = null;


        public abstract List<SnapPoint> GenerateSnapPonts();

        public void UpdateHitBox()
        {
            if (Hitbox == null)
            {
                //Init
                Hitbox = new LogicHitbox(Position, size, GenerateSnapPonts());
                GUI_HitBox_Register.Hitboxes.Add(this, Hitbox);
            }
            else
            {
                Hitbox.Position = Position;
            }

        }

        /// <summary>
        /// Prevent this item from Sliding over another
        /// Or escape from the relevant area
        /// </summary>
        /// <returns></returns>
        private bool CheckDragCollision()
        {
            Vector2 pos = Event.current.mousePosition;
            pos.x -= size.x / 2;
            pos.y -= size.y / 2;
            //pos is at the Top Left
            //The mouse is at the centre

            //Check the Container
            if (pos.y < GUI_HitBox_Register.Boundary.y || pos.x < GUI_HitBox_Register.Boundary.x || (pos.x + size.x) > GUI_HitBox_Register.Boundary.xMax || (pos.y + size.y) > GUI_HitBox_Register.Boundary.yMax)
            {
                // Log.Message("pos: " + pos + "   Boundary: " + GUI_HitBox_Register.Boundary);
                return false;
            }

            var test = new Rect(pos, size);
            var other = new Rect(pos, size);

            //Check Other Hit Boxes
            foreach (LogicHitbox logicHitbox in GUI_HitBox_Register.Hitboxes.Where(h => h.Key != this).ToList().Select(p => p.Value))
            {
                other.position = logicHitbox.Position;
                other.size = logicHitbox.Size;

                if (test.Overlaps(other))
                {
                    return false;
                }

            }






            return true;
        }

        public void HandleElement()
        {
            var test = new Rect(Position, size);
            Draw(test);
            if (Input.GetMouseButton(0) && Mouse.IsOver(test))
            {
                if (CheckDragCollision())
                {
                    Position = Event.current.mousePosition;
                    Position.x -= size.x / 2;
                    Position.y -= size.y / 2;
                }


                SnapPoint snap_o = Hitbox.SnapPoints.Where(p => p is SnapPoint_Output).First();
                if (snap_o != null)
                {
                    //Find any SnapPoints in snap range that accept this point

                }


                UpdateHitBox();
            }
        }


        public virtual void Draw(Rect test)
        {
            Widgets.ButtonImage(test, image);
        }

        public Dragable_LogicBlock()
        {

        }


    }

    class GUI_And_Block : Dragable_LogicBlock
    {
        protected override Texture2D image => RS.Algebra_And;

        public GUI_And_Block(Vector2 pos, Vector2 size)
        {
            this.size = size;
            Position = pos;
            UpdateHitBox();
        }

        public override List<SnapPoint> GenerateSnapPonts()
        {
            return new List<SnapPoint> { new SnapPoint_Input(new Vector2(0, size.y / 3)), new SnapPoint_Input(new Vector2(0, (size.y / 3) * 2)), new SnapPoint_Output(new Vector2(size.x, size.y / 2)) };
        }
    }
    class GUI_Or_Block : Dragable_LogicBlock
    {
        protected override Texture2D image => RS.Algebra_Or;

        public GUI_Or_Block(Vector2 pos, Vector2 size)
        {
            this.size = size;
            Position = pos;
            UpdateHitBox();
        }
        public override List<SnapPoint> GenerateSnapPonts()
        {
            return new List<SnapPoint> { new SnapPoint_Input(new Vector2(0, size.y / 3)), new SnapPoint_Input(new Vector2(0, (size.y / 3) * 2)), new SnapPoint_Output(new Vector2(size.x, size.y / 2)) };
        }
    }
    class GUI_Not_Block : Dragable_LogicBlock
    {
        protected override Texture2D image => RS.Algebra_Not;

        public GUI_Not_Block(Vector2 pos, Vector2 size)
        {
            this.size = size;
            Position = pos;
            UpdateHitBox();
        }
        public override List<SnapPoint> GenerateSnapPonts()
        {
            return new List<SnapPoint> { new SnapPoint_Input(new Vector2(0, size.y / 2)), new SnapPoint_Output(new Vector2(size.x, size.y / 2)) };
        }

    }
    class GUI_Input_Block : Dragable_LogicBlock
    {
        protected override Texture2D image => null;

        public GUI_Input_Block(Vector2 pos, Vector2 size)
        {
            this.size = size;
            Position = pos;
            UpdateHitBox();
        }

        public override void Draw(Rect test)
        {
            base.Draw(test);
        }
        public override List<SnapPoint> GenerateSnapPonts()
        {
            return new List<SnapPoint> { new SnapPoint_Output(new Vector2(size.x, size.y / 2)) };
        }
    }
    class GUI_Output_Block : Dragable_LogicBlock
    {
        protected override Texture2D image => null;

        public GUI_Output_Block(Vector2 pos, Vector2 size)
        {
            this.size = size;
            Position = pos;
            UpdateHitBox();
        }

        public override void Draw(Rect test)
        {
            base.Draw(test);
        }
        public override List<SnapPoint> GenerateSnapPonts()
        {
            return new List<SnapPoint> { new SnapPoint_Input(new Vector2(0, size.y / 2)) };
        }

    }













    #endregion

    class ITab_LogicController : ITab
    {

        private Building_LogicController this_Controller { get => this.SelThing as Building_LogicController; }

        public override bool IsVisible => base.IsVisible;

        ThingFilter dummyfilter = new ThingFilter();

        private List<TabRecord> tabs = new List<TabRecord>();


        Vector2 scrollPos_itemFilter = new Vector2();

        Vector2 scrollPos_ValueList = new Vector2();
        Vector2 scrollPos_LeafLogic = new Vector2();
        Vector2 scrollPos_LogicSignal = new Vector2();

        Vector2 scrollPos_AdvancedAlgebraEditor = new Vector2();

        private static GUIStyle richTextStyle
        {
            get
            {
                GUIStyle gtter_richTextStyle = new GUIStyle();
                gtter_richTextStyle.richText = true;
                gtter_richTextStyle.normal.textColor = Color.white;
                return gtter_richTextStyle;
            }
        }

        private int ValueRefListItem(ValueRefrence vr, Rect rect, int i, bool selected)
        {


            bool clicked = Widgets.ButtonInvisible(rect);
            string vrString = "";

            if (selected) vrString += "<color=red>";
            if (vr is ValueRefrence_Signal)
            {
                vrString += "(Signal) ";
            }
            else if (vr is ValueRefrence_Fixed)
            {
                vrString += "(Fixed) ";
            }
            else if (vr is ValueRefrence_ThingCount)
            {
                vrString += "(Thing) ";
            }

            vrString += vr.Name;
            if (vr.dynamicSlot == EnumDynamicSlotGroupID.NA)
            {
                vrString += "   " + vr.GetValue(null, null);
            }


            if (selected) vrString += "</color>";

            CommonGUIFunctions.Label(rect, vrString, richTextStyle);



            return clicked ? i : -1;
        }

        private int LeafLogicListItem(Leaf_Logic leaf, Rect rect, int i, bool selected)
        {


            bool clicked = Widgets.ButtonInvisible(rect);
            string vrString = "";

            if (selected) vrString += "<color=red>";


            vrString += leaf.Name;
            if (leaf.dynamicSlot == EnumDynamicSlotGroupID.NA)
            {
                vrString += "   " + leaf.GetVerdict(null, null);
            }


            if (selected) vrString += "</color>";

            CommonGUIFunctions.Label(rect, vrString, richTextStyle);



            return clicked ? i : -1;
        }


        private int LogicSignalListItem(LogicSignal signal, Rect rect, int i, bool selected)
        {


            bool clicked = Widgets.ButtonInvisible(rect);
            string vrString = "";

            if (selected) vrString += "<color=red>";


            vrString += signal.Name;
            if (signal.dynamicSlot == EnumDynamicSlotGroupID.NA)
            {
                vrString += "   " + signal.GetValue();
            }


            if (selected) vrString += "</color>";

            CommonGUIFunctions.Label(rect, vrString, richTextStyle);



            return clicked ? i : -1;
        }




        private string EnumCompareOperator_ToString(EnumCompareOperator enu)
        {
            switch (enu)
            {
                case EnumCompareOperator.Equal: return "==";
                case EnumCompareOperator.Greater: return ">";
                case EnumCompareOperator.GreaterEqual: return ">=";
                case EnumCompareOperator.NotEqual: return "!=";
                case EnumCompareOperator.Smaller: return "<";
                case EnumCompareOperator.SmallerEqual: return "<=";
                default: return "  ";
            }

        }


        private void LeafAlgebra_Advanced_Button(ref Rect EleRect, List<FloatMenuOption> floatMenuOptions, string text)
        {
            if (Widgets.ButtonText(EleRect, text))
            {
                Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
            }
            EleRect.x += EleRect.width + 10;
        }


        private string EnumBinaryAlgebra_toString(EnumBinaryAlgebra algebra)
        {
            switch (algebra)
            {
                case EnumBinaryAlgebra.bAND: return "∧";
                case EnumBinaryAlgebra.bBracketClose: return ")";
                case EnumBinaryAlgebra.bBracketOpen: return "(";
                case EnumBinaryAlgebra.bNA: return "N/A";
                case EnumBinaryAlgebra.bNOT: return "Not";
                case EnumBinaryAlgebra.bOR: return "∨";
                default: return "ERROR";
            }
        }


        /// <summary>
        /// This is the easier version for Users that are faliliar with Boolshe Algebra
        /// And It's Also "easier" to implement 
        /// TODO maybe add support for NOT & ()
        /// </summary>
        /// <param name="currentY"></param>
        private void AlgebraGUI_Advanced(ref float currentY, Rect TabRect, LogicSignal logicSignal)
        {



            var AlgebraGUI = new Rect(TabRect.x, currentY, TabRect.width - 50, 75);

            if (logicSignal.UserInfixValid)
            {
                CommonGUIFunctions.DrawBox(AlgebraGUI, CommonGUIFunctions.GreenTex);

            }
            else
            {
                CommonGUIFunctions.DrawBox(AlgebraGUI, CommonGUIFunctions.RedTex);

            }
            currentY += 5;
            float current_X = TabRect.x + 5;
            float LeafWidth = 100;
            float AlgebraWidth = 50;
            float elementHight = 20;

            AlgebraGUI.x += 2;
            AlgebraGUI.y += 2;
            AlgebraGUI.width -= 4;
            AlgebraGUI.height -= 4;

            var ScrollInternal = AlgebraGUI;
            ScrollInternal.height -= 25;

            ScrollInternal.width = logicSignal.TreeUserInfixExp.Count() * (LeafWidth + 10);


            Widgets.BeginScrollView(AlgebraGUI, ref scrollPos_AdvancedAlgebraEditor, ScrollInternal);

            currentY += 15;

            var ButtonRect = new Rect(current_X, currentY, LeafWidth, elementHight);






            foreach (Tree_node node in logicSignal.TreeUserInfixExp)
            {
                if (node.Algebra == EnumBinaryAlgebra.bNA)
                {
                    List<FloatMenuOption> floatMenuOptions_leaf_Logics = this_Controller.leaf_Logics
                     .Where(l => l.Visible)
                     .Select(g => new FloatMenuOption(g.Name, () => { logicSignal.TreeUserInfixExp[logicSignal.TreeUserInfixExp.IndexOf(node)].Leaf_Logic_ref = g; }))
                     .ToList();

                    floatMenuOptions_leaf_Logics.Insert(0, new FloatMenuOption("[Remove]", () =>
                    {
                        int index = logicSignal.TreeUserInfixExp.IndexOf(node);
                        if (logicSignal.TreeUserInfixExp.Count == index + 1)
                        {
                            //Is Last one
                            logicSignal.TreeUserInfixExp.RemoveAt(logicSignal.TreeUserInfixExp.IndexOf(node));
                        }
                        else
                        {
                            //Not last --> Remove all after this one
                            logicSignal.TreeUserInfixExp.RemoveRange(index, logicSignal.TreeUserInfixExp.Count - index);
                        }



                    }));
                    ButtonRect.width = LeafWidth;
                    LeafAlgebra_Advanced_Button(ref ButtonRect, floatMenuOptions_leaf_Logics, node.Leaf_Logic_ref.Name);
                }
                else
                {
                    List<FloatMenuOption> floatMenuOptions_Operators = new List<FloatMenuOption>();
                    floatMenuOptions_Operators.Add(new FloatMenuOption("∧", () => { node.Algebra = EnumBinaryAlgebra.bAND; }));
                    floatMenuOptions_Operators.Add(new FloatMenuOption("∨", () => { node.Algebra = EnumBinaryAlgebra.bOR; }));
                    // floatMenuOptions_Operators.Add(new FloatMenuOption("(", () => { node.Algebra = EnumBinaryAlgebra.bBracketOpen; }));
                    // floatMenuOptions_Operators.Add(new FloatMenuOption(")", () => { node.Algebra = EnumBinaryAlgebra.bBracketClose; }));
                    floatMenuOptions_Operators.Insert(0, new FloatMenuOption("[Remove]", () =>
                    {
                        int index = logicSignal.TreeUserInfixExp.IndexOf(node);
                        if (logicSignal.TreeUserInfixExp.Count == index + 1)
                        {
                            //Is Last one
                            logicSignal.TreeUserInfixExp.RemoveAt(logicSignal.TreeUserInfixExp.IndexOf(node));
                        }
                        else
                        {
                            //Not last --> Remove all after this one
                            logicSignal.TreeUserInfixExp.RemoveRange(index, logicSignal.TreeUserInfixExp.Count - index);
                        }



                    }));
                    ButtonRect.width = AlgebraWidth;
                    LeafAlgebra_Advanced_Button(ref ButtonRect, floatMenuOptions_Operators, EnumBinaryAlgebra_toString(node.Algebra));

                }



            }

            //Add Option to Expand Expression
            if (logicSignal.TreeUserInfixExp.Count > 0 && (logicSignal.TreeUserInfixExp.Last().Algebra == EnumBinaryAlgebra.bNA || logicSignal.TreeUserInfixExp.Last().Algebra == EnumBinaryAlgebra.bBracketClose))
            {
                //Add Option for Algebra
                List<FloatMenuOption> floatMenuOptions_Operators = new List<FloatMenuOption>();
                floatMenuOptions_Operators.Add(new FloatMenuOption("∧", () => { logicSignal.TreeUserInfixExp.Add(new Tree_node(EnumBinaryAlgebra.bAND, null)); }));
                floatMenuOptions_Operators.Add(new FloatMenuOption("∨", () => { logicSignal.TreeUserInfixExp.Add(new Tree_node(EnumBinaryAlgebra.bOR, null)); }));
                //  floatMenuOptions_Operators.Add(new FloatMenuOption("(", () => {  logicSignal.TreeUserInfixExp.Add(new Tree_node(EnumBinaryAlgebra.bBracketOpen, null)); }));
                //  floatMenuOptions_Operators.Add(new FloatMenuOption(")", () => {  logicSignal.TreeUserInfixExp.Add(new Tree_node(EnumBinaryAlgebra.bBracketClose, null)); }));
                ButtonRect.width = AlgebraWidth;
                LeafAlgebra_Advanced_Button(ref ButtonRect, floatMenuOptions_Operators, "[Add]");
            }
            else
            {
                //Add Option for Value
                List<FloatMenuOption> floatMenuOptions_leaf_Logics = this_Controller.leaf_Logics
                     .Where(l => l.Visible)
                     .Select(g => new FloatMenuOption(g.Name, () => { logicSignal.TreeUserInfixExp.Add(new Tree_node(EnumBinaryAlgebra.bNA, g)); }))
                     .ToList();
                ButtonRect.width = LeafWidth;
                LeafAlgebra_Advanced_Button(ref ButtonRect, floatMenuOptions_leaf_Logics, "[Add]");

            }

            Widgets.EndScrollView();
            currentY += 57;
        }



        Dragable_LogicBlock block = null;
        Dragable_LogicBlock block2 = null;
        Dragable_LogicBlock block3 = null;


        private void DragShapeEditor(ref float currentY, Rect TabRect, LogicSignal logicSignal)
        {


            if (GUI_HitBox_Register.Boundary.width == 0) GUI_HitBox_Register.Boundary = TabRect;
            if (block == null) block = new GUI_And_Block(new Vector2(TabRect.x, currentY), new Vector2(50, 50));
            if (block2 == null) block2 = new GUI_Or_Block(new Vector2(TabRect.x + 100, currentY), new Vector2(50, 50));
            if (block3 == null) block3 = new GUI_Not_Block(new Vector2(TabRect.x + 200, currentY), new Vector2(50, 50));

            block3.HandleElement();
            block2.HandleElement();
            block.HandleElement();
            //   var test = new Rect(TabRect.x, currentY, 50, 50);

            // Widgets.ButtonImage(test, RS.Algebra_And);

            // Widgets.ButtonImageDraggable(test, RS.Algebra_And);

        }


        //Maybe move this to a lib / Seperate Class
        private void ListBox<T>(float posX, ref float currentY, ref Vector2 scrollPos, ref int SelectedIndex, int selectedRef, List<T> inputList, Func<T, Rect, int, bool, int> func)
        {
            float listBoxWidth = 250;
            float itemHight = 30;
            float itemSeperation = 0;
            var ListBox_Outside = new Rect(posX, currentY, listBoxWidth, 150);

            Widgets.DrawMenuSection(ListBox_Outside);

            var ListBox_Inside = ListBox_Outside;
            ListBox_Inside.width -= 20;

            //TODO
            ListBox_Inside.height = (itemHight + itemSeperation) * inputList.Count();


            Widgets.BeginScrollView(ListBox_Outside, ref scrollPos, ListBox_Inside);

            float currY_Scroll = 60;
            var ValueRefItemRect = new Rect(ListBox_Inside.x + 5, currY_Scroll, ListBox_Inside.width, itemHight);
            int selectTemp = -1;

            for (int i = 0; i < inputList.Count(); i++)
            {
                ValueRefItemRect.y = currY_Scroll;
                selectTemp = func(inputList[i], ValueRefItemRect, i, selectedRef == i);
                if (selectTemp != -1)
                {
                    SelectedIndex = selectTemp;
                }
                currY_Scroll += itemHight + itemSeperation;
            }

            Widgets.EndScrollView();
        }



        int selsecteditem = -1;
        int selsecteditemleaf = -1;
        int selsecteditemLogicSignal = -1;


        Vector2 dragPos = new Vector2(0, 0);
        Vector2 dragboxSize = new Vector2(50, 50);


        protected override void FillTab()
        {
            var frame = new Rect(10f, 10f, size.x - 10f, size.y - 50f);
            frame.yMin += 32f;
            TabDrawer.DrawTabs(frame, tabs);
            float buttonWidth = 150;


            var innerFrame = frame;
            innerFrame.x += 10;
            innerFrame.y += 10;


            float currentY = innerFrame.y;
            float listBoxWidth = 250;
            float LeftHalveX = innerFrame.x + listBoxWidth + 20;


            if (currentTab == 0) //Is Algebra Tab
            {
                ListBox(innerFrame.x, ref currentY, ref scrollPos_LogicSignal, ref selsecteditemLogicSignal, selsecteditemLogicSignal, this_Controller.LogicSignals, delegate (LogicSignal r, Rect c, int e, bool f) { return LogicSignalListItem(r, c, e, f); });


                //Right Hand Buttons
                var buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Add Logic Signal"))
                {
                    this_Controller.LogicSignals.Add(new LogicSignal(new Tree(new List<Tree_node> { new Tree_node(EnumBinaryAlgebra.bNA, this_Controller.leaf_Logics[0]) }), "Logic Signal"));
                    this_Controller.UpdateRegisteredSignals();
                }
                currentY += 20;

                currentY += 60;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Remove Selected"))
                {
                    //TODO Maybe add a confirmation Box
                    if (selsecteditemLogicSignal != -1)
                    {
                        this_Controller.LogicSignals.RemoveAt(selsecteditemLogicSignal);
                        selsecteditemLogicSignal = -1;
                        this_Controller.UpdateRegisteredSignals();
                    }
                }
                currentY += 20;
                currentY += 70; // For the ListBox


                //Edit UI
                Widgets.DrawLineHorizontal(0, currentY, size.y);
                currentY += 20;

                if (selsecteditemLogicSignal != -1)
                {
                    LogicSignal selectedSignal = this_Controller.LogicSignals[selsecteditemLogicSignal];

                    var EiditRect = new Rect(innerFrame.x + 10 - 100, currentY, 300, 20);


                    selectedSignal.Name = Widgets.TextEntryLabeled(EiditRect, "Name", selectedSignal.Name);
                    bool toggle = selectedSignal.AvailableCrossMap;
                    EiditRect.x += EiditRect.width + 30;
                    Widgets.CheckboxLabeled(EiditRect, "Availibale Cross Map", ref toggle);
                    selectedSignal.AvailableCrossMap = toggle;


                    currentY += 30;


                    AlgebraGUI_Advanced(ref currentY, innerFrame, selectedSignal);

                    //TODO finish the GUI drag Interface and add it later
                    //currentY += 30;
                    //var GUIRect = new Rect( innerFrame.x + 10, currentY, 600, 300);
                    //DragShapeEditor(ref currentY, GUIRect, selectedSignal);

                }
            }
            else if (currentTab == 1) //Is Leaf Logic Tab
            {
                ListBox(innerFrame.x, ref currentY, ref scrollPos_LeafLogic, ref selsecteditemleaf, selsecteditemleaf, this_Controller.leaf_Logics, delegate (Leaf_Logic r, Rect c, int e, bool f) { return LeafLogicListItem(r, c, e, f); });


                //Right Hand Buttons
                var buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Add Leaf Logic"))
                {
                    List<ValueRefrence> dummys = this_Controller.valueRefrences.Where(vr => vr.Visible == false).ToList();
                    this_Controller.leaf_Logics.Add(new Leaf_Logic(dummys[0], dummys[1], EnumCompareOperator.Equal));

                }
                currentY += 20;

                currentY += 60;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Remove Selected"))
                {
                    //TODO Maybe add a confirmation Box
                    if (selsecteditemleaf != -1)
                    {
                        this_Controller.leaf_Logics.RemoveAt(selsecteditemleaf);
                        selsecteditemleaf = -1;
                    }
                }
                currentY += 20;
                currentY += 70; // For the ListBox


                //Edit UI
                Widgets.DrawLineHorizontal(0, currentY, size.y);
                currentY += 20;

                if (selsecteditemleaf != -1)
                {
                    Leaf_Logic selectedItem = this_Controller.leaf_Logics[selsecteditemleaf];

                    var EiditRect = new Rect(innerFrame.x + 10 - 100, currentY, 300, 20);
                    selectedItem.Name = Widgets.TextEntryLabeled(EiditRect, "Name", selectedItem.Name);

                    currentY += 20;
                    currentY += 20;

                    List<FloatMenuOption> floatMenuOptions_ref1 = this_Controller.valueRefrences
                        .Where(g => g.Visible)
                        .Select(g => new FloatMenuOption(g.Name, () => selectedItem.Value1 = g))
                        .ToList();
                    var DropdownRect = new Rect(innerFrame.x + 10, currentY, buttonWidth, 20);
                    if (Widgets.ButtonText(DropdownRect, selectedItem.Value1.Name))
                    {
                        Find.WindowStack.Add(new FloatMenu(floatMenuOptions_ref1));
                    }


                    List<FloatMenuOption> floatMenuOptions_Compare = new List<FloatMenuOption>();
                    floatMenuOptions_Compare.Add(new FloatMenuOption("==", () => selectedItem.Operator = EnumCompareOperator.Equal));
                    floatMenuOptions_Compare.Add(new FloatMenuOption(">", () => selectedItem.Operator = EnumCompareOperator.Greater));
                    floatMenuOptions_Compare.Add(new FloatMenuOption(">=", () => selectedItem.Operator = EnumCompareOperator.GreaterEqual));
                    floatMenuOptions_Compare.Add(new FloatMenuOption("!=", () => selectedItem.Operator = EnumCompareOperator.NotEqual));
                    floatMenuOptions_Compare.Add(new FloatMenuOption("<", () => selectedItem.Operator = EnumCompareOperator.Smaller));
                    floatMenuOptions_Compare.Add(new FloatMenuOption("<=", () => selectedItem.Operator = EnumCompareOperator.SmallerEqual));
                    DropdownRect.x += DropdownRect.width + 30;
                    if (Widgets.ButtonText(DropdownRect, EnumCompareOperator_ToString(selectedItem.Operator)))
                    {
                        Find.WindowStack.Add(new FloatMenu(floatMenuOptions_Compare));
                    }

                    List<FloatMenuOption> floatMenuOptions_ref2 = this_Controller.valueRefrences
                        .Where(g => g.Visible)
                        .Select(g => new FloatMenuOption(g.Name, () => selectedItem.Value2 = g))
                        .ToList();
                    DropdownRect.x += DropdownRect.width + 30;
                    if (Widgets.ButtonText(DropdownRect, selectedItem.Value2.Name))
                    {
                        Find.WindowStack.Add(new FloatMenu(floatMenuOptions_ref2));
                    }
                }
            }
            else if (currentTab == 2) //is Value Tab 
            {
                ListBox(innerFrame.x, ref currentY, ref scrollPos_ValueList, ref selsecteditem, selsecteditem, this_Controller.valueRefrences, delegate (ValueRefrence r, Rect c, int e, bool f) { return ValueRefListItem(r, c, e, f); });


                //Right Hand Buttons
                var buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Add Fixed"))
                {
                    this_Controller.valueRefrences.Add(new ValueRefrence_Fixed(0));

                }
                currentY += 20;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Add Item Rrfrence"))
                {
                    this_Controller.valueRefrences.Add(new ValueRefrence_ThingCount(new ThingFilter(), new StorageLocation(), this_Controller.Map));
                }
                currentY += 20;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Add Signal", active: this_Controller.LogicSignals.Count > 0))
                {
                    List<FloatMenuOption> AddSignalFloat = Current.Game.GetComponent<PRFGameComponent>().LoigSignalRegestry.Where(e => e.Key.AvailableCrossMap || e.Value == this.SelThing.Map).Select(e => e.Key)
                     .Select(s => new FloatMenuOption(s.Name, () => { this_Controller.valueRefrences.Add(new ValueRefrence_Signal(s)); }))
                     .ToList();
                    Find.WindowStack.Add(new FloatMenu(AddSignalFloat));
                }
                currentY += 20;

                currentY += 20;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Remove Selected"))
                {
                    //TODO Maybe add a confirmation Box
                    if (selsecteditem != -1)
                    {
                        this_Controller.valueRefrences.RemoveAt(selsecteditem);
                        selsecteditem = -1;
                    }
                }

                currentY += 20;
                currentY += 70; // For the ListBox


                //Edit UI
                Widgets.DrawLineHorizontal(0, currentY, size.y);
                currentY += 20;

                if (selsecteditem != -1)
                {

                    ValueRefrence selectedItem = this_Controller.valueRefrences[selsecteditem];

                    var EiditRect = new Rect(innerFrame.x + 10 - 100, currentY, 300, 20);
                    if (selectedItem is ValueRefrence_Signal)
                    {
                        Widgets.TextEntryLabeled(EiditRect, "Name", selectedItem.Name);
                    }
                    else
                    {
                        selectedItem.Name = Widgets.TextEntryLabeled(EiditRect, "Name", selectedItem.Name);
                    }


                    if (selectedItem is ValueRefrence_Fixed)
                    {
                        currentY += 20;
                        EiditRect = new Rect(innerFrame.x + 10, currentY, buttonWidth, 20);
                        string bufferstr = "" + selectedItem.GetValue(null, null);
                        int refval = selectedItem.GetValue(null, null);
                        //Can't pass getter by ref :(
                        Widgets.TextFieldNumericLabeled<int>(EiditRect, "Number", ref refval, ref bufferstr);
                        selectedItem.Value = refval;
                    }

                    if (selectedItem is ValueRefrence_ThingCount)
                    {
                        ValueRefrence_ThingCount selectedItem_tc = selectedItem as ValueRefrence_ThingCount;
                        //Thing Filter
                        currentY += 20;
                        EiditRect = new Rect(innerFrame.x + 10, currentY, 200, 300);


                        ThingFilterUI.DoThingFilterConfigWindow(EiditRect, ref scrollPos_itemFilter, ((ValueRefrence_ThingCount)selectedItem).filter);



                        //Add the Zone Selection
                        var ZoneButtonRect = new Rect(EiditRect.x + EiditRect.width + 20, currentY, 200, 25);

                        List<FloatMenuOption> floatMenuOptions = Find.CurrentMap.haulDestinationManager.AllGroups.ToList()
                                .Select(g => new FloatMenuOption(g.parent.SlotYielderLabel(), () => { selectedItem_tc.storage.SlotGroup = g; selectedItem_tc.dynamicSlot = EnumDynamicSlotGroupID.NA; }))
                                .ToList();
                        floatMenuOptions.Insert(0, new FloatMenuOption("Entire Map", () => { selectedItem_tc.storage.SlotGroup = null; selectedItem_tc.dynamicSlot = EnumDynamicSlotGroupID.NA; }));
                        floatMenuOptions.Insert(1, new FloatMenuOption("(Input Cell)", () => { selectedItem_tc.dynamicSlot = EnumDynamicSlotGroupID.Group_1; selectedItem_tc.storage.SlotGroup = null; }));
                        floatMenuOptions.Insert(2, new FloatMenuOption("(Output Cell)", () => { selectedItem_tc.dynamicSlot = EnumDynamicSlotGroupID.Group_2; selectedItem_tc.storage.SlotGroup = null; }));


                        if (Widgets.ButtonText(ZoneButtonRect, selectedItem_tc.storage.GetLocationName()))
                        {
                            Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                        }
                    }
                }
            }
            else
            {
                Log.Error("PRF ITab_LogicController Tab Selection Error");
            }

        }

        private Vector2 winSize = new Vector2(800f, 600f);

        protected override void UpdateSize()
        {

            this.size = winSize;

            base.UpdateSize();



        }

        int currentTab = 0;


        public override void OnOpen()
        {
            base.OnOpen();
            tabs.Clear();
            tabs.Add(new TabRecord("Algebra", delegate
            {
                currentTab = 0;
            }, () => currentTab == 0));
            tabs.Add(new TabRecord("Leaf Logic", delegate
            {
                currentTab = 1;
            }, () => currentTab == 1));
            tabs.Add(new TabRecord("Values", delegate
            {
                currentTab = 2;
            }, () => currentTab == 2));


        }



        public ITab_LogicController()
        {
            this.labelKey = "Logic_GUI";
        }

    }
}
