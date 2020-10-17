using System;

using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRimFactory.AutoMachineTool {
    public class Graphic_LinkedConveyorTwo : Graphic {
        protected Graphic_LinkedConveyor NS;
        protected Graphic_LinkedConveyor EW;

        public Graphic_LinkedConveyorTwo() : base() {
            //Note: this approach takes advantage of the fact that Graphic_LinkedConveyor
            //  properly rotates its "null" conveyor on its own
            //  and the fact that the artist did not rotate the null conveyor in the _East
            //  graphic!  So it's a fortuitous accident, and there is no need to set a flag.
            //  ...
            //  for NOW!
            NS = new Graphic_LinkedConveyor();
            EW = new Graphic_LinkedConveyor();
        }

        public override void Init(GraphicRequest req) {
            //base.Init(req);

            this.data = req.graphicData;
            this.path = req.path;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            // Create child Graphics, with _East and _North names
            //   This seems to work, so ...sure?
            string origPath = ""+req.path;
            string origTexPath = "" + req.graphicData.texPath;
            req.graphicData.texPath += "_North"; // probably safe!
            req.path += "_North";
            this.NS.Init(req);
            req.graphicData.texPath = origTexPath + "_East";
            req.path = origPath + "_East";
            this.EW.Init(req);
            req.graphicData.texPath = origTexPath; // no idea if this is important. Nooooo idea.
            req.path = origPath;
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
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation) {
            if (rot == Rot4.North || rot == Rot4.South)
                NS.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            else
                EW.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }

    }
}
