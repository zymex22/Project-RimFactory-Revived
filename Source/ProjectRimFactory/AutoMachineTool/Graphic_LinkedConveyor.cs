using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool {
    public class Graphic_LinkedConveyor : Graphic_Link2<Graphic_LinkedConveyor> {

        private static Material arrow00 => MaterialPool.MatFrom("Belts/SmallArrow00");
        private static Material arrow01 => MaterialPool.MatFrom("Belts/SmallArrow01");

        public Graphic_LinkedConveyor() : base() {
        }


        public override bool ShouldLinkWith(Rot4 dir, Thing parent) {
            IntVec3 c = parent.Position + dir.FacingCell;
            if (!c.InBounds(parent.Map)) {
                return false;
            }
            // hardcoded for conveyors; no other conveyor buildings 
            //   will get this special logic.
            if (parent.GetType() == typeof(Building_BeltConveyor) &&
                parent.Rotation == dir) {
                return true;
            }

            var blueprint = parent as Blueprint;
            if (blueprint == null) {
                var belt = parent as IBeltConveyorLinkable;
                return c.GetThingList(parent.Map)
                    .OfType<IBeltConveyorLinkable>()
                    .Any(belt.HasLinkWith);
            }
            var def = (ThingDef)parent.def.entityDefToBuild;
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
            /* Old approach, which does basically the same thing:
            var cellThing = blueprint ?
                c.GetThingList(parent.Map)
                    .SelectMany(t => Option(t as Blueprint))
                    .Where(b => b.def.entityDefToBuild as ThingDef != null)
                    .Select(b => new { Thing = (Thing)b, Def = (ThingDef)b.def.entityDefToBuild })

                    .Where(b => Building_BeltConveyor.IsBeltConveyorDef(b.Def) || Building_BeltConveyorUGConnector.IsConveyorUGConnecterDef(b.Def))
                    .FirstOption().GetOrDefault(null) :
                c.GetThingList(parent.Map)
                    .SelectMany(t => Option(t as Building))
                    .Select(b => new { Thing = (Thing)b, Def = b.def })
                    .Where(b => Building_BeltConveyor.IsBeltConveyorDef(b.Def) || Building_BeltConveyorUGConnector.IsConveyorUGConnecterDef(b.Def))
                    .FirstOption().GetOrDefault(null);

            if (cellThing == null) {
                return false;
            }
            return Building_BeltConveyor.CanLink(parent, cellThing.Thing, def, cellThing.Def);
            */
        }

        public override bool ShouldDrawRotated {
            get {
                return this.data == null || this.data.drawRotated;
            }
        }

        public override void Print(SectionLayer layer, Thing thing) {
            if (thing is Blueprint) {
                base.Print(layer, thing);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.1f, 0), this.drawSize, arrow00, thing.Rotation.AsAngle);
            } else {
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

                base.Print(layer, thing);
                Printer_Plane.PrintPlane(layer, thing.TrueCenter() + new Vector3(0, 0.1f, 0), this.drawSize, arrow00, thing.Rotation.AsAngle);
                // this prints tiny brown arrows pointing in output directions:
                //   Helpful for splitters:
                if (conveyor != null) {
                    conveyor.ActiveOutputDirections.Where(d => d != thing.Rotation)
                        .ForEach(d => Printer_Plane.PrintPlane(layer, thing.TrueCenter(),
                                 this.drawSize, arrow01, d.AsAngle));
                }
            }
        }
        static Graphic_LinkedConveyor() {
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
