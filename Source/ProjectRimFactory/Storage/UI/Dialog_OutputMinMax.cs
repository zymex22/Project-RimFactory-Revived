using System;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage.UI
{
    public class Dialog_OutputMinMax : Window
    {
        static class LimitUpdateRequest
        {
            private static Dialog_OutputMinMax parrent;
            private const int gracePeriodMax = 75;
            private static int gracePeriod = -1;
            static string lastval = "";

            static Predicate<bool> predicate = (d) => false;
            /// <summary>
            /// True  => min = max;
            /// false => max = min;
            /// </summary>
            private static bool minIsMax = false;

            public static void init(Dialog_OutputMinMax par, Predicate<bool> validator)
            {
                predicate = validator;
                parrent = par;
            }

            public static void Update(bool dir, string buff)
            {
                if (gracePeriod == -1 || buff != lastval)
                {
                    gracePeriod = gracePeriodMax;
                }
                else
                {
                    if (dir != minIsMax)
                    {
                        //The Request is for a diffrent direction
                        LostFocus();
                    }
                }
                minIsMax = dir;
                lastval = buff;
            }

            public enum LimitUpdateRequestFocus
            {
                max = 1,
                min = 0,
                undefined = -1
            }
            public static void CheckFocusLoss(LimitUpdateRequestFocus maxFocus)
            {
                if (gracePeriod == -1) return;
                if (maxFocus >= 0 && ((int)maxFocus == 1 && minIsMax || (int)maxFocus == 0 && !minIsMax))
                {
                    //Still have correct focus
                }
                else
                {
                    LostFocus();
                }
            }
            public static void LostFocus(bool force = false)
            {
                if (gracePeriod == -1 && !force) return;
                gracePeriod = 1;
                Tick();
            }

            public static void Tick()
            {
                if (gracePeriod >= 0) gracePeriod--;

                if (gracePeriod == 0 && predicate(minIsMax))
                {
                    parrent?.OverrideBuffer(minIsMax);
                }
            }
        }

        public void OverrideBuffer(bool minIsMax)
        {

            if (minIsMax)
            {
                minBufferString = maxBufferString;
            }
            else
            {
                maxBufferString = minBufferString;
            }
        }

        private string controlIdMinInput = null;
        private string controlIdMaxInput = null;

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

        private bool validator(bool data)
        {
            if (maxBufferString.NullOrEmpty())
            {
                maxBufferString = "0";
                outputSettings.max = 0;
            }
            if (minBufferString.NullOrEmpty())
            {
                minBufferString = "0";
                outputSettings.min = 0;
            }

            return outputSettings.max < outputSettings.min;
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

            var focus = GUI.GetNameOfFocusedControl();
            if (focus == controlIdMaxInput)
            {
                LimitUpdateRequest.CheckFocusLoss(LimitUpdateRequest.LimitUpdateRequestFocus.max);
            }
            else if (focus == controlIdMinInput)
            {
                LimitUpdateRequest.CheckFocusLoss(LimitUpdateRequest.LimitUpdateRequestFocus.min);
            }
            else
            {
                LimitUpdateRequest.CheckFocusLoss(LimitUpdateRequest.LimitUpdateRequestFocus.undefined);
            }

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
                controlIdMinInput ??= "TextField" + rectRight.y.ToString("F0") + rectRight.x.ToString("F0");
            }
            if (outputSettings.max < outputSettings.min && GUI.GetNameOfFocusedControl() == controlIdMinInput)
            {
                LimitUpdateRequest.Update(false, minBufferString);
                //maxBufferString = minBufferString;
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
                controlIdMaxInput ??= "TextField" + rectRight.y.ToString("F0") + rectRight.x.ToString("F0");
            }
            if (outputSettings.min > outputSettings.max && GUI.GetNameOfFocusedControl() == controlIdMaxInput)
            {
                LimitUpdateRequest.Update(true, maxBufferString);
                //minBufferString = maxBufferString;
            }
            LimitUpdateRequest.Tick();




            list.End();
        }

        public override void PostClose()
        {
            LimitUpdateRequest.LostFocus(maxBufferString.NullOrEmpty() || minBufferString.NullOrEmpty());
            base.PostClose();
            this.postClose?.Invoke();
        }

        public override void PreOpen()
        {
            LimitUpdateRequest.init(this, validator);
            base.PreOpen();
        }
    }
}
