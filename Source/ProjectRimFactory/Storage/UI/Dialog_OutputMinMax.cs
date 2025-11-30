using System;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage.UI
{
    public class Dialog_OutputMinMax : Window
    {
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
        
        private static string minBufferString;
        private static string maxBufferString;
        
        private string controlIdMinInput = null;
        private string controlIdMaxInput = null;

        private static OutputSettings outputSettings;
        private const float TitleLabelHeight = 32f;

        private readonly Action postClose;

        public override Vector2 InitialSize => new(500f, 250f);


        private class GracefulInput(Action<int> applyChangeAction)
        {
            private const int GracePeriod = 75;
            private int remainingGracePeriod = -1;

            public void UpdateValue(int value)
            {
                Value = value;
                oldValue = Value;
            }
            
            private bool ValueChanged => Value != oldValue;

            private int oldValue = -1;
            public int Value = -1;
            
            private void ApplyChange()
            {
                remainingGracePeriod = -1;
                applyChangeAction.Invoke(Value);
                Value = -1;
            }
            public void LostFocus()
            {
                if (!ValueChanged)
                {
                    return;
                }
                if (remainingGracePeriod > -1)
                {
                    remainingGracePeriod = 0;
                }

                if (remainingGracePeriod == 0)
                {
                    ApplyChange();
                }
                
            }
            
            public void Tick()
            {
                if (!ValueChanged)
                {
                    return;
                }
                if (remainingGracePeriod == -1)
                {
                    remainingGracePeriod = GracePeriod;
                }
                

                if (remainingGracePeriod > 0)
                {
                    remainingGracePeriod--;
                }
                
                if (remainingGracePeriod == 0)
                {
                    ApplyChange();
                }
            }
            
        }
        
        private readonly GracefulInput inputMaximum = new(i =>
        {
            if (i >= 0)
            {
                outputSettings.Max = i;
                maxBufferString = null;
                minBufferString = null;
            }
        });
        private readonly GracefulInput inputMinimum = new(i =>
        {
            if (i >= 0)
            {
                outputSettings.Min = i;
                maxBufferString = null;
                minBufferString = null;
            }
        });
        
        
        public override void DoWindowContents(Rect rect)
        {
            maxBufferString ??= outputSettings.Max.ToString();
            minBufferString ??= outputSettings.Min.ToString();
            if (inputMaximum.Value == -1)
            {
                inputMaximum.UpdateValue(outputSettings.Max) ;
            } 
            if (inputMinimum.Value == -1)
            {
                inputMinimum.UpdateValue(outputSettings.Min);
            } 
            
            var list = new Listing_Standard(GameFont.Small)
            {
                ColumnWidth = rect.width
            };

            var focus = GUI.GetNameOfFocusedControl();
            if (focus == controlIdMaxInput)
            {
                inputMinimum.LostFocus();
            }
            else if (focus == controlIdMinInput)
            {
                inputMaximum.LostFocus();
            }
            else
            {
                inputMaximum.LostFocus();
                inputMinimum.LostFocus();
            }

            list.Begin(rect);
            var titleRect = new Rect(0f, 0f, rect.width, TitleLabelHeight);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "SmartHopper_SetTargetAmount".Translate());
            Text.Font = GameFont.Small;
            list.Gap();
            list.Gap();
            list.Gap();
            list.CheckboxLabeled("SmartHopper_Minimum_Label".Translate(), ref outputSettings.UseMin, outputSettings.MinTooltip.Translate());
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
                Widgets.TextFieldNumeric(rectRight, ref inputMinimum.Value, ref minBufferString);
                controlIdMinInput ??= "TextField" + rectRight.y.ToString("F0") + rectRight.x.ToString("F0");
            }
            list.Gap();
            list.CheckboxLabeled("SmartHopper_Maximum_Label".Translate(), ref outputSettings.UseMax, outputSettings.MaxTooltip.Translate());
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
                Widgets.TextFieldNumeric(rectRight, ref inputMaximum.Value, ref maxBufferString);
                controlIdMaxInput ??= "TextField" + rectRight.y.ToString("F0") + rectRight.x.ToString("F0");
            }
            
            inputMaximum.Tick();
            inputMinimum.Tick();
            
            list.End();
        }

        public override void PostClose()
        {
            inputMaximum.LostFocus();
            inputMinimum.LostFocus();
            base.PostClose();
            postClose?.Invoke();
        }
    }
}
