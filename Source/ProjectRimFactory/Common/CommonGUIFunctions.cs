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


namespace ProjectRimFactory.Common
{
    static class CommonGUIFunctions
    {

		//Adaption of "Verse.Widgets.Label(Rect rect, string label)" To expose GUIStyle
		//This enabel the control over the Text Style
		public static void Label(Rect rect, string label,GUIStyle gUIStyle)
		{

			Rect val = rect;
			float num = Prefs.UIScale / 2f;
			if (Prefs.UIScale > 1f && Math.Abs(num - Mathf.Floor(num)) > float.Epsilon)
			{
				val.xMin = Widgets.AdjustCoordToUIScalingFloor(rect.xMin);
				val.xMax = Widgets.AdjustCoordToUIScalingFloor(rect.xMax);
				val.yMin = Widgets.AdjustCoordToUIScalingFloor(rect.yMin);
				val.yMax = Widgets.AdjustCoordToUIScalingFloor(rect.yMax);
			}
			GUI.Label(val, label, gUIStyle);
		}



	}
}
