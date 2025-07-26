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

        protected override bool ShouldLinkWith(Rot4 dir, Thing parent)
        {
            var cell = parent.Position + dir.FacingCell;
            return cell.InBounds(parent.Map) && IsWall(cell, parent.Map);
        }

        public override bool ShouldDrawRotated => data == null || data.drawRotated;

        private static bool IsWall(IntVec3 pos, Map map)
        {
            return !pos.GetThingList(map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t.def.building != null)
                .Any(t => t.def.passability == Traversability.Impassable || (t is Building_Door));
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            var num = 0;
            if (thingDef.PlaceWorkers?.All(p => p.AllowsPlacing(thingDef, loc.ToIntVec3(), rot, Find.CurrentMap).Accepted) ?? false)
            {
                var num2 = 1;
                for (var i = 0; i < 4; i++)
                {
                    var cell = loc.ToIntVec3() + GenAdj.CardinalDirections[i];
                    if (IsWall(cell, Find.CurrentMap))
                    {
                        num += num2;
                    }
                    num2 *= 2;
                }
            }
            var material = subMats[(int)(LinkDirections)num];
            material.shader = ShaderDatabase.Transparent;
            
            var quaternion = QuatFromRot(rot);
            if (extraRotation != 0f)
            {
                quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
            }
            Graphics.DrawMesh(MeshAt(rot), loc, quaternion, material, 0);
            if (ShadowGraphic != null)
            {
                ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            }
        }
    }
}
