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

    class ITab_LogicController : ITab 
    {

        private LogicController this_Controller { get => this.SelThing as LogicController; }

        public override bool IsVisible => base.IsVisible;

        ThingFilter dummyfilter = new ThingFilter();

        private List<TabRecord> tabs = new List<TabRecord>();


        Vector2 scrollPos_itemFilter = new Vector2();

        Vector2 scrollPos_ValueList = new Vector2();

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

        private int ValueRefListItem(ValueRefrence vr,Rect rect,int i , bool selected)
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
                vrString += "   " + vr.GetValue(null,null);
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
                case EnumCompareOperator.Equal:         return "==";
                case  EnumCompareOperator.Greater:      return ">";
                case  EnumCompareOperator.GreaterEqual: return ">=";
                case  EnumCompareOperator.NotEqual:     return "!=";
                case  EnumCompareOperator.Smaller:      return "<";
                case EnumCompareOperator.SmallerEqual:  return "<=";
                default: return "  ";
            }

        }


        /// <summary>
        /// This is the easier version for Users that are faliliar with Boolshe Algebra
        /// And It's Also "easier" to implement 
        /// </summary>
        /// <param name="currentY"></param>
        private void AlgebraGUI_Advanced(ref float currentY , Rect TabRect)
        {





        }


        int selsecteditem = -1;
        int selsecteditemleaf = -1;
        int selsecteditemLogicSignal = -1;


        Vector2 dragPos = new Vector2(0,0);
        Vector2 dragboxSize = new Vector2(50, 50);


        protected override void FillTab()
        {
            //this_Controller.LogicSignals


            //Somhow need to add a Edit UI Here
            //Welp
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
                /*
                //if (Input.GetMouseButton(0)) Log.Message(" Input.GetMouseButton(0)");


                //Input.get_mousePosition()
                if (dragPos.x == 0 && dragPos.y == 0) dragPos = new Vector2( innerFrame.x, currentY);

                var DragBoxTest = new Rect(dragPos, dragboxSize);

                Widgets.DrawBoxSolid(DragBoxTest, Color.red);

                if (Input.GetMouseButton(0) && Mouse.IsOver(DragBoxTest))
                {
                    //Log.Message(dragPos + "  -  " + Event.current.mousePosition);

                    dragPos =  Event.current.mousePosition;
                }
                */








                var ListBox_Outside = new Rect(innerFrame.x, currentY, listBoxWidth, 150);

                var ListBox_Inside = ListBox_Outside;

                ListBox_Inside.height *= 3;
                ListBox_Inside.width -= 20;

                //Left List Box

                Widgets.BeginScrollView(ListBox_Outside, ref scrollPos_ValueList, ListBox_Inside);




                float currY_Scroll = 60;
                var ValueRefItemRect = new Rect(ListBox_Inside.x, currY_Scroll, ListBox_Inside.width, 30);
                int selectTemp = -1;

                foreach (LogicSignal logicSignal in this_Controller.LogicSignals)
                {

                    ValueRefItemRect.y = currY_Scroll;
                    selectTemp = LogicSignalListItem(logicSignal, ValueRefItemRect, this_Controller.LogicSignals.IndexOf(logicSignal), selsecteditemleaf == this_Controller.LogicSignals.IndexOf(logicSignal));
                    if (selectTemp != -1)
                    {
                        selsecteditemLogicSignal = selectTemp;
                    }
                    currY_Scroll += 30;
                }
                //Log.Message("selsecteditemleaf: " + selsecteditemleaf);


                Widgets.EndScrollView();

                //Right Hand Buttons


                var buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Add Logic Signal"))
                {
                    this_Controller.LogicSignals.Add(new LogicSignal(new Tree(new List<Tree_node> { new Tree_node(EnumBinaryAlgebra.bNA, this_Controller.leaf_Logics[0]) }), "Logic Signal"));
                }
                currentY += 20;



                currentY += 60;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Remove Selected"))
                {
                    //TODO Maybe add a confirmation Box
                    if (selsecteditemleaf != -1)
                    {
                        this_Controller.LogicSignals.RemoveAt(selsecteditemleaf);
                        selsecteditemleaf = -1;
                    }


                }

                currentY += 20;

                currentY += 70; // For the ListBox

                //Edit UI
                Widgets.DrawLineHorizontal(0, currentY, size.y);
                currentY += 20;







                AlgebraGUI_Advanced(ref currentY, innerFrame);










            }
            else if (currentTab == 1) //Is Leaf Logic Tab
            {







                var ListBox_Outside = new Rect(innerFrame.x, currentY, listBoxWidth, 150);

                var ListBox_Inside = ListBox_Outside;

                ListBox_Inside.height *= 3;
                ListBox_Inside.width -= 20;

                //Left List Box

                Widgets.BeginScrollView(ListBox_Outside, ref scrollPos_ValueList, ListBox_Inside);




                float currY_Scroll = 60;
                var ValueRefItemRect = new Rect(ListBox_Inside.x, currY_Scroll, ListBox_Inside.width, 30);
                int selectTemp = -1;

                foreach (Leaf_Logic leaf in this_Controller.leaf_Logics)
                {
                    if (!leaf.Visible) continue; //Skipp Dummy
                    ValueRefItemRect.y = currY_Scroll;
                    selectTemp = LeafLogicListItem(leaf, ValueRefItemRect, this_Controller.leaf_Logics.IndexOf(leaf), selsecteditemleaf == this_Controller.leaf_Logics.IndexOf(leaf));
                    if (selectTemp != -1)
                    {
                        selsecteditemleaf = selectTemp;
                    }
                    currY_Scroll += 30;
                }
                //Log.Message("selsecteditemleaf: " + selsecteditemleaf);


                Widgets.EndScrollView();

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
                        .Where(g => g.Visible )
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

               
               


                var ListBox_Outside = new Rect(innerFrame.x, currentY, listBoxWidth, 150);

                var ListBox_Inside =  ListBox_Outside;

                ListBox_Inside.height *= 3;
                ListBox_Inside.width -= 20;

                //Left List Box
                
                Widgets.BeginScrollView(ListBox_Outside, ref scrollPos_ValueList, ListBox_Inside);

                


                float currY_Scroll = 60 ;
                var ValueRefItemRect = new Rect(ListBox_Inside.x, currY_Scroll, ListBox_Inside.width, 30);
                int selectTemp = -1;

                foreach (ValueRefrence vr in this_Controller.valueRefrences)
                {
                    if (!vr.Visible) continue; //Skip Dummy
                    ValueRefItemRect.y = currY_Scroll;
                    selectTemp = ValueRefListItem(vr, ValueRefItemRect, this_Controller.valueRefrences.IndexOf(vr), selsecteditem == this_Controller.valueRefrences.IndexOf(vr));
                    if (selectTemp != -1)
                    {
                        selsecteditem = selectTemp;
                    }
                    currY_Scroll += 30;
                }
                //Log.Message("selsecteditem: " + selsecteditem);
                

                Widgets.EndScrollView();

                //Right Hand Buttons


                var buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if ( Widgets.ButtonText(buttonrect, "Add Fixed"))
                {
                    this_Controller.valueRefrences.Add(new ValueRefrence_Fixed());

                }
                currentY += 20;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if (Widgets.ButtonText(buttonrect, "Add Item Rrfrence"))
                {
                    this_Controller.valueRefrences.Add(new ValueRefrence_ThingCount(new ThingFilter(),new StorageLocation(),this_Controller.Map));
                }
                currentY += 20;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                if(Widgets.ButtonText(buttonrect, "Add Signal"))
                {
                    //TODO Once i have Signals
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
                        string bufferstr = "" + selectedItem.GetValue(null,null);
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
                        floatMenuOptions.Insert(0, new FloatMenuOption("Entire Map",  () => { selectedItem_tc.storage.SlotGroup = null; selectedItem_tc.dynamicSlot = EnumDynamicSlotGroupID.NA; }));
                        floatMenuOptions.Insert(1, new FloatMenuOption("Dynamic - 1", () => { selectedItem_tc.dynamicSlot = EnumDynamicSlotGroupID.Group_1; selectedItem_tc.storage.SlotGroup = null; } ));
                        floatMenuOptions.Insert(2, new FloatMenuOption("Dynamic - 2", () => { selectedItem_tc.dynamicSlot = EnumDynamicSlotGroupID.Group_2; selectedItem_tc.storage.SlotGroup = null; } ));


                        if (Widgets.ButtonText(ZoneButtonRect, selectedItem_tc.storage.GetLocationName()))
                        {
                            Find.WindowStack.Add(new FloatMenu( floatMenuOptions));
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
