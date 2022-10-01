using Verse;

namespace ProjectRimFactory.Archo
{
    public class ResearchMod_Notify : ResearchMod
    {
        public string text;
        public override void Apply()
        {
            if (Find.WindowStack.WindowOfType<Dialog_MessageBox>() == null && !Current.Game.World.GetComponent<WorldComponent_NotificatonManager>().notifiedMessages.Contains(text))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(text));
                Current.Game.World.GetComponent<WorldComponent_NotificatonManager>().notifiedMessages.Add(text);
            }
        }
    }
}
