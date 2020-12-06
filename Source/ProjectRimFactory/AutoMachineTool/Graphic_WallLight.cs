using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Graphic_WallLight : Graphic_Link2<Graphic_WallLight>
    {
        public override bool ShouldDrawRotated => data == null || data.drawRotated;

        public override bool ShouldLinkWith(Rot4 dir, Thing parent)
        {
            var c = parent.Position + dir.FacingCell;
            if (!c.InBounds(parent.Map)) return false;

            return IsWall(c, parent.Map);
        }

        private bool IsWall(IntVec3 pos, Map map)
        {
            return !pos.GetThingList(map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t.def.building != null)
                .Where(t => !t.def.building.isNaturalRock)
                .Any(t => t.def.passability == Traversability.Impassable);
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            var num = 0;
            if (thingDef.PlaceWorkers.All(
                p => p.AllowsPlacing(thingDef, loc.ToIntVec3(), rot, Find.CurrentMap).Accepted))
            {
                var num2 = 1;
                for (var i = 0; i < 4; i++)
                {
                    var c = loc.ToIntVec3() + GenAdj.CardinalDirections[i];
                    if (IsWall(c, Find.CurrentMap)) num += num2;
                    num2 *= 2;
                }
            }

            var linkSet = (LinkDirections) num;
            var material = subMats[(int) linkSet];
            material.shader = ShaderDatabase.Transparent;

            var mesh = MeshAt(rot);
            var quaternion = QuatFromRot(rot);
            if (extraRotation != 0f) quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
            Graphics.DrawMesh(mesh, loc, quaternion, material, 0);
            if (ShadowGraphic != null) ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }
    }
}