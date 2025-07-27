using HarmonyLib;
using ProjectRimFactory.AutoMachineTool;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //Update The Ticks used by Auto Machine Tool
    //if the Debug Method "DebugSetTicksGame" is Used see #726
    [HarmonyPatch(typeof(TickManager), "DebugSetTicksGame")]
    public class Patch_TickManager_DebugSetTicksGame
    {
        public static bool Prefix(int newTicksGame)
        {
            var maps = Current.Game.Maps;
            foreach (var map in maps)
            {
                map.GetComponent<MapTickManager>().HandleTimeSkip(newTicksGame);
            }
            return true;
        }
    }
}
