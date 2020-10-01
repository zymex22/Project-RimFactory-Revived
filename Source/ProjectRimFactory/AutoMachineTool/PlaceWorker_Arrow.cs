using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class PlaceWorker_Arrow : PlaceWorker
    {
        public static readonly Material arrow = FadedMaterialPool.FadedVersionOf(MaterialPool.MatFrom(RS.Arrow), .6f);
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            base.DrawGhost(def, center, rot, ghostCol, thing);
            var pos = center.ToVector3Shifted();
            pos.y = AltitudeLayer.LightingOverlay.AltitudeFor();
            Graphics.DrawMesh(MeshPool.plane10, pos, rot.AsQuat, arrow, 0);
        }
    }
}
