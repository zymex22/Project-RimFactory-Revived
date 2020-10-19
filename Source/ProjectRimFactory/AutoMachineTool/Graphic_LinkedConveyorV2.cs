using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRimFactory.AutoMachineTool {
    [StaticConstructorOnStartup]
    public class Graphic_LinkedConveyorV2 : Verse.Graphic_Linked {
        public static Material arrow00; // initialized in the static constructor
        public static Material arrow01;

        public Graphic_LinkedConveyorV2() : base() {
        }
        public Graphic_LinkedConveyorV2(Graphic subGraphic) : base(subGraphic) {
        }

/*        public override void Init(GraphicRequest req) {
            this.data = req.graphicData;
            this.path = req.path;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
        }*/

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
            Log.Message("Actualy printing for " + thing);
            base.Print(layer, thing);
            // Print the tiny yellow arrow showing direction:
            Printer_Plane.PrintPlane(layer, thing.TrueCenter()
                + new Vector3(0, 0.1f, 0), this.drawSize, arrow00,
                thing.Rotation.AsAngle);
        }

        public override bool ShouldLinkWith(IntVec3 c, Thing parent) {
            if (!c.InBounds(parent.Map)) {
                return false;
            }
            Log.Message("Drawing " + parent + "; does it link with " + c);
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
                    .Any(belt.HasLinkWith);
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

        static Graphic_LinkedConveyorV2() {
            arrow00 = MaterialPool.MatFrom("Belts/SmallArrow00");
            arrow01 = MaterialPool.MatFrom("Belts/SmallArrow01");
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
