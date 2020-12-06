using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    /// <summary>
    ///     The wall graphic is special as it needs transition graphics from wall->non-wall.
    ///     The transition graphics are on the W, S, and E (can't see the north side of walls,
    ///     so there's no extra graphic in that direction)
    ///     Note: This copies vanilla's base LinkedDrawMatFrom for faster execution
    /// </summary>
    public class Graphic_LinkedConveyorWall : Graphic_LinkedConveyorV2, IHaveGraphicExtraData
    {
        private List<ThingDef> sameLinkDefs;
        private bool showE;
        private bool showS;
        private bool showW;
        private GraphicData transitionGData; // Graphic_Multi, one assumes.

        public override void ExtraInit(GraphicRequest req, GraphicExtraData extraData)
        {
            Debug.Warning(Debug.Flag.ConveyorGraphics,
                "Graphics ExtraInit for Graphic_LinkedConveyorWall: (" + req.graphicData.texPath + ")");
            base.ExtraInit(req, extraData);
            if (extraData == null)
            {
                Log.Error("PRF's Wall Conveyor graphic requires GraphicExtraData");
            }
            else if (extraData.graphicData1 == null)
            {
                Log.Error("PRF's Wall Conveyor grahpic requires GraphicExtraData's graphicData1");
            }
            else
            {
                transitionGData = new GraphicData();
                transitionGData.CopyFrom(extraData.graphicData1);
                // If this throws errors, that's okay - it's a config error that needs to be fixed:
                // NOTE: this can be changed later if the graphics needs to allow multilpe defs,
                //   not all of which may be actually listed...
                sameLinkDefs = new List<ThingDef>(extraData.specialLinkDefs.Select(
                    s => DefDatabase<ThingDef>.GetNamed(s)));
                Debug.Message(Debug.Flag.ConveyorGraphics, "  added sameLinkDefs: " +
                                                           (sameLinkDefs == null
                                                               ? "null"
                                                               : string.Join(", ", sameLinkDefs)));
            }
        }

        public override void Print(SectionLayer layer, Thing thing)
        {
            showW = false;
            showS = false;
            showE = false;
            // This may set some of those flags:
            base.Print(layer, thing);
            Debug.Message(Debug.Flag.ConveyorGraphics,
                "Printing transitions for " + thing + " S:" + showS + " W:" + showW + "E:" + showE);
            Material mat;
            if (showW)
            {
                mat = transitionGData.Graphic.MatWest;
//                mat = transitionWest.Graphic.MatSingleFor(thing);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.1f, 0),
                    Vector2.one, mat);
            }

            if (showS)
            {
                mat = transitionGData.Graphic.MatSouth;
//                mat = transitionSouth.Graphic.MatSingleFor(thing);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.1f, 0),
                    Vector2.one, mat);
            }

            if (showE)
            {
                mat = transitionGData.Graphic.MatEast;
//                mat = transitionEast.Graphic.MatSingleFor(thing);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.1f, 0),
                    Vector2.one, mat);
            }
        }

        public override bool ShouldLinkWith(IntVec3 c, Thing parent)
        {
            // Tag which directions link walls to non-walls, so can draw transition buildings
            if (parent is Blueprint) return base.ShouldLinkWith(c, parent);
            var x = c - parent.Position;
            if (x == IntVec3.North) return base.ShouldLinkWith(c, parent);
            if (!c.InBounds(parent.Map)) return false;
            var belt = parent as IBeltConveyorLinkable; // don't use this with non-belts
            var otherBelt = c.GetThingList(parent.Map)
                .OfType<IBeltConveyorLinkable>()
                .FirstOrDefault(belt.HasLinkWith);
            if (otherBelt == null) return false;
            Debug.Message(Debug.Flag.ConveyorGraphics,
                "WallBelt graphic testing links vs sameLinkDefs for " + parent + ": " +
                (sameLinkDefs == null ? "null" : string.Join(", ", sameLinkDefs)));
            if (!sameLinkDefs.Contains((otherBelt as Thing).def))
            {
                Debug.Message(Debug.Flag.ConveyorGraphics,
                    " found link with " + otherBelt + " (" + (otherBelt as Thing).def.defName + ")");
                if (x == IntVec3.East) showE = true;
                else if (x == IntVec3.South) showS = true;
                else // has to be W, or something is v v weird
                    showW = true;
            }

            return true;
        }
    }
}