using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Archo
{
    public class ResearchMod_Notify : ResearchMod
    {
        // used in XML
        // ReSharper disable once InconsistentNaming
        public string text;
        
        public override void Apply()
        {
            var notificationManager = Current.Game.World.GetComponent<WorldComponent_NotificatonManager>();
            if (notificationManager is null) return;
            if (Find.WindowStack.WindowOfType<Dialog_MessageBox>() == null && !notificationManager.notifiedMessages.Contains(text))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(text));
                notificationManager.notifiedMessages.Add(text);
            }
        }
    }
}
