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
using ProjectRimFactory.Common;

namespace ProjectRimFactory.Drones
{

    interface IDroneSeetingsITab
    {
        Dictionary<WorkTypeDef, bool> GetWorkSettings { get; set; }

        List<SkillRecord> DroneSeetings_skillDefs { get; }

        string[] GetSleepTimeList { get; }

        CompRefuelable compRefuelable { get; }

    }

    class ITab_DroneStation : ITab
    {
        //This will need a lot of fine tuning...
        private static readonly float checkboxheight = 30; //30 seems great
        private static readonly float labelheight = 70; //70 seems good maybe a bit smaller would be perfect
        private static readonly float headerheight = 30;

        private static readonly float itabwidth = 400; //400

        private IDroneSeetingsITab droneInterface => (this.SelThing as IDroneSeetingsITab);


        private static readonly Vector2 WinSize = new Vector2(itabwidth, checkboxheight + labelheight + headerheight);

        public ITab_DroneStation()
        {
            this.size = WinSize;
            //Do NOT add .Translate() to this.labelKey as this is already done automaticly.
            this.labelKey = "ITab_DroneStation_labelKey";
        }

        public override void TabUpdate()
        {
            base.TabUpdate();
            //Calculate New hight based on Content
            float additionalHeight = (checkboxheight * droneInterface.GetWorkSettings.Count) + labelheight + headerheight;
            if (droneInterface.GetSleepTimeList[0] != "")
            {
                additionalHeight += 50;
            }
            if (droneInterface.compRefuelable != null)
            {
                additionalHeight += 70;
            }
                this.size = new Vector2(WinSize.x,  additionalHeight);
            this.UpdateSize();
        }

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

        //TODO may need to add code that enabels a scroll bar should we ever need a station with that many WorkTypeDef(s). may also depend on resulution / UI Scale.
        protected override void FillTab()
        {
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);

            list.Begin(inRect);

            list.Gap();
            var rect = new Rect();
            
            rect = list.GetRect(30f);

            Rect rect3 = rect;
            rect3.y -= 17;
            Widgets.Label(rect3, "ITab_DroneStation_HeaderLabel".Translate());

            //Add Lable Explayning the pannel
            Widgets.Label(rect, "ITab_DroneStation_InfoLabel".Translate()) ;
            rect = list.GetRect(30f);
            Widgets.DrawLineHorizontal(rect.x, rect.y, itabwidth);

            foreach (WorkTypeDef def in droneInterface.GetWorkSettings.Keys.ToList())
            {
                droneInterface.GetWorkSettings[def] = CheckboxHelper(rect, list, droneInterface.GetWorkSettings[def], def);   
            }
            

            //Add The Sleep Times Overview
            //If There are Sleep Times configured
            if (droneInterface.GetSleepTimeList[0] != "")
            {
                rect = list.GetRect(30f);
                Widgets.DrawLineHorizontal(rect.x, rect.y, itabwidth);
               
                CommonGUIFunctions.Label(rect, "DroneStation Sleep Times (marked in red)", richTextStyle);
                AddSleepDisplay(rect, list);
            }
            

            //Add the fule display if existing
            if (droneInterface.compRefuelable != null)
            {
                rect = list.GetRect(30f);
                Widgets.DrawLineHorizontal(rect.x, rect.y, itabwidth);
                
                CommonGUIFunctions.Label(rect, "DroneStation FuleLevel", richTextStyle);
                rect = list.GetRect(30f);
                list.Gap();
                droneInterface.compRefuelable.TargetFuelLevel = Widgets.HorizontalSlider(rect, droneInterface.compRefuelable.TargetFuelLevel, 0, droneInterface.compRefuelable.Props.fuelCapacity, true, "Set target fuel Level:" + droneInterface.compRefuelable.TargetFuelLevel, null, null, 1);


            }



        list.End();
        }
        private void AddSleepDisplay(Rect rect, Listing_Standard list)
        {
            rect = list.GetRect(30f);
            // droneInterface.GetSleepTimeList
            string txt = "";
           for (int i = 0;i<= 24; i++)
           {
            if (droneInterface.GetSleepTimeList.Contains(i.ToString()))
                {
                    txt += "<color=red><b>" + i.ToString() + "</b></color> ";
                }
                else
                {
                    txt += i.ToString() + " ";
                }
           }
            CommonGUIFunctions.Label(rect, txt, richTextStyle);
        }


        private void AddskillLabel(WorkTypeDef def , Rect rect2)
        {
            string labeltext = "ITab_DroneStation_averageskill".Translate();
            rect2.x = itabwidth - (10 * labeltext.Length);
            if (def.relevantSkills.Count > 0)
            {
                int medSkill = 0;
                foreach (SkillRecord skill in droneInterface.DroneSeetings_skillDefs)
                {
                    if ( def.relevantSkills.Contains(skill.def))
                    {
                        medSkill += skill.levelInt;
                    }
                }
                rect2.y += 5;
                
                medSkill = medSkill / def.relevantSkills.Count;
                
                Widgets.Label(rect2, labeltext + medSkill);
                return;
            }
            Widgets.Label(rect2, "-");
        }


        //Small helper function to create each Checkbox as i cant pass variable directly
        private bool CheckboxHelper(Rect rect, Listing_Standard list, bool variable, WorkTypeDef def)
        {
            rect = list.GetRect(30f); //That seems to affect the text possition
            bool lstatus = variable;
            Widgets.CheckboxLabeled(rect, def.labelShort, ref lstatus);
            Rect rect2 = rect;
            
            AddskillLabel(def, rect2);
            return lstatus;
        }

    }
}
