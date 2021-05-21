using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    abstract class  Window_Confirm : Window
    {
        private string confirmString = "Ok";
        private string abortString = "Cancel";
        private string content_String = "";

        public virtual string Content_String { get => content_String; set => content_String = value; }

        public override Vector2 InitialSize => new Vector2(500f, 500f);

        public override void DoWindowContents(Rect inRect)
        {
            DoWindowContentsCall();
            float margin = 5;
            
            float button_hight = 20;
            float button_width = 80;
            float Confim_ExtraMargin = 15;

            float textArea_Hight = inRect.height - (margin * 2) - (margin + Confim_ExtraMargin) - button_hight;

            Rect rectTextArea = new Rect(margin, margin, inRect.width - margin*2, textArea_Hight);
            Rect rectButton_Confirm = new Rect(margin, margin*2 + Confim_ExtraMargin + textArea_Hight, button_width, button_hight);
            Rect rectButton_Abort = new Rect(inRect.width - margin - button_width, margin*2 + Confim_ExtraMargin + textArea_Hight, button_width, button_hight);

            
            Widgets.TextArea(rectTextArea, content_String,true);

            if(Widgets.ButtonText(rectButton_Confirm, confirmString))
            {
                ConfirmAction();
                Find.WindowStack.TryRemove(this);
            }
            if (Widgets.ButtonText(rectButton_Abort, abortString))
            {
               AbortAction();
                Find.WindowStack.TryRemove(this);
            }

        }

          public abstract void DoWindowContentsCall();
          public abstract void ConfirmAction();
          public abstract void AbortAction();

        public Window_Confirm(string content)
        {
            content_String = content;
        }
        public Window_Confirm(string content, string confirm,string abort)
        {
            content_String = content;
            abortString = abort;
            confirmString = confirm;
        }
    }
}
