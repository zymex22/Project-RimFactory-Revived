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
        // to show the input direction for the splitter:
        public static Material arrow00b;
        // to show all output directions for the splitter:
        public static Material arrow01;

        private GraphicData splitterBuildingDoorOpen;
        private GraphicData splitterBuildingDoorClosed;

        static Graphic_LinkedSplitter() {
            arrow01 = MaterialPool.MatFrom("Belts/SmallArrow01");
            arrow00b = MaterialPool.MatFrom("Belts/SmallArrow00b");
        }

        public override void Init(GraphicRequest req) {
            var extraData = GraphicExtraData.Extract(req, out GraphicRequest newReq);
            ExtraInit(newReq, extraData);
        }

        public override void ExtraInit(GraphicRequest req, GraphicExtraData extraData) {
            base.ExtraInit(req, extraData);
            if (extraData == null) {
                Log.Error("PRF: invalid XML for conveyor Splitter's graphic:\n" +
                  "   it must have <texPath>[extraData]...[texPath2]path/to/building[/texPath2]</texPath>");
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
        }

        public override void Print(SectionLayer layer, Thing thing) {
            base.Print(layer, thing);
            // Similar to Graphic_LinkedConveyor, we need to not print this
            //   if it's underground (unless something is selected, etc etc)
            if (thing is Building_BeltSplitter splitter) {
                if (splitter.IsUnderground && !(layer is SectionLayer_UGConveyor))
                    return;
                if ((splitter.OutputLinks.ContainsKey(Rot4.South) &&
                     splitter.OutputLinks[Rot4.South].Active)
                    || splitter.Rotation == Rot4.North) {
                    // Draw open door
                    var mat = splitterBuildingDoorOpen.Graphic.MatSingleFor(thing);
                    Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.3f, 0),
                        Vector2.one, mat);
                } else {
                    // Draw closed door
                    var mat = splitterBuildingDoorClosed.Graphic.MatSingleFor(thing);
                    Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.3f, 0),
                        Vector2.one, mat);
                }
                // Print the splitter version of the tiny yellow arrow showing direction:
                Printer_Plane.PrintPlane(layer, thing.TrueCenter()
                    + new Vector3(0, 1f, 0), this.drawSize, arrow00b,
                    thing.Rotation.AsAngle);
                // print tiny brown arrows pointing in output directions:
                foreach (var d in splitter.ActiveOutputDirections) {
                    Printer_Plane.PrintPlane(layer, thing.TrueCenter() + 
                             this.arrowOffsetsByRot4[d.AsInt],
                             this.drawSize, arrow01, d.AsAngle);
                }
            } else { // blueprint?
                var mat = FadedMaterialPool.FadedVersionOf(splitterBuildingDoorClosed.Graphic.MatSingleFor(thing), 0.5f);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.3f, 0),
                    Vector2.one, mat);
            }
        }

    }
}
