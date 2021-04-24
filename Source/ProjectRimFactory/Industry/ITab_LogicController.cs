using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;


namespace ProjectRimFactory.Industry
{
    class ITab_LogicController : ITab
    {

        private LogicController this_Controller { get => this.SelThing as LogicController; }

        public override bool IsVisible => base.IsVisible;

        ThingFilter dummyfilter = new ThingFilter();

        private List<TabRecord> tabs = new List<TabRecord>();


        Vector2 scrollPos_itemFilter = new Vector2();

        protected override void FillTab()
        {
            //this_Controller.LogicSignals


            int FixedValue = 0;


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


            if (currentTab == 0) //Is Algebra Tab
            {

            }
            else if (currentTab == 1) //Is Leaf Logic Tab
            {

            }
            else if (currentTab == 2) //is Value Tab 
            {
                
                


                //Left Hand Buttons
                float LeftHalveX = innerFrame.x + 200;
                
                var buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                Widgets.ButtonText(buttonrect, "Add Fixed");
                currentY += 20;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                Widgets.ButtonText(buttonrect, "Add Item Rrfrence");
                currentY += 20;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                Widgets.ButtonText(buttonrect, "Add Signal");
                currentY += 20;
                
                
                currentY += 20;
                buttonrect = new Rect(LeftHalveX, currentY, buttonWidth, 20);
                Widgets.ButtonText(buttonrect, "Remove Selected");

                currentY += 20;

                //Edit UI
                Widgets.DrawLineHorizontal(0, currentY, size.y);
                currentY += 20;

                var EiditRect = new Rect(innerFrame.x + 10, currentY, buttonWidth, 20);
                Widgets.TextEntryLabeled(EiditRect, "Name", "TODO");

                currentY += 20;
                EiditRect = new Rect(innerFrame.x + 10, currentY, buttonWidth, 20);
                string bufferstr = "";
                Widgets.TextFieldNumericLabeled<int>(EiditRect, "Number", ref FixedValue, ref bufferstr);

                //Thing Filter
                currentY += 20;
                EiditRect = new Rect(innerFrame.x + 10, currentY, buttonWidth, 200);


                ThingFilterUI.DoThingFilterConfigWindow(EiditRect, ref  scrollPos_itemFilter, dummyfilter);





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
