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
        GraphicData transitionWest;
        GraphicData transitionSouth;
        GraphicData transitionEast;
        List<ThingDef> sameLinkDefs;
        public override void ExtraInit(GraphicRequest req, GraphicExtraData extraData) {
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
                sameLinkDefs = extraData.specialLinkDefs;
            }
        }
        public override void Print(SectionLayer layer, Thing thing) {
            showW = false; showS = false; showE = false;
            // This may set some of those flags:
            base.Print(layer, thing);
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
            var belt = parent as IBeltConveyorLinkable;
            var otherBelt = c.GetThingList(parent.Map)
                    .OfType<IBeltConveyorLinkable>()
                    .FirstOrDefault(belt.HasLinkWith);
            if (otherBelt == null) return false;
            if (!sameLinkDefs.Contains((otherBelt as Thing).def)) {
                if (x == IntVec3.East) showE = true;
                else if (x == IntVec3.South) showS = true;
                else // has to be W, or something is v v weird
                    showW = true;
            }
            return true;
        }
    }
}
