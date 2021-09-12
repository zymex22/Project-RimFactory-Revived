using System;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;
using ProjectRimFactory.SAL3.Things;
using ProjectRimFactory.Storage;

namespace ProjectRimFactory.Storage.UI
{
    public class Dialog_OutputMinMax : Window
    {
        protected OutputSettings outputSettings;
        private const float TitleLabelHeight = 32f;
        string minBufferString, maxBufferString;

        public Dialog_OutputMinMax(OutputSettings settings, Action postClose = null)
        {
            outputSettings = settings;
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
            drawShadow = true;
            focusWhenOpened = true;
            forcePause = true;
            this.postClose = postClose;
        }

        private Action postClose;
        
        public override Vector2 InitialSize
        {
            get { return new Vector2(500f, 250f); }
        }

        public override void DoWindowContents(Rect rect)
        {
            if (maxBufferString == null)
            {
                maxBufferString = outputSettings.max.ToString();
            }
            if (minBufferString == null)
            {
                minBufferString = outputSettings.min.ToString();
            }
            Listing_Standard list = new Listing_Standard(GameFont.Small)
            {
                ColumnWidth = rect.width
            };
            list.Begin(rect);
            var titleRect = new Rect(0f, 0f, rect.width, TitleLabelHeight);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "SmartHopper_SetTargetAmount".Translate());
            Text.Font = GameFont.Small;
            list.Gap();
            list.Gap();
            list.Gap();
            list.CheckboxLabeled("SmartHopper_Minimum_Label".Translate(), ref outputSettings.useMin, outputSettings.minTooltip.Translate());
            list.Gap();
            {
                Rect rectLine = list.GetRect(Text.LineHeight);
                Rect rectLeft = rectLine.LeftHalf().Rounded();
                Rect rectRight = rectLine.RightHalf().Rounded();
                TextAnchor anchorBuffer = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.DrawHighlightIfMouseover(rectLine);
                Widgets.Label(rectLeft, "SmartHopper_MinimumKeyword".Translate());
                Text.Anchor = anchorBuffer;
                Widgets.TextFieldNumeric(rectRight, ref outputSettings.min, ref minBufferString, 0);
            }
            if (outputSettings.max < outputSettings.min)
            {
                maxBufferString = outputSettings.min.ToString();
            }
            list.Gap();
            list.CheckboxLabeled("SmartHopper_Maximum_Label".Translate(), ref outputSettings.useMax, outputSettings.maxTooltip.Translate());
            list.Gap();
            {
                Rect rectLine = list.GetRect(Text.LineHeight);
                Rect rectLeft = rectLine.LeftHalf().Rounded();
                Rect rectRight = rectLine.RightHalf().Rounded();
                TextAnchor anchorBuffer = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.DrawHighlightIfMouseover(rectLine);
                Widgets.Label(rectLeft, "SmartHopper_MaximumKeyword".Translate());
                Text.Anchor = anchorBuffer;
                Widgets.TextFieldNumeric(rectRight, ref outputSettings.max, ref maxBufferString, 0);
            }
            if (outputSettings.min > outputSettings.max)
            { 
                minBufferString = outputSettings.max.ToString();
            }
            list.End();
        }

        public override void PostClose()
        {
            base.PostClose();
            this.postClose?.Invoke();
        }
    }
}
