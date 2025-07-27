using HarmonyLib;
using RimWorld;
using Verse;
// ReSharper disable UnusedMember.Global

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /// <summary>
    /// Harmony Patch to make special sculptures have a correct description in game
    /// </summary>
    [HarmonyPatch(typeof(CompArt), "GenerateImageDescription")]
    // ReSharper disable once UnusedType.Global
    public class CompArt_GenerateImageDescription
    {
        public static bool Prefix(out TaggedString __result, CompArt __instance)
        {
            Thing t = __instance.parent;
            var specialSculptures = Current.Game.GetComponent<PRFGameComponent>().specialScupltures;
            var sculpture = specialSculptures?.FirstOrDefault(x => x.currentInstances != null &&
                                            x.currentInstances.Contains(t));
            if (sculpture == null) return true;
            __result = new TaggedString(sculpture.descKey.Translate());
            return false; // skip vanilla
        }
    }
}
