using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class SectionLayer_UGConveyor : SectionLayer_Things
    {
        public SectionLayer_UGConveyor(Section section) : base(section)
		{
            this.requireAddToMapMesh = false;
            this.relevantChangeTypes = MapMeshFlag.Buildings;
        }

        public override void DrawLayer()
        {
            if (OverlayDrawHandler_UGConveyor.ShouldDraw)
            {
                base.DrawLayer();
            }
        }

        protected override void TakePrintFrom(Thing t)
        {
            if (t.Faction != null && t.Faction != Faction.OfPlayer)
            {
                return;
            }
            if (t is Building_BeltConveyor b && b.IsUnderground)
//TODO:            if(Building_BeltConveyor.IsBeltConveyorDef(t.def) && Building_BeltConveyor.IsUndergroundDef(t.def))
            {
                //TODO 1.3 Added flaot ExtraRotation. i set it to 0 but unsure if this is correct
                t.Graphic.Print(this, t , 0);
            }
        }
    }

    public static class OverlayDrawHandler_UGConveyor
    {
        private static int lastDrawFrame;

        public static bool ShouldDraw
        {
            get
            {
                return lastDrawFrame + 1 >= Time.frameCount;
            }
        }

        public static void DrawOverlayThisFrame()
        {
            lastDrawFrame = Time.frameCount;
        }
    }
}
