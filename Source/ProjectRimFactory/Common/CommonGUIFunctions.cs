using RimWorld;
using System;
using UnityEngine;
using Verse;


namespace ProjectRimFactory.Common
{
    internal static class CommonGUIFunctions
    {

        //Adaption of "Verse.Widgets.Label(Rect rect, string label)" To expose GUIStyle
        //This enables the control over the Text Style
        public static void Label(Rect rect, string label, GUIStyle gUIStyle)
        {

            var val = rect;
            var num = Prefs.UIScale / 2f;
            if (Prefs.UIScale > 1f && Math.Abs(num - Mathf.Floor(num)) > float.Epsilon)
            {
                val.xMin = LudeonTK.UIScaling.AdjustCoordToUIScalingFloor(rect.xMin);
                val.xMax = LudeonTK.UIScaling.AdjustCoordToUIScalingFloor(rect.xMax);
                val.yMin = LudeonTK.UIScaling.AdjustCoordToUIScalingFloor(rect.yMin);
                val.yMax = LudeonTK.UIScaling.AdjustCoordToUIScalingFloor(rect.yMax);
            }
            GUI.Label(val, label, gUIStyle);
        }

    }
}
