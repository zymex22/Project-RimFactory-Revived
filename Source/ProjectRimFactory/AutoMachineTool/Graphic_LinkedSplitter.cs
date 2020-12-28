using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;
using ProjectRimFactory.AutoMachineTool;

using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool {
    /// <summary>
    /// The graphic for the Conveyor Splitter.  This distinguishes itself by
    ///   having the standard linked conveyor graphic plus a building placed
    ///   above the belt (and moving objects).
    /// The building is hardcoded here, but this graphic could easily be 
    ///   extended to take the "building" graphic from a DefModExtension
    ///   and work for any such "building over item over building" scenario.
    /// Yeah, maybe I should have done that, but this was easier.
    /// --LWM
    /// </summary>
    [StaticConstructorOnStartup]
    public class Graphic_LinkedSplitter : Graphic_LinkedConveyorV2, IHaveGraphicExtraData {
        // default arrow to show the input direction for the splitter:
        public static Material arrow00b;
        // default arrow to show all output directions for the splitter:
        public static Material arrow01;

        public GraphicData splitterBuildingDoorOpen;
        public GraphicData splitterBuildingDoorClosed;
        public GraphicData splitterBuildingBlueprint;

        public Material arrowInput;
        public Material arrowOutput;

        static Graphic_LinkedSplitter() {
            arrow01 = MaterialPool.MatFrom("Belts/SmallArrow01");
            arrow00b = MaterialPool.MatFrom("Belts/SmallArrow00b");
        }

        public override void Init(GraphicRequest req) {
            // Move all Init to ExtraInit
            var extraData = GraphicExtraData.Extract(req, out GraphicRequest newReq);
            ExtraInit(newReq, extraData);
        }

        public override void ExtraInit(GraphicRequest req, GraphicExtraData extraData) {
            arrowInput = arrow00b;
            arrowOutput = arrow01;
            base.ExtraInit(req, extraData);
            if (extraData == null) {
                Log.Error("PRF: invalid XML for conveyor Splitter's graphic:\n" +
                  "   it must have <texPath>[extraData]...[texPath2]path/to/building[/texPath2]</texPath>\n"+
                  "   but has string: "+req.path);
                return;
            }
            string doorBasePath = extraData.texPath2;
            splitterBuildingDoorOpen = new GraphicData
            {
                graphicClass = typeof(Graphic_Single),
                texPath = doorBasePath + "_Open",
                drawSize = Vector2.one
            };
            splitterBuildingDoorClosed = new GraphicData
            {
                graphicClass = typeof(Graphic_Single),
                texPath = doorBasePath + "_Closed",
                drawSize = Vector2.one
            };
            splitterBuildingBlueprint = new GraphicData
            {
                graphicClass = typeof(Graphic_Single),
                texPath = doorBasePath + "_Blueprint",
                drawSize = Vector2.one
            };
            if (extraData.arrowTexPath1 != null) {
                this.arrowOutput = MaterialPool.MatFrom(extraData.arrowTexPath1);
            }
            if (extraData.arrowTexPath2 != null) {
                this.arrowInput = MaterialPool.MatFrom(extraData.arrowTexPath2);
            }
        }

        //Note: if someone wanted to make this accessible via XML, go for it!
        public Vector3 BuildingOffset => new Vector3(0, 0.3f, 0); // mostly picked by trial and error
        // Make sure arrows show over building:
        public Vector3 ArrowOffset(Rot4 rot)
        {
            var preOffset = this.arrowOffsetsByRot4[rot.AsInt];
            var building = this.BuildingOffset;
            var offset = new Vector3(preOffset.x,
                     Mathf.Max(preOffset.y, building.y + 0.01f),
                     preOffset.z);
            return offset;
        }

        public override void Print(SectionLayer layer, Thing thing) {
            base.Print(layer, thing);
            // Similar to Graphic_LinkedConveyor, we need to not print this
            //   if it's underground (unless something is selected, etc etc)
            if (thing is Building_BeltSplitter splitter) {
                if (splitter.IsUnderground && !(layer is SectionLayer_UGConveyor))
                    return;
                // We want to draw the open door only if something is using the S
                //   facing wall, so either an output link to the S or an incoming link:
                if ((splitter.OutputLinks.TryGetValue(Rot4.South, out var link) &&
                     link.Active)
                    || splitter.IncomingLinks.Any(o => splitter.Position + Rot4.South.FacingCell == o.Position)) {
                    // Draw open door
                    var mat = splitterBuildingDoorOpen.Graphic.MatSingleFor(thing);
                    Printer_Plane.PrintPlane(layer, thing.TrueCenter() + BuildingOffset,
                        Vector2.one, mat);
                } else {
                    // Draw closed door
                    var mat = splitterBuildingDoorClosed.Graphic.MatSingleFor(thing);
                    Printer_Plane.PrintPlane(layer, thing.TrueCenter() + BuildingOffset,
                        Vector2.one, mat);
                }
                // Print the splitter version of the tiny yellow arrow showing input direction:
                foreach (var i in splitter.IncomingLinks) {
                    if (i.Position.IsNextTo(splitter.Position)) { // otherwise need new logic
                        // splitter.Position + offset = i.Position, so
                        // offset = i.Position - splitter.Position
                        var r = Rot4.FromIntVec3(i.Position - splitter.Position);
                        Printer_Plane.PrintPlane(layer, thing.TrueCenter()
                                     + ArrowOffset(r), this.drawSize, arrowInput,
                                     r.Opposite.AsAngle);
                    }
                }
                // print tiny brown arrows pointing in output directions:
                foreach (var d in splitter.ActiveOutputDirections) {
                    Printer_Plane.PrintPlane(layer, thing.TrueCenter() + 
                             ArrowOffset(d),
                             this.drawSize, arrowOutput, d.AsAngle);
                }
            } else { // blueprint?
                //var mat = FadedMaterialPool.FadedVersionOf(splitterBuildingDoorClosed.Graphic.MatSingleFor(thing), 0.5f);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.3f, 0),
                    Vector2.one, splitterBuildingBlueprint.Graphic.MatSingleFor(thing));
            }
        }

    }
}
