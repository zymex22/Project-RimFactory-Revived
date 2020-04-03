using ProjectRimFactory.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Drones
{
    public class Building_DroneStationSpecialGraphics : Building_DroneStationRefuelable
    {
        public override void DrawDormantDrones()
        {
            if (DronesLeft > 0)
            {
                // Instead of drawing drone graphic on all occupied cells, it is only drawn on Position cell
                PRFDefOf.PRFDrone.graphic.DrawFromDef(Position.ToVector3ShiftedWithAltitude(AltitudeLayer.LayingPawn), default(Rot4), PRFDefOf.PRFDrone);
            }
        }
    }
}
