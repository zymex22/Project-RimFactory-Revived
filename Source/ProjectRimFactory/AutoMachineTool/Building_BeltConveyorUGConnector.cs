using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;
using ProjectRimFactory;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.AutoMachineTool {
    class Building_BeltConveyorUGConnector : Building_BeltConveyor, IBeltConveyorLinkable {
        public override void DrawGUIOverlay() {
            base.DrawGUIOverlay();

            if (this.State != WorkingState.Ready && Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest) {
                var p = CarryPosition();
                if (!this.ToUnderground || this.WorkLeft > 0.7f) {
                    Vector2 result = Find.Camera.WorldToScreenPoint(p + new Vector3(0, 0, -0.4f)) / Prefs.UIScale;
                    result.y = (float)UI.screenHeight - result.y;
                    GenMapUI.DrawThingLabel(result, this.CarryingThing().stackCount.ToStringCached(), GenMapUI.DefaultThingLabelColor);
                }
            }
        }
        public override void DrawCarried() {
            if (!ToUnderground ||
                (IsStuck && WorkLeft < 0.05f) ||
                (!IsStuck && WorkLeft > 0.85f)) {
                this.CarryingThing().DrawAt(CarryPosition());
            }            
        }

        public override bool AcceptsThing(Thing newThing, IPRF_Building giver = null) {
            Debug.Warning(Debug.Flag.Conveyors, "" + this + " was asked to accept " + newThing);
            if (!IsActive()) return false;
            if (giver is AutoMachineTool.IBeltConveyorLinkable conveyor) {
                if (this.ToUnderground) {
                    if (!conveyor.CanSendToLevel(ConveyorLevel.Ground)) {
                        Debug.Message(Debug.Flag.Conveyors, "  but this starts on the surface");
                        return false;
                    }
                } else // this: underground -> surface
                    if (!conveyor.CanSendToLevel(ConveyorLevel.Underground)) {
                        Debug.Message(Debug.Flag.Conveyors, " but this starts un\tderground");
                        return false;
                }
            }
            if (this.State == WorkingState.Ready) {
                Debug.Message(Debug.Flag.Conveyors, "  taking it.");
                if (newThing.Spawned && this.IsUnderground) newThing.DeSpawn();
                newThing.Position = this.Position;
                this.ForceStartWork(newThing, 1f);
                return true;
            } else {
                Debug.Message(Debug.Flag.Conveyors, "  but busy; trying to absorb.");
                var target = this.State == WorkingState.Working ? this.Working : this.products[0];
                return target.TryAbsorbStack(newThing, true);
            }
        }

        protected override bool PlaceProduct(ref List<Thing> products) {
            // These can only place things in one direction.
            var thing = products[0];
            var next = this.OutputConveyor();
            if (next != null) {
                // コンベアある場合、そっちに流す.
                // If there is a conveyor, let it flow to it
                if (next.AcceptsThing(thing, this)) {
                    this.stuck = false;
                    return true;
                }
            } else {
                if (!this.ToUnderground && this.PRFTryPlaceThing(thing,
                    this.Position + this.Rotation.FacingCell,
                    this.Map)) {
                    this.stuck = false;
                    return true;
                }
            }
            // 配置失敗.
            this.stuck = true;
            return false;
        }

        // TODO: Faster to directly access, or to cache
        private IBeltConveyorLinkable OutputConveyor() {
            return (this.Position + Rotation.FacingCell).GetThingList(this.Map)
                .OfType<IBeltConveyorLinkable>()
                .Where(b => this.CanLinkTo(b))
                .FirstOrDefault(b => b.CanLinkFrom(this));
        }

        protected override bool WorkInterruption(Thing working) {
            return false;
        }

        //TODO: Decide if we allow this.
        protected override bool TryStartWorking(out Thing target, out float workAmount) {
            if (this.ToUnderground) return base.TryStartWorking(out target, out workAmount);
            target = null;
            workAmount = 1f;
            return false;
        }

        public bool ToUnderground { get => this.Extension.toUnderground; }

        public override bool CanSendToLevel(ConveyorLevel level) {
            if (this.ToUnderground) {
                if (level == ConveyorLevel.Underground) return true;
            } else // this is exit to surface:
                if (level == ConveyorLevel.Ground) return true;
            return false;
        }
        public override bool CanReceiveFromLevel(ConveyorLevel level) {
            if (this.ToUnderground) {
                if (level == ConveyorLevel.Ground) return true;
            } else // this is exit to surface:
                if (level == ConveyorLevel.Underground) return true;
            return false;
        }
        public override bool CanLinkTo(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition = true) {
            if (this.ToUnderground) {
                if (!otherBeltLinkable.CanReceiveFromLevel(ConveyorLevel.Underground)) return false;
            } else { // exit to surface:
                if (!otherBeltLinkable.CanReceiveFromLevel(ConveyorLevel.Ground)) return false;
            }
            if (!checkPosition) return true;
            // Connectors can only link to something directly in front of them:
            return (this.Position + this.Rotation.FacingCell == otherBeltLinkable.Position);
        }

        new public static bool CanDefSendToRot4AtLevel(ThingDef def, Rot4 defRotation,
                     Rot4 queryRotation, ConveyorLevel queryLevel) {
            // Not going to error check here: if there's a config error, there will be prominent
            //   red error messages in the log.
            if (queryLevel == ConveyorLevel.Underground) {
                if (def.GetModExtension<ModExtension_Conveyor>()?.toUnderground != true)
                    return false;
            } else { // Ground
                if (def.GetModExtension<ModExtension_Conveyor>()?.toUnderground == true)
                    return false;
            }
            return defRotation == queryRotation;
        }
        new public static bool CanDefReceiveFromRot4AtLevel(ThingDef def, Rot4 defRotation,
                      Rot4 queryRotation, ConveyorLevel queryLevel) {
            if ((queryLevel == ConveyorLevel.Ground &&
                 def.GetModExtension<ModExtension_Conveyor>()?.toUnderground == true)
                || (queryLevel == ConveyorLevel.Underground &&
                    def.GetModExtension<ModExtension_Conveyor>()?.toUnderground != true))
                return (defRotation != queryRotation.Opposite);
            return false;
        }
    }
}
