﻿using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones
{
    internal interface IDroneSeetingsITab
    {
        Dictionary<WorkTypeDef, bool> GetWorkSettings { get; set; }

        List<SkillRecord> DroneSeetings_skillDefs { get; }

        string[] GetSleepTimeList { get; }

        CompRefuelable compRefuelable { get; }
    }

    public class ITab_DroneStation_Def : IPRF_SettingsContent
    {
        private readonly object caller;


        public ITab_DroneStation_Def(object callero)
        {
            caller = callero;
        }

        private static GUIStyle richTextStyle
        {
            get
            {
                var gtter_richTextStyle = new GUIStyle();
                gtter_richTextStyle.richText = true;
                gtter_richTextStyle.normal.textColor = Color.white;
                return gtter_richTextStyle;
            }
        }

        private IDroneSeetingsITab droneStation => caller as IDroneSeetingsITab;

        public float ITab_Settings_Minimum_x => 400;


        public float ITab_Settings_Additional_y
        {
            get
            {
                float additionalHeight = 30 * droneStation.GetWorkSettings.Count + 70 + 30;
                if (droneStation.GetSleepTimeList[0] != "") additionalHeight += 70;
                if (droneStation.compRefuelable != null) additionalHeight += 70;
                return additionalHeight;
            }
        }

        public Listing_Standard ITab_Settings_AppendContent(Listing_Standard list, Rect parrent_rect)
        {
            var rect = new Rect();

            rect = list.GetRect(30f);

            var rect3 = rect;
            rect3.y -= 17;
            Widgets.Label(rect3, "ITab_DroneStation_HeaderLabel".Translate());

            //Add Lable Explayning the pannel
            Widgets.Label(rect, "ITab_DroneStation_InfoLabel".Translate());
            rect = list.GetRect(30f);
            Widgets.DrawLineHorizontal(rect.x, rect.y, 400);

            foreach (var def in droneStation.GetWorkSettings.Keys.ToList())
                droneStation.GetWorkSettings[def] = CheckboxHelper(rect, list, droneStation.GetWorkSettings[def], def);


            //Add The Sleep Times Overview
            //If There are Sleep Times configured
            if (droneStation.GetSleepTimeList[0] != "")
            {
                rect = list.GetRect(30f);
                Widgets.DrawLineHorizontal(rect.x, rect.y, 400);

                CommonGUIFunctions.Label(rect, "ITab_DroneStation_Sleeptimes".Translate(), richTextStyle);
                rect = list.GetRect(30f);
                // droneInterface.GetSleepTimeList
                var txt = "";
                for (var i = 0; i < 24; i++)
                    if (droneStation.GetSleepTimeList.Contains(i.ToString()))
                        txt += "<color=red><b>" + i + "</b></color> ";
                    else
                        txt += i + " ";
                CommonGUIFunctions.Label(rect, txt, richTextStyle);
            }


            //Add the fule display if existing
            if (droneStation.compRefuelable != null)
            {
                rect = list.GetRect(30f);
                Widgets.DrawLineHorizontal(rect.x, rect.y, 400);

                CommonGUIFunctions.Label(rect, "ITab_DroneStation_SetTargetFuel".Translate(), richTextStyle);
                rect = list.GetRect(30f);
                list.Gap();
                droneStation.compRefuelable.TargetFuelLevel = Widgets.HorizontalSlider(rect,
                    droneStation.compRefuelable.TargetFuelLevel, 0, droneStation.compRefuelable.Props.fuelCapacity,
                    true, "SetTargetFuelLevel".Translate(droneStation.compRefuelable.TargetFuelLevel), "0",
                    droneStation.compRefuelable.Props.fuelCapacity.ToString(), 1);
            }


            return list;
        }


        //Small helper function to create each Checkbox as i cant pass variable directly
        private bool CheckboxHelper(Rect rect, Listing_Standard list, bool variable, WorkTypeDef def)
        {
            rect = list.GetRect(30f); //That seems to affect the text possition
            var lstatus = variable;
            Widgets.CheckboxLabeled(rect, def.labelShort, ref lstatus);
            var rect2 = rect;

            string labeltext = "ITab_DroneStation_averageskill".Translate();
            rect2.x = 400 - 10 * labeltext.Length;
            if (def.relevantSkills.Count > 0)
            {
                var medSkill = 0;
                foreach (var skill in droneStation.DroneSeetings_skillDefs)
                    if (def.relevantSkills.Contains(skill.def))
                        medSkill += skill.levelInt;
                rect2.y += 5;

                medSkill = medSkill / def.relevantSkills.Count;

                Widgets.Label(rect2, labeltext + medSkill);
            }
            else
            {
                Widgets.Label(rect2, "-");
            }

            return lstatus;
        }
    }
}