using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Graphic_LinkedConveyorTwo : Graphic_Two<Graphic_LinkedConveyorV2> { }
    public class Graphic_LinkedSplitterTwo : Graphic_Two<Graphic_LinkedSplitter> { }
    public class Graphic_LinkedConveyorWallTwo : Graphic_Two<Graphic_LinkedConveyorWall> { }

    public class Graphic_Two<T> : Graphic, IHaveGraphicExtraData
                                     where T : Graphic, new()
    {
        protected Graphic NS;
        protected Graphic EW;
        public Graphic_Two() : base()
        {
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return NS;
        }

        public override void Init(GraphicRequest req)
        {
            GraphicRequest newReq;
            var extraData = GraphicExtraData.Extract(req, out newReq);
            ExtraInit(newReq, extraData);
        }
        public virtual void ExtraInit(GraphicRequest req, GraphicExtraData extraData)
        {
            // All basically pointless:
            data = req.graphicData;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;
            path = extraData?.texPath ?? req.path;
            // What we want is two duplicate graphics with slightly different paths
            EW = MakeSubgraphic(req, extraData, "_East");
            NS = MakeSubgraphic(req, extraData, "_North");
        }
        protected Graphic MakeSubgraphic(GraphicRequest req, GraphicExtraData extraData, string texSuffix)
        {
            var childReq = GraphicExtraData.CopyGraphicRequest(req);
            childReq.graphicClass = typeof(T);
            var tmpG = new T();
            if (tmpG is IHaveGraphicExtraData ihged && extraData != null)
            {
                var tmpPath = extraData.texPath;
                extraData.texPath += texSuffix;
                ihged.ExtraInit(childReq, extraData);
                extraData.texPath = tmpPath;
            }
            else
            {
                childReq.graphicData.texPath += texSuffix;
                childReq.path = childReq.graphicData.texPath;
                tmpG.Init(childReq);
            }
            return tmpG;
        }
        public override Material MatSingle => NS.MatSingle;

        public override Material MatSingleFor(Thing thing)
        {
            if (thing.Rotation == Rot4.North || thing.Rotation == Rot4.South)
                return NS.MatSingleFor(thing);
            return EW.MatSingleFor(thing);
        }
        //TODO Changed in 1.3 --> extraRotation was added. any changes needed?
        public override void Print(SectionLayer layer, Thing thing, float extraRotation)
        {
            if (thing.Rotation == Rot4.East || thing.Rotation == Rot4.West)
                EW.Print(layer, thing, extraRotation);
            else
                NS.Print(layer, thing, extraRotation);
        }
        // I have NOOOOO idea if these are actually useful/important:
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (rot == Rot4.North || rot == Rot4.South)
                NS.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            else
                EW.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }
        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            if (rot == Rot4.East || rot == Rot4.West)
                return EW.MatAt(rot, thing);
            return NS.MatAt(rot, thing);
        }
    }
}
