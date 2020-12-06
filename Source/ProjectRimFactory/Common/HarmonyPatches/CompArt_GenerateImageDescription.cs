using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectRimFactory
{
    [HarmonyPatch(typeof(CompArt), "GenerateImageDescription")]
    public class CompArt_GenerateImageDescription
    {
        public static bool Prefix(out TaggedString __result, CompArt __instance)
        {
            Thing t = __instance.parent;
            var ss = Current.Game.GetComponent<PRFGameComponent>().specialScupltures;
            if (ss == null) return true;
            var s = ss.FirstOrDefault(x => x.currentInstances != null &&
                                           x.currentInstances.Contains(t));
            if (s == null) return true;
            __result = new TaggedString(s.descKey.Translate());
            return false; // skip vanilla
        }
    }
}