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


        private List<TabRecord> tabs = new List<TabRecord>();

        protected override void FillTab()
        {
            //this_Controller.LogicSignals


            //Somhow need to add a Edit UI Here
            //Welp
            var frame = new Rect(10f, 10f, size.x - 10f, size.y - 50f);
            frame.yMin += 32f;
            TabDrawer.DrawTabs(frame, tabs);


            if (currentTab == 0) //Is Algebra Tab
            {

            }
            else if (currentTab == 1) //Is Leaf Logic Tab
            {

            }
            else if (currentTab == 2) //is Value Tab 
            {

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
