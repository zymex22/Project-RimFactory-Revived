using ProjectRimFactory.Common;
using Verse;

namespace ProjectRimFactory.Drones
{
    public class Building_DroneStationSpecialGraphics : Building_DroneStationRefuelable
    {
        protected override void DrawDormantDrones()
        {
            if (DronesLeft > 0)
            {
                // Instead of drawing drone graphic on all occupied cells, it is only drawn on Position cell
                PRFDefOf.PRFDrone.graphic.DrawFromDef(Position.ToVector3ShiftedWithAltitude(AltitudeLayer.LayingPawn), default(Rot4), PRFDefOf.PRFDrone);
            }
        }
    }
}
