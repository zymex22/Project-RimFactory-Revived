using System;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    class Command_ActionRightLeft : Command
    {

        private static bool wasRightClick = false;

        public Action actionL;
        public Action actionR;

        private Color? iconDrawColorOverride;

        public override Color IconDrawColor => iconDrawColorOverride ?? base.IconDrawColor;

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (wasRightClick)
            {
                actionR();
            }
            else
            {
                actionL();
            }

        }

        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            base.DrawIcon(rect, buttonMat, parms);

            if (Input.GetMouseButtonDown(0))
            {
                wasRightClick = false;
            }
            if (Input.GetMouseButtonDown(1))
            {
                wasRightClick = true;
            }
        }

        public void SetColorOverride(Color color)
        {
            iconDrawColorOverride = color;
        }

    }
}
