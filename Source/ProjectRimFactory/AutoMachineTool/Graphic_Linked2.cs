using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class Graphic_Linked2 : Graphic
    {
        public Graphic_Linked2() : base()
        {
            this.subGraphic = new Graphic_Single();
        }

        public Graphic subGraphic;

        public override Material MatSingle
        {
            get
            {
                return this.subMats[(int)LinkDirections.None];
            }
        }

        public override Material MatSingleFor(Thing thing)
        {
            return this.LinkedDrawMatFrom(thing, thing.Position);
        }

        public override void Init(GraphicRequest req)
        {
            this.data = req.graphicData;
            this.path = req.path;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            this.subGraphic.Init(req);

            this.CreateSubMats();
        }

        public void CreateSubMats()
        {
            Vector2 mainTextureScale = new Vector2(0.25f, 0.25f);
            for (int i = 0; i < 16; i++)
            {
                float x = (float)(i % 4) * 0.25f;
                float y = (float)(i / 4) * 0.25f;
                Vector2 mainTextureOffset = new Vector2(x, y);
                Material material = new Material(this.subGraphic.MatSingle);
                material.name = this.subGraphic.MatSingle.name + "_ASM" + i;
                material.mainTextureScale = mainTextureScale;
                material.mainTextureOffset = mainTextureOffset;
                this.subMats[i] = material;
            }
        }

        protected Material[] subMats = new Material[16];

        protected Material LinkedDrawMatFrom(Thing parent, IntVec3 cell)
        {
            int num = 0;
            int num2 = 1;
            for (int i = 0; i < 4; i++)
            {
                if (this.ShouldLinkWith(new Rot4(i), parent))
                {
                    num += num2;
                }
                num2 *= 2;
            }
            LinkDirections linkSet = (LinkDirections)num;
            return LinkedMaterial(parent, linkSet);
        }

        public virtual Material LinkedMaterial(Thing parent, LinkDirections linkSet)
        {
            return this.subMats[(int)linkSet];
        }

        public abstract bool ShouldLinkWith(Rot4 dir, Thing parent);

        public override void Print(SectionLayer layer, Thing thing)
        {
            Material mat = this.LinkedDrawMatFrom(thing, thing.Position);
            Printer_Plane.PrintPlane(layer, thing.TrueCenter(), this.drawSize, mat);
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (!(thing is MinifiedThing))
            {
                GraphicDatabase.Get<Graphic_Single>(thingDef.uiIconPath, ShaderTypeDefOf.EdgeDetect.Shader, thingDef.graphicData.drawSize, this.color, this.colorTwo)
                    .DrawWorker(loc, rot, thingDef, thing, extraRotation);
            }
            else {
                base.DrawWorker(loc,rot,thingDef,thing,extraRotation);
            }

        }
    }

    public abstract class Graphic_Link2<T> : Graphic_Linked2 where T : Graphic_Linked2, new()
    {
        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            var g = new T();
            g.subGraphic = this.subGraphic.GetColoredVersion(newShader, newColor, newColorTwo);
            g.data = this.data;
            g.CreateSubMats();
            return g;
        }
    }
}
