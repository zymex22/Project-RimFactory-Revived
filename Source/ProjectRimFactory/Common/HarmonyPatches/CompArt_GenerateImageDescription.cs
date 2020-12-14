using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using Verse.AI;

namespace ProjectRimFactory {
    /// <summary>
    /// Harmony Patch to make special scupltures have a correct description in game
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.CompArt), "GenerateImageDescription")]
    public class CompArt_GenerateImageDescription {
        static public bool Prefix(out TaggedString __result, CompArt __instance) {
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
