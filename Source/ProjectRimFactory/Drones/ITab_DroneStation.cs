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
        Dictionary<WorkTypeDef, bool> WorkSettings { get; set; }

        List<SkillRecord> DroneSeetings_skillDefs { get; }

    }

    class ITab_DroneStation : ITab
    {
        //This will need a lot of fine tuning...
        private static readonly float checkboxheight = 30; //30 seems great
        private static readonly float labelheight = 70; //70 seems good maybe a bit smaller would be perfect
        private static readonly float headerheight = 30;

        private static readonly float itabwidth = 400;

        private IDroneSeetingsITab droneInterface => (this.SelThing as IDroneSeetingsITab);

        private static readonly Vector2 WinSize = new Vector2(itabwidth, checkboxheight + labelheight + headerheight);

        public ITab_DroneStation()
        {
            this.size = WinSize;
            this.labelKey = "ITab_DroneStation_labelKey".Translate(); //Some issues with that....
        }

        public override void TabUpdate()
        {
            base.TabUpdate();
            //Calculate New hight based on Content
            float additionalHeight = (checkboxheight * droneInterface.WorkSettings.Count) + labelheight + headerheight;
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


            rect = list.GetRect(30f);
            //Add Lable Explayning the pannel
            Widgets.Label(rect, "ITab_DroneStation_InfoLabel".Translate());
            rect = list.GetRect(30f);
            Widgets.DrawLineHorizontal(rect.x, rect.y, itabwidth);

            foreach (WorkTypeDef def in droneInterface.WorkSettings.Keys.ToList())
            {
                droneInterface.WorkSettings[def] = CheckboxHelper(rect, list, droneInterface.WorkSettings[def], def);   
            }
            list.End();
        }

        private void AddskillLabel(WorkTypeDef def , Rect rect2)
        {
            if (def.relevantSkills.Count > 0)
            {
                int medSkill = 0;
                foreach (SkillRecord skill in droneInterface.DroneSeetings_skillDefs)
                {
                    if( def.relevantSkills.Contains(skill.def)){
                        medSkill += skill.levelInt;
                    }
                }
                rect2.y += 5;
                medSkill = medSkill / def.relevantSkills.Count;
                Widgets.Label(rect2, "AverageSkillLevel = " + medSkill);
            }
            
            
        }


        //Small helper function to create each Checkbox as i cant pass variable directly
        private bool CheckboxHelper(Rect rect, Listing_Standard list, bool variable, WorkTypeDef def)
        {
            rect = list.GetRect(30f); //That seems to affect the text possition
            bool lstatus = variable;
            Widgets.CheckboxLabeled(rect, def.labelShort, ref lstatus);
            Rect rect2 = rect;
            rect2.x = itabwidth - 200;
            AddskillLabel(def, rect2);
            return lstatus;
        }

    }
}
