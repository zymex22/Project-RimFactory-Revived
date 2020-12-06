using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    /// <summary>
    ///     This is the original Nobo version of Linked Graphics for our use
    ///     It uses an unpadded atlas, which causes problems when the
    ///     atlas has enough detail: the Link2 class doesn't have any
    ///     buffer between bits of the atlas, and the edges can bleed
    ///     into each other. It can lead to thin-line artifacts along
    ///     edges.  Prefer to use Graphic_LinkedConveyorV2 if there's
    ///     any edge contact.  This may be fine for some applicatios,
    ///     such as lights.
    /// </summary>
    public abstract class Graphic_Linked2 : Graphic
    {
        public Graphic subGraphic;

        protected Material[] subMats = new Material[16];

        public Graphic_Linked2()
        {
            subGraphic = new Graphic_Single();
        }

        public override Material MatSingle => subMats[(int) LinkDirections.None];

        public override Material MatSingleFor(Thing thing)
        {
            return LinkedDrawMatFrom(thing, thing.Position);
        }

        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;
            subGraphic.Init(req);

            CreateSubMats();
        }

        public void CreateSubMats()
        {
            var mainTextureScale = new Vector2(0.25f, 0.25f);
            for (var i = 0; i < 16; i++)
            {
                var x = i % 4 * 0.25f;
                var y = i / 4 * 0.25f;
                var mainTextureOffset = new Vector2(x, y);
                var material = new Material(subGraphic.MatSingle);
                material.name = subGraphic.MatSingle.name + "_ASM" + i;
                material.mainTextureScale = mainTextureScale;
                material.mainTextureOffset = mainTextureOffset;
                subMats[i] = material;
            }
        }

        protected Material LinkedDrawMatFrom(Thing parent, IntVec3 cell)
        {
            var num = 0;
            var num2 = 1;
            for (var i = 0; i < 4; i++)
            {
                if (ShouldLinkWith(new Rot4(i), parent)) num += num2;
                num2 *= 2;
            }

            var linkSet = (LinkDirections) num;
            return LinkedMaterial(parent, linkSet);
        }

        public virtual Material LinkedMaterial(Thing parent, LinkDirections linkSet)
        {
            return subMats[(int) linkSet];
        }

        public abstract bool ShouldLinkWith(Rot4 dir, Thing parent);

        public override void Print(SectionLayer layer, Thing thing)
        {
            var mat = LinkedDrawMatFrom(thing, thing.Position);
            Printer_Plane.PrintPlane(layer, thing.TrueCenter(), drawSize, mat);
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (!(thing is MinifiedThing))
                GraphicDatabase.Get<Graphic_Single>(thingDef.uiIconPath, ShaderTypeDefOf.EdgeDetect.Shader,
                        thingDef.graphicData.drawSize, color, colorTwo)
                    .DrawWorker(loc, rot, thingDef, thing, extraRotation);
            else
                base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }
    }

    public abstract class Graphic_Link2<T> : Graphic_Linked2 where T : Graphic_Linked2, new()
    {
        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            var g = new T();
            g.subGraphic = subGraphic.GetColoredVersion(newShader, newColor, newColorTwo);
            g.data = data;
            g.CreateSubMats();
            return g;
        }
    }
}