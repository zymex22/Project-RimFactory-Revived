using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class SectionLayer_UGConveyor : SectionLayer_Things
    {
        public SectionLayer_UGConveyor(Section section) : base(section)
        {
            requireAddToMapMesh = false;
            relevantChangeTypes = MapMeshFlag.Buildings;
        }

        public override void DrawLayer()
        {
            if (OverlayDrawHandler_UGConveyor.ShouldDraw) base.DrawLayer();
        }

        protected override void TakePrintFrom(Thing t)
        {
            if (t.Faction != null && t.Faction != Faction.OfPlayer) return;
            if (t is Building_BeltConveyor b && b.IsUnderground)
//TODO:            if(Building_BeltConveyor.IsBeltConveyorDef(t.def) && Building_BeltConveyor.IsUndergroundDef(t.def))
                t.Graphic.Print(this, t);
        }
    }

    public static class OverlayDrawHandler_UGConveyor
    {
        private static int lastDrawFrame;

        public static bool ShouldDraw => lastDrawFrame + 1 >= Time.frameCount;

        public static void DrawOverlayThisFrame()
        {
            lastDrawFrame = Time.frameCount;
        }
    }
}