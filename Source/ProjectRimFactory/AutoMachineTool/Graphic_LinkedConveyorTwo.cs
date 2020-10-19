using System;

using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRimFactory.AutoMachineTool {
    public class Graphic_LinkedConveyorTwo : Graphic {
        protected Graphic NS;
        protected Graphic EW;

        public Graphic_LinkedConveyorTwo() : base() {
            //Note: this approach takes advantage of the fact that Graphic_LinkedConveyor
            //  properly rotates its "null" conveyor on its own
            //  and the fact that the artist did not rotate the null conveyor in the _East
            //  graphic!  So it's a fortuitous accident, and there is no need to set a flag.
            //  ...
            //  for NOW!
//            NS = new Graphic_LinkedConveyorV2();
//            EW = new Graphic_LinkedConveyorV2();
        }

        public override void Init(GraphicRequest req) {
            //base.Init(req);
            // All basically pointless:
            this.data = req.graphicData;
            this.path = req.path;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            // What we want is two duplicate graphics with slightly different paths and
            //   then *we want a linked version* of them.
            // The way RW handles linked graphics is by using a graphicData that has a
            //   cached Linked graphic.  We set up a similar thing here (altho we have
            //   to make our cached linked graphic the hard way)
            GraphicData ewData = new GraphicData();
            ewData.CopyFrom(req.graphicData);
            ewData.drawRotated = false;
            ewData.graphicClass = typeof(Graphic_Single);
            ewData.texPath += "_East";
            // force Init(), which creates first
            //   cached graphic
            var tmpGraphic = ewData.Graphic;
            var cachedGraphicField = typeof(GraphicData).GetField("cachedGraphic", System.Reflection.BindingFlags.NonPublic
                   | System.Reflection.BindingFlags.SetField | System.Reflection.BindingFlags.Instance);
            cachedGraphicField.SetValue(ewData, new Graphic_LinkedConveyorV2(tmpGraphic));
            GraphicRequest ewReq = new GraphicRequest(ewData.graphicClass, ewData.texPath, req.shader, req.drawSize,
                req.color, req.colorTwo, ewData, req.renderQueue, req.shaderParameters);
            EW = new Graphic_Single();
            EW.Init(ewReq);

            // And NS:
            GraphicData nsData = new GraphicData();
            nsData.CopyFrom(req.graphicData);
            nsData.drawRotated = false;
            nsData.graphicClass = typeof(Graphic_Single);
            nsData.texPath += "_North";
            // force Init(), which creates first
            //   cached graphic
            tmpGraphic = nsData.Graphic;
            cachedGraphicField.SetValue(nsData, new Graphic_LinkedConveyorV2(tmpGraphic));
            GraphicRequest nsReq = new GraphicRequest(nsData.graphicClass, nsData.texPath, req.shader, req.drawSize,
                req.color, req.colorTwo, nsData, req.renderQueue, req.shaderParameters);
            NS = new Graphic_Single();
            NS.Init(nsReq);

            /*
            // Create child Graphics, with _East and _North names
            //   This seems to work, so ...sure?
            string origPath = ""+req.path;
            string origTexPath = "" + req.graphicData.texPath;
            req.graphicData.texPath += "_North"; // probably safe!
            req.path += "_North";
            Log.Message("Two-About to init " + origPath);
            this.NS.Init(req);
            req.graphicData.texPath = origTexPath + "_East";
            req.path = origPath + "_East";
            this.EW.Init(req);
            req.graphicData.texPath = origTexPath; // no idea if this is important. Nooooo idea.
            req.path = origPath;
            */
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
            Log.Message("G_LC2 has print request for " + thing);
            if (thing.Rotation == Rot4.East || thing.Rotation == Rot4.West)
                EW.Print(layer, thing);
            else
                NS.Print(layer, thing);
        }
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation) {
            Log.Message("DrawWorker request");
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
