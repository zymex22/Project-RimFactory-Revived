using System;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    internal class Command_ActionRightLeft : Command
    {

        private static bool wasRightClick;

        public Action ActionL;
        public Action ActionR;
        
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (wasRightClick)
            {
                ActionR();
            }
            else
            {
                ActionL();
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

    }
}
