using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;
namespace ProjectRimFactory.AutoMachineTool {
    /// <summary>
    /// The wall graphic is special as it needs transition graphics from wall->non-wall.
    /// The transition graphics are on the W, S, and E (can't see the north side of walls,
    /// so there's no extra graphic in that direction)
    /// Note: This copies vanilla's base LinkedDrawMatFrom for faster execution
    /// </summary>
    public class Graphic_LinkedConveyorWall : Graphic_LinkedConveyorV2, IHaveGraphicExtraData {
        public Graphic_LinkedConveyorWall() : base() {
        }
        bool showW;
        bool showS;
        bool showE;
        //TODO: I'm an idiot: this should be a Graphic_Multi!  Then zymex can play with normal RW graphics settings
        GraphicData transitionWest;
        GraphicData transitionSouth;
        GraphicData transitionEast;
        List<ThingDef> sameLinkDefs;
        public override void ExtraInit(GraphicRequest req, GraphicExtraData extraData) {
            Debug.Warning(Debug.Flag.ConveyorGraphics, "Graphics ExtraInit for Graphic_LinkedConveyorWall: (" + req.graphicData.texPath + ")");
            base.ExtraInit(req, extraData);
            if (extraData == null) Log.Error("PRF's Wall Conveyor graphic requires GraphicExtraData");
            else {
                transitionWest = new GraphicData
                {
                    graphicClass = typeof(Graphic_Single),
                    texPath = extraData.texPath2 + "_West",
                    drawSize = Vector2.one
                };
                transitionSouth = new GraphicData
                {
                    graphicClass = typeof(Graphic_Single),
                    texPath = extraData.texPath2 + "_South",
                    drawSize = Vector2.one
                };
                transitionEast = new GraphicData
                {
                    graphicClass = typeof(Graphic_Single),
                    texPath = extraData.texPath2 + "_East",
                    drawSize = Vector2.one
                };
                // If this throws errors, that's okay - it's a config error that needs to be fixed:
                // NOTE: this can be changed later if the graphics needs to allow multilpe defs,
                //   not all of which may be actually listed...
                sameLinkDefs = new List<ThingDef>(extraData.specialLinkDefs.Select(
                    s=>DefDatabase<ThingDef>.GetNamed(s)));
                Debug.Message(Debug.Flag.ConveyorGraphics, "  added sameLinkDefs: " +
                    (sameLinkDefs == null ? "null" : String.Join(", ", sameLinkDefs)));
            }
        }
        public override void Print(SectionLayer layer, Thing thing) {
            showW = false; showS = false; showE = false;
            // This may set some of those flags:
            base.Print(layer, thing);
            Debug.Message(Debug.Flag.ConveyorGraphics, "Printing transitions for " + thing + " S:" + showS + " W:" + showW + "E:" + showE);
            Material mat;
            if (showW) {
                mat = transitionWest.Graphic.MatSingleFor(thing);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.1f, 0),
                    Vector2.one, mat);
            }
            if (showS) {
                mat = transitionSouth.Graphic.MatSingleFor(thing);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.1f, 0),
                    Vector2.one, mat);
            }
            if (showE) {
                mat = transitionEast.Graphic.MatSingleFor(thing);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.1f, 0),
                    Vector2.one, mat);
            }
        }
        public override bool ShouldLinkWith(IntVec3 c, Thing parent) {
            // Tag which directions link walls to non-walls, so can draw transition buildings
            if (parent is Blueprint) return base.ShouldLinkWith(c, parent);
            var x = (c - parent.Position);
            if (x == IntVec3.North) return base.ShouldLinkWith(c, parent);
            if (!c.InBounds(parent.Map)) return false;
            var belt = parent as IBeltConveyorLinkable; // don't use this with non-belts
            var otherBelt = c.GetThingList(parent.Map)
                    .OfType<IBeltConveyorLinkable>()
                    .FirstOrDefault(belt.HasLinkWith);
            if (otherBelt == null) return false;
            Debug.Message(Debug.Flag.ConveyorGraphics, "WallBelt graphic testing links vs sameLinkDefs for " + parent + ": " + (sameLinkDefs == null ? "null" : String.Join(", ", sameLinkDefs)));
            if (!sameLinkDefs.Contains((otherBelt as Thing).def)) {
                Debug.Message(Debug.Flag.ConveyorGraphics, " found link with " + otherBelt + " (" + (otherBelt as Thing).def.defName + ")");
                if (x == IntVec3.East) showE = true;
                else if (x == IntVec3.South) showS = true;
                else // has to be W, or something is v v weird
                    showW = true;
            }
            return true;
        }
    }
}
