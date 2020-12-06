using System;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage.UI
{
    public class Dialog_OutputMinMax : Window
    {
        private const float TitleLabelHeight = 32f;
        private string minBufferString, maxBufferString;
        protected OutputSettings outputSettings;

        private readonly Action postClose;

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

        public override Vector2 InitialSize => new Vector2(500f, 250f);

        public override void DoWindowContents(Rect rect)
        {
            if (maxBufferString == null) maxBufferString = outputSettings.max.ToString();
            if (minBufferString == null) minBufferString = outputSettings.min.ToString();
            var list = new Listing_Standard(GameFont.Small)
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
            list.CheckboxLabeled("SmartHopper_Minimum_Label".Translate(), ref outputSettings.useMin,
                outputSettings.minTooltip.Translate());
            list.Gap();
            {
                var rectLine = list.GetRect(Text.LineHeight);
                var rectLeft = rectLine.LeftHalf().Rounded();
                var rectRight = rectLine.RightHalf().Rounded();
                var anchorBuffer = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.DrawHighlightIfMouseover(rectLine);
                Widgets.Label(rectLeft, "SmartHopper_MinimumKeyword".Translate());
                Text.Anchor = anchorBuffer;
                Widgets.TextFieldNumeric(rectRight, ref outputSettings.min, ref minBufferString);
            }
            list.Gap();
            list.CheckboxLabeled("SmartHopper_Maximum_Label".Translate(), ref outputSettings.useMax,
                outputSettings.maxTooltip.Translate());
            list.Gap();
            {
                var rectLine = list.GetRect(Text.LineHeight);
                var rectLeft = rectLine.LeftHalf().Rounded();
                var rectRight = rectLine.RightHalf().Rounded();
                var anchorBuffer = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.DrawHighlightIfMouseover(rectLine);
                Widgets.Label(rectLeft, "SmartHopper_MaximumKeyword".Translate());
                Text.Anchor = anchorBuffer;
                Widgets.TextFieldNumeric(rectRight, ref outputSettings.max, ref maxBufferString);
            }
            list.End();
        }

        public override void PostClose()
        {
            base.PostClose();
            postClose?.Invoke();
        }
    }
}