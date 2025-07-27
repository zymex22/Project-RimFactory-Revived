using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    class Building_BeltConveyorUGConnector : Building_BeltConveyor, IBeltConveyorLinkable
    {
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();

            if (State != WorkingState.Ready && Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                var p = CarryPosition();
                if (!ToUnderground || WorkLeft > 0.7f)
                {
                    Vector2 result = Find.Camera.WorldToScreenPoint(p + new Vector3(0, 0, -0.4f)) / Prefs.UIScale;
                    result.y = (float)UI.screenHeight - result.y;
                    GenMapUI.DrawThingLabel(result, CarryingThing().stackCount.ToStringCached(), GenMapUI.DefaultThingLabelColor);
                }
            }
        }
        public override void DrawCarried()
        {
            if (!ToUnderground ||
                (IsStuck && WorkLeft < 0.05f) ||
                (!IsStuck && WorkLeft > 0.85f))
            {
                base.DrawCarried();
            }
        }

        //TODO: Decide if we allow this.
        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            if (ToUnderground) return base.TryStartWorking(out target, out workAmount);
            target = null;
            workAmount = 1f;
            return false;
        }

        public bool ToUnderground => modExtension_Conveyor.toUnderground;

        public override bool CanSendToLevel(ConveyorLevel level)
        {
            if (ToUnderground)
            {
                if (level == ConveyorLevel.Underground) return true;
            }
            else // this is exit to surface:
                if (level == ConveyorLevel.Ground) return true;
            return false;
        }
        public override bool CanReceiveFromLevel(ConveyorLevel level)
        {
            if (ToUnderground)
            {
                if (level == ConveyorLevel.Ground) return true;
            }
            else // this is exit to surface:
                if (level == ConveyorLevel.Underground) return true;
            return false;
        }
        public override bool CanLinkTo(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition = true)
        {
            if (ToUnderground)
            {
                if (!otherBeltLinkable.CanReceiveFromLevel(ConveyorLevel.Underground)) return false;
            }
            else
            { // exit to surface:
                if (!otherBeltLinkable.CanReceiveFromLevel(ConveyorLevel.Ground)) return false;
            }
            if (!checkPosition) return true;
            // Connectors can only link to something directly in front of them:
            return (Position + Rotation.FacingCell == otherBeltLinkable.Position);
        }

        public new static bool CanDefSendToRot4AtLevel(ThingDef def, Rot4 defRotation,
                     Rot4 queryRotation, ConveyorLevel queryLevel)
        {
            // Not going to error check here: if there's a config error, there will be prominent
            //   red error messages in the log.
            if (queryLevel == ConveyorLevel.Underground)
            {
                if (def.GetModExtension<ModExtension_Conveyor>()?.toUnderground != true)
                    return false;
            }
            else
            { // Ground
                if (def.GetModExtension<ModExtension_Conveyor>()?.toUnderground == true)
                    return false;
            }
            return defRotation == queryRotation;
        }
        public new static bool CanDefReceiveFromRot4AtLevel(ThingDef def, Rot4 defRotation,
                      Rot4 queryRotation, ConveyorLevel queryLevel)
        {
            if ((queryLevel == ConveyorLevel.Ground &&
                 def.GetModExtension<ModExtension_Conveyor>()?.toUnderground == true)
                || (queryLevel == ConveyorLevel.Underground &&
                    def.GetModExtension<ModExtension_Conveyor>()?.toUnderground != true))
                return (defRotation != queryRotation);
            return false;
        }
    }
}
