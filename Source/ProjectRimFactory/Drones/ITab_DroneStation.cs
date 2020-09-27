using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using ProjectRimFactory.AutoMachineTool;
using RimWorld.Planet;

namespace ProjectRimFactory.Drones
{

    interface IDroneSeetingsITab
    {
        //This List contains are Work Types for That station
        List<WorkTypeDef> WorkBaseList { get;}
        //Contains the respective allowed setting for WorkBaseList
        List<bool> WorkBaseListEnable { get; set; }

    }

    class ITab_DroneStation : ITab
    {
        //This will need a lot of fine tuning...
        private static readonly float checkboxheight = 30; //30 seems great
        private static readonly float labelheight = 70; //70 seems good maybe a bit smaller would be perfect

        private IDroneSeetingsITab droneInterface => (this.SelThing as IDroneSeetingsITab);

        private static readonly Vector2 WinSize = new Vector2(400f, checkboxheight + labelheight);

        public ITab_DroneStation()
        {
            this.size = WinSize;
            this.labelKey = "ITab_DroneStation_labelKey".Translate(); //Some issues with that....
        }

        public override void TabUpdate()
        {
            base.TabUpdate();
            //Calculate New hight based on Content
            float additionalHeight = (checkboxheight * droneInterface.WorkBaseList.Count) + labelheight;
            this.size = new Vector2(WinSize.x,  additionalHeight);
            this.UpdateSize();
        }

        protected override void FillTab()
        {
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);

            list.Begin(inRect);

            list.Gap();
            var rect = new Rect();

            //TODO Check if i can remove that
            if (droneInterface.WorkBaseListEnable.Count == 0)
            {
                for (int i = 0; i< droneInterface.WorkBaseList.Count; i++)
                {
                    droneInterface.WorkBaseListEnable.Add(true);
                }
            }
            rect = list.GetRect(30f);
            //Add Lable Explayning the pannel
            Widgets.Label(rect, "ITab_DroneStation_InfoLabel".Translate());

            //add a Checkbox for each WorkTypeDef of the Station
            foreach (WorkTypeDef def in droneInterface.WorkBaseList)
            {
                int defindex = droneInterface.WorkBaseList.IndexOf(def);
                droneInterface.WorkBaseListEnable[defindex] = CheckboxHelper(rect, list, droneInterface.WorkBaseListEnable[defindex], def.labelShort);
            }
            list.End();
        }

        //Small helper function to create each Checkbox as i cant pass variable directly
        private bool CheckboxHelper(Rect rect, Listing_Standard list, bool variable,string text)
        {
           rect = list.GetRect(30f); //That seems to affect the text possition
           bool lstatus = variable;
           Widgets.CheckboxLabeled(rect, text, ref lstatus);
           return lstatus;
        }

    }
}
