using System;

using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRimFactory.AutoMachineTool {
    public class Graphic_LinkedConveyorTwo : Graphic_Two<Graphic_LinkedConveyorV2> { }
    public class Graphic_LinkedSplitterTwo : Graphic_Two<Graphic_LinkedSplitter> { }

    public class Graphic_Two<T> : Graphic, IHaveGraphicExtraData 
                                     where T : Graphic, new() {
        protected Graphic NS;
        protected Graphic EW;
        public Graphic_Two() : base() {
        }

        public override void Init(GraphicRequest req) {
            GraphicRequest newReq;
            var extraData = GraphicExtraData.Extract(req, out newReq);
            ExtraInit(newReq, extraData);
        }
        public virtual void ExtraInit(GraphicRequest req, GraphicExtraData extraData) {
            // All basically pointless:
            this.data = req.graphicData;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            this.path = req.path;
            // What we want is two duplicate graphics with slightly different paths
            // EW
            var childReq = GraphicExtraData.CopyGraphicRequest(req);
            childReq.path += "_East";
            childReq.graphicData.texPath += "_East";
            childReq.graphicClass = typeof(T);
            var tmpLC = new T();
            if (tmpLC is IHaveGraphicExtraData ihged)
                ihged.ExtraInit(childReq, extraData);
            else
                tmpLC.Init(req);
            EW = tmpLC;

            // And NS:
            childReq = GraphicExtraData.CopyGraphicRequest(req);
            childReq.path += "_North";
            childReq.graphicData.texPath += "_North";
            childReq.graphicClass = typeof(T);
            tmpLC = new T();
            if (tmpLC is IHaveGraphicExtraData ihged2)
                ihged2.ExtraInit(childReq, extraData);
            else
                tmpLC.Init(req);
            NS = tmpLC;
        }
        public override Material MatSingle {
            get {
                return NS.MatSingle;
            }
        }
        public override Material MatSingleFor(Thing thing) {
            if (thing.Rotation == Rot4.North || thing.Rotation == Rot4.South)
                return NS.MatSingleFor(thing);
            return EW.MatSingleFor(thing);
        }
        public override void Print(SectionLayer layer, Thing thing) {
            if (thing.Rotation == Rot4.East || thing.Rotation == Rot4.West)
                EW.Print(layer, thing);
            else
                NS.Print(layer, thing);
        }
        // I have NOOOOO idea if these are actually useful/important:
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation) {
            if (rot == Rot4.North || rot == Rot4.South)
                NS.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            else
                EW.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }
        public override Material MatAt(Rot4 rot, Thing thing = null) {
            if (rot == Rot4.East || rot == Rot4.West)
                return EW.MatAt(rot, thing);
            return NS.MatAt(rot, thing);
        }
    }
}
