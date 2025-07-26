using ProjectRimFactory.Common;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones
{

    interface IDroneSeetingsITab
    {
        Dictionary<WorkTypeDef, bool> GetWorkSettings { get; set; }

        List<SkillRecord> DroneSettingsSkillDefs { get; }

        string[] GetSleepTimeList { get; }

        CompRefuelable CompRefuelable { get; }

    }

    public class ITab_DroneStation_Def : IPRF_SettingsContent
    {

        private static GUIStyle RichTextStyle
        {
            get
            {
                var textStyle = new GUIStyle
                {
                    richText = true,
                    normal =
                    {
                        textColor = Color.white
                    }
                };
                return textStyle;
            }
        }

        private readonly object caller;

        IDroneSeetingsITab droneStation => caller as IDroneSeetingsITab;


        public ITab_DroneStation_Def(object callerParam)
        {
            caller = callerParam;
        }

        public float ITab_Settings_Minimum_x => 400;


        public float ITab_Settings_Additional_y
        {
            get
            {
                float additionalHeight = (30 * droneStation.GetWorkSettings.Count) + 60 + 12;
                if (droneStation.GetSleepTimeList[0] != "")
                {
                    additionalHeight += 70;
                }
                if (droneStation.CompRefuelable != null)
                {
                    additionalHeight += 70;
                }
                return additionalHeight;

            }

        }



        //Small helper function to create each Checkbox as i cant pass variable directly
        private bool CheckboxHelper(Rect rect, Listing_Standard list, bool variable, WorkTypeDef def)
        {
            rect = list.GetRect(30f); //That seems to affect the text possition
            var lstatus = variable;
            Widgets.CheckboxLabeled(rect, def.labelShort, ref lstatus);
            var rect2 = rect;

            string labelText = "ITab_DroneStation_averageskill".Translate();
            rect2.x = 400 - (10 * labelText.Length);
            if (def.relevantSkills.Count > 0)
            {
                var medSkill = 0;
                foreach (var skill in droneStation.DroneSettingsSkillDefs)
                {
                    if (def.relevantSkills.Contains(skill.def))
                    {
                        medSkill += skill.levelInt;
                    }
                }
                rect2.y += 5;

                medSkill /= def.relevantSkills.Count;

                Widgets.Label(rect2, labelText + medSkill);
            }
            else
            {
                Widgets.Label(rect2, "-");
            }

            return lstatus;
        }

        public Listing_Standard ITab_Settings_AppendContent(Listing_Standard list, Rect parrentRect)
        {

            var rect = new Rect();

            list.Label("ITab_DroneStation_HeaderLabel".Translate());
            list.Label("ITab_DroneStation_InfoLabel".Translate());
            list.GapLine();

            foreach (var def in droneStation.GetWorkSettings.Keys.ToList())
            {
                droneStation.GetWorkSettings[def] = CheckboxHelper(rect, list, droneStation.GetWorkSettings[def], def);
            }

            //Add The Sleep Times Overview
            //If There are Sleep Times configured
            if (droneStation.GetSleepTimeList[0] != "")
            {
                list.GapLine();
                list.Label("ITab_DroneStation_Sleeptimes".Translate());

                rect = list.GetRect(30f);
                // droneInterface.GetSleepTimeList
                var txt = string.Empty;
                for (var i = 0; i < 24; i++)
                {
                    if (droneStation.GetSleepTimeList.Contains(i.ToString()))
                    {
                        txt += $"<color=red><b>{i}</b></color> ";
                    }
                    else
                    {
                        txt += $"{i} ";
                    }
                }
                CommonGUIFunctions.Label(rect, txt, RichTextStyle);
                
            }


            //Add the fuel display if existing
            if (droneStation.CompRefuelable == null) return list;
            list.GapLine();
            list.Label("ITab_DroneStation_SetTargetFuel".Translate());

            rect = list.GetRect(30f);
            list.Gap();
            droneStation.CompRefuelable.TargetFuelLevel = Widgets.HorizontalSlider(rect, droneStation.CompRefuelable.TargetFuelLevel, 0, droneStation.CompRefuelable.Props.fuelCapacity, true, "SetTargetFuelLevel".Translate(droneStation.CompRefuelable.TargetFuelLevel), "0", droneStation.CompRefuelable.Props.fuelCapacity.ToString(), 1);
            
            return list;
        }
    }

}
