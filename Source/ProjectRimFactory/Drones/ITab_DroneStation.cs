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
        
        List<WorkTypeDef> WorkBaseList { get;}

        List<bool> WorkBaseListEnable { get; set; }

    }

    


    class ITab_DroneStation : ITab
    {
        //This will need a lot of fine tuning...
        private static readonly float checkboxhight = 60;

        //We need a check for each item of that list
        private IDroneSeetingsITab droneInterface => (this.SelThing as IDroneSeetingsITab);


        private static readonly Vector2 WinSize = new Vector2(400f, 130f);

        public ITab_DroneStation()
        {
            this.size = WinSize;
            this.labelKey = "ITab_DroneStation_labelKey".Translate(); //Some issues with that....
        }


        public override void TabUpdate()
        {
            base.TabUpdate();

            float additionalHeight = checkboxhight * droneInterface.WorkBaseList.Count;
            this.size = new Vector2(WinSize.x, Mathf.Max(  additionalHeight,140f));
            this.UpdateSize();
        }


        //  private bool status = false;

        //  private Dictionary<WorkTypeDef, bool> local_WorkSelectionList;

        // private bool test = false;

        protected override void FillTab()
        {
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);

            list.Begin(inRect);

            list.Gap();
            var rect = new Rect();

            //--------------------------------------


            //--------------

            if (droneInterface.WorkBaseListEnable.Count == 0)
            {
                for (int i = 0; i< droneInterface.WorkBaseList.Count; i++)
                {
                    droneInterface.WorkBaseListEnable.Add(true);
                }
            }
            rect = list.GetRect(30f);

            Widgets.Label(rect, "ITab_DroneStation_InfoLabel".Translate()); ;

            foreach (WorkTypeDef def in droneInterface.WorkBaseList)
            {
                int defindex = droneInterface.WorkBaseList.IndexOf(def);
                droneInterface.WorkBaseListEnable[defindex] = CheckboxHelper(rect, list, droneInterface.WorkBaseListEnable[defindex], def.labelShort);
            }
            list.End();
        }

        private bool CheckboxHelper(Rect rect, Listing_Standard list, bool variable,string text)
        {
            rect = list.GetRect(30f); //That seems to affect the text possition
           bool lstatus = variable;
            Widgets.CheckboxLabeled(rect, text, ref lstatus);
           return lstatus;
        }

    }
}
