using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /// <summary>
    /// Patch to ensure Items in Cold Storage Contribute to Wealth
    /// 1k Items ~ 1ms (every 5k Ticks)
    /// </summary>
    class Patch_WealthWatcher_CalculateWealthItems
    {

        public static void Postfix(Verse.Map ___map, ref float __result)
        {
            var buildings = PatchStorageUtil.GetPRFMapComponent(___map).ColdStorageBuildings;
            var cnt = buildings.Count;
            for(int i = 0; i < cnt; i++)
            {
                var building = buildings[i];
                __result += building.GetItemWealth();
            }

        }
    }
}
