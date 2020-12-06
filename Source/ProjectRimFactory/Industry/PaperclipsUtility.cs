using RimWorld;
using Verse;

namespace ProjectRimFactory.Industry
{
    public static class PaperclipsUtility
    {
        public static float PaperclipAmount(this Thing t)
        {
            return t.GetStatValue(StatDefOf.Mass) * t.stackCount * 1000;
        }

        public static float PaperclipAmount(this ThingDef tDef, ThingDef stuff = null)
        {
            return StatDefOf.Mass.Worker.GetValue(StatRequest.For(tDef, stuff)) * 1000;
        }
    }
}