using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Graphic_WallLight : Graphic_Link2<Graphic_WallLight>
    {
        public Graphic_WallLight() : base()
        {
        }

        public override bool ShouldLinkWith(Rot4 dir, Thing parent)
        {
            IntVec3 c = parent.Position + dir.FacingCell;
            if (!c.InBounds(parent.Map))
            {
                return false;
            }

            return IsWall(c, parent.Map);
        }

        public override bool ShouldDrawRotated => this.data == null || this.data.drawRotated;

        private bool IsWall(IntVec3 pos, Map map)
        {
            return !pos.GetThingList(map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t.def.building != null)
                .Any(t => t.def.passability == Traversability.Impassable || (t is Building_Door));
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            int num = 0;
            if (thingDef.PlaceWorkers?.All(p => p.AllowsPlacing(thingDef, loc.ToIntVec3(), rot, Find.CurrentMap).Accepted) ?? false)
            {
                int num2 = 1;
                for (int i = 0; i < 4; i++)
                {
                    IntVec3 c = loc.ToIntVec3() + GenAdj.CardinalDirections[i];
                    if (this.IsWall(c, Find.CurrentMap))
                    {
                        num += num2;
                    }
                    num2 *= 2;
                }
            }
            LinkDirections linkSet = (LinkDirections)num;
            var material = this.subMats[(int)linkSet];
            material.shader = ShaderDatabase.Transparent;

            Mesh mesh = this.MeshAt(rot);
            Quaternion quaternion = this.QuatFromRot(rot);
            if (extraRotation != 0f)
            {
                quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
            }
            Graphics.DrawMesh(mesh, loc, quaternion, material, 0);
            if (this.ShadowGraphic != null)
            {
                this.ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            }
        }
    }
}
