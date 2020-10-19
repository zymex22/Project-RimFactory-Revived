using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool {
#if false
    /// <summary>
    /// Note: This works okay for some graphics, but fails when the
    ///   atlas has enough detail: the Link2 class doesn't have any
    ///   buffer between bits of the atlas, and the edges can bleed
    ///   into each other. It can lead to thin-line artifacts along
    ///   edges.  Prefer to use Graphic_LinkedConveyorV2.
    /// </summary>
    [StaticConstructorOnStartup]
    public class Graphic_LinkedConveyor : Graphic_Link2<Graphic_LinkedConveyor> {

        public static Material arrow00; // initialized in the static constructor
        public static Material arrow01;

        public Graphic_LinkedConveyor() : base() {
        }

        public override void Init(GraphicRequest req) {
            base.Init(req);
        }



        public override bool ShouldLinkWith(Rot4 dir, Thing parent) {
            IntVec3 c = parent.Position + dir.FacingCell;
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
            Material mat = this.LinkedDrawMatFrom(thing, thing.Position);
            float extraRotation = 0f;
            if (mat == subMats[(int)LinkDirections.None]) {
                // The graphic for no links is up/down.  But
                //   if the conveyor is oriented East/West, 
                //   we should draw the singleton conveyor
                //   rotated to match
                extraRotation = thing.Rotation.AsAngle;
            }
            if (thing is Blueprint) {
                mat = FadedMaterialPool.FadedVersionOf(mat, 0.5f);
            }
            // Print the conveyor material:
            Printer_Plane.PrintPlane(layer, thing.TrueCenter(),
                this.drawSize, mat, extraRotation);
            // Print the tiny yellow arrow showing direction:
            Printer_Plane.PrintPlane(layer, thing.TrueCenter()
                + new Vector3(0, 0.1f, 0), this.drawSize, arrow00,
                thing.Rotation.AsAngle);
        }
        static Graphic_LinkedConveyor() {
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
#endif
}
