using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRimFactory.AutoMachineTool {
    /// <summary>
    /// A Graphic that handles custom Link logic - this can be adapted
    ///   to all sorts of other linked classes if needed.
    /// Requires a padded atlas (similar to Vanilla) (which avoids any
    ///   weird edge artifacts)
    /// </summary>
    [StaticConstructorOnStartup]
    public class Graphic_LinkedConveyorV2 : Verse.Graphic_Linked, IHaveGraphicExtraData {
        // Little yellow arrow that points the direction of conveyor travel:
        // HARDCODED DEFAULT:
        public static Material arrow00; // default arrow, initialized in the static constructor
        public Material arrow; // used by the graphic
        // Offsets used for placing those arrows:
        public Vector3[] arrowOffsetsByRot4 = {
                    new Vector3(0f, 0.1f, 0f),
                    new Vector3(0f, 0.1f, 0f),
                    new Vector3(0f, 0.1f, 0f),
                    new Vector3(0f, 0.1f, 0f)
            };

        public Graphic_LinkedConveyorV2() : base() {
        }
        public Graphic_LinkedConveyorV2(Graphic subGraphic) : base(subGraphic) {
        }

        public override void Init(GraphicRequest req) {
            arrow = arrow00;
            // I'm sure "req ... out req" is perfectly safe?
            var extraData = GraphicExtraData.Extract(req, out req);
            ExtraInit(req, extraData);
        }
        public virtual void ExtraInit(GraphicRequest req, GraphicExtraData extraData) {
            this.data = req.graphicData;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            this.subGraphic = new Graphic_Single();
            if (extraData == null) {
                this.subGraphic.Init(req);
                this.path = req.path;
                return;
            }
            var req2 = GraphicExtraData.CopyGraphicRequest(req, extraData.texPath);
            this.path = extraData.texPath;
            this.subGraphic.Init(req2);
            if (extraData.arrowDrawOffset != null) {
                var v = extraData.arrowDrawOffset.Value;
                for (int i = 0; i < 4; i++) {
                    arrowOffsetsByRot4[i] = new Vector3(v.x, v.y, v.z);
                }
            }
            if (extraData.arrowEastDrawOffset != null)
                arrowOffsetsByRot4[1] = extraData.arrowEastDrawOffset.Value;
            if (extraData.arrowWestDrawOffset != null)
                arrowOffsetsByRot4[3] = extraData.arrowWestDrawOffset.Value;
            if (extraData.arrowNorthDrawOffset != null)
                arrowOffsetsByRot4[0] = extraData.arrowNorthDrawOffset.Value;
            if (extraData.arrowSouthDrawOffset != null)
                arrowOffsetsByRot4[2] = extraData.arrowSouthDrawOffset.Value;
            if (extraData.arrowTexPath1 != null) {
                this.arrow = MaterialPool.MatFrom(extraData.arrowTexPath1);
            }
        }

        public override void Print(SectionLayer layer, Thing thing) {
            var conveyor = thing as IBeltConveyorLinkable;
            if (!(thing is Building_BeltConveyorUGConnector)
                && conveyor != null && conveyor.IsUnderground
                && !(layer is SectionLayer_UGConveyor)) {
                // Original Logic (notation by LWM)
                // if it IS NOT an underground connector
                // and it IS an IBeltLinkable
                // and it IS underground
                // and the layer IS NOT Sectionlayer for UGConveyor
                // then return
                // so.....
                // if it IS a connector
                // or it's NOT an IBletLinkable
                // or it's above ground
                // or it's UG's SectionLayer
                // then print this
                // So....don't print underground belts?
                return;
            }
            //TODO: print ourself if it's underground, so it's higher than walls and visible
            base.Print(layer, thing);
            // Print the tiny yellow arrow showing direction:

            //arrow is null on spawn (@LWM your init is not working)
            arrow = arrow ?? arrow00;

            Printer_Plane.PrintPlane(layer, thing.TrueCenter()
                + arrowOffsetsByRot4[thing.Rotation.AsInt], this.drawSize, arrow,
                thing.Rotation.AsAngle);
        }

        public override bool ShouldLinkWith(IntVec3 c, Thing parent) {
            //TODO: should probably cache this in the conveyor, for speed
            if (!c.InBounds(parent.Map)) {
                return false;
            }
            //TODO: Still need a good set of logic for this
            //  For example, if pointing down, and linked
            //    from the left, should curve.  But does not.
            // but at the same time, it would be nice to have 
            //   end conveyors actually end?

            /*
            // hardcoded for conveyors; no other conveyor buildings 
            //   will get this special logic.
            if (parent.GetType() == typeof(Building_BeltConveyor) &&
                parent.Rotation == dir) {
                return true;
            }
            */

            var blueprint = parent as Blueprint;
            if (blueprint == null) {
                var belt = parent as IBeltConveyorLinkable;
                return c.GetThingList(parent.Map)
                    .OfType<IBeltConveyorLinkable>()
                    .Any(other => other.CanLinkTo(belt) || (!belt.IsEndOfLine && belt.CanLinkTo(other)));
            }
            var def = (ThingDef)parent.def.entityDefToBuild;
            Rot4 dir;
            foreach (var r in Enumerable.Range(0, 4).Select(n => new Rot4(n))) {
                if ((parent.Position + r.FacingCell)==c) {
                    dir = r;
                    break;
                }
            }
            // Don't bother error checking. If an error shows up, we'll KNOW
            foreach (var l in (ConveyorLevel[])Enum
                                .GetValues(typeof(ConveyorLevel))) {
                if (canSendTos[def.thingClass](def, parent.Rotation,
                           dir, l)) {
                    foreach (var t in c.GetThingList(parent.Map)) {
                        if (t is Blueprint b) {
                            ThingDef tdef = b.def.entityDefToBuild as ThingDef;
                            Type tc = tdef?.thingClass;
                            if (typeof(Building_BeltConveyor).IsAssignableFrom(tc)) {
                                if (canGetFroms[tc](tdef, b.Rotation, dir.Opposite, l)) return true;
                            }
                        } else if (t is IBeltConveyorLinkable) {
                            if (canGetFroms[t.GetType()](t.def, t.Rotation, dir.Opposite, l)) return true;
                        }
                    }
                }
                if (canGetFroms[def.thingClass](def, parent.Rotation, dir, l)) {
                    foreach (var t in c.GetThingList(parent.Map)) {
                        if (t is Blueprint b) {
                            ThingDef tdef = b.def.entityDefToBuild as ThingDef;
                            Type tc = tdef?.thingClass;
                            if (typeof(Building_BeltConveyor).IsAssignableFrom(tc)) {
                                if (canSendTos[tc](tdef, b.Rotation, dir.Opposite, l)) return true;
                            }
                        } else if (t is IBeltConveyorLinkable) {
                            if (canSendTos[t.GetType()](t.def, t.Rotation, dir.Opposite, l)) return true;
                        }
                    }
                }
            }
            return false;
        }

        static Graphic_LinkedConveyorV2() { // this runs after graphics are loaded
            arrow00 = MaterialPool.MatFrom("Belts/SmallArrow00");
            canSendTos[typeof(Building_BeltConveyor)] = Building_BeltConveyor.CanDefSendToRot4AtLevel;
            canGetFroms[typeof(Building_BeltConveyor)] = Building_BeltConveyor.CanDefReceiveFromRot4AtLevel;
            canSendTos[typeof(Building_BeltConveyorUGConnector)] = Building_BeltConveyorUGConnector.CanDefSendToRot4AtLevel;
            canGetFroms[typeof(Building_BeltConveyorUGConnector)] = Building_BeltConveyorUGConnector.CanDefReceiveFromRot4AtLevel;
            canSendTos[typeof(Building_BeltSplitter)] = Building_BeltSplitter.CanDefSendToRot4AtLevel;
            canGetFroms[typeof(Building_BeltSplitter)] = Building_BeltSplitter.CanDefReceiveFromRot4AtLevel;
        }
        protected static Dictionary<Type, Func<ThingDef, Rot4, Rot4, ConveyorLevel, bool>>
               canSendTos = new Dictionary<Type, Func<ThingDef, Rot4, Rot4, ConveyorLevel, bool>>();
        protected static Dictionary<Type, Func<ThingDef, Rot4, Rot4, ConveyorLevel, bool>>
               canGetFroms = new Dictionary<Type, Func<ThingDef, Rot4, Rot4, ConveyorLevel, bool>>();

    }
}
