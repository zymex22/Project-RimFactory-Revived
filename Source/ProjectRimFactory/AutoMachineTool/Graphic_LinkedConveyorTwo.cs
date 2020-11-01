using System;

using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRimFactory.AutoMachineTool {
    public class Graphic_LinkedConveyorTwo : Graphic {
        protected Graphic NS;
        protected Graphic EW;

        public Graphic_LinkedConveyorTwo() : base() {
        }

        public override void Init(GraphicRequest req) {
            // All basically pointless:
            this.data = req.graphicData;
            this.path = req.path;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            // What we want is two duplicate graphics with slightly different paths and
            //   then *we want a linked version* of them.
            // We duplicate the graphic requests (no idea if it' necessary to do all of
            //   this, but *it works* - "it works" is a general theme, here)
            GraphicData ewData = new GraphicData();
            ewData.CopyFrom(req.graphicData);
            ewData.drawRotated = false;
            ewData.graphicClass = typeof(Graphic_LinkedConveyorV2);
            ewData.texPath += "_East";
            GraphicRequest ewReq = new GraphicRequest(ewData.graphicClass, ewData.texPath, req.shader, req.drawSize,
                req.color, req.colorTwo, ewData, req.renderQueue, req.shaderParameters);
            EW = new Graphic_LinkedConveyorV2();
            EW.Init(ewReq);

            // And NS:
            GraphicData nsData = new GraphicData();
            nsData.CopyFrom(req.graphicData);
            nsData.drawRotated = false;
            nsData.graphicClass = typeof(Graphic_LinkedConveyorV2);
            nsData.texPath += "_North";
            GraphicRequest nsReq = new GraphicRequest(nsData.graphicClass, nsData.texPath, req.shader, req.drawSize,
                req.color, req.colorTwo, nsData, req.renderQueue, req.shaderParameters);
            NS = new Graphic_LinkedConveyorV2();
            NS.Init(nsReq);
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
