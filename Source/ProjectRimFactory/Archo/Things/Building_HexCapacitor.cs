using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Archo.Things
{
    public class Building_HexCapacitor : Building
    {
        CompPowerBattery batteryComp;
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (Find.CameraDriver.CurrentZoom < CameraZoomRange.Middle)
            {
                GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(this, 0f), batteryComp.StoredEnergy.ToString("F0"), Color.white);
            }
        }
        public override void PostMake()
        {
            base.PostMake();
            batteryComp = GetComp<CompPowerBattery>();
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            batteryComp = GetComp<CompPowerBattery>();
        }
    }
}
