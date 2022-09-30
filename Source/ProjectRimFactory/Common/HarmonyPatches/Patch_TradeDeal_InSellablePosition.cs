 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using HarmonyLib;
using Verse;
using ProjectRimFactory.Storage;
using System.Reflection.Emit;
using System.Reflection;

namespace ProjectRimFactory.Common.HarmonyPatches
{

    [HarmonyPatch(typeof(TradeDeal), "InSellablePosition")]
    class Patch_TradeDeal_InSellablePosition
    {
        public static bool Prefix(Thing t, out string reason, ref bool __result)
        {
            if (!t.Spawned && t.MapHeld != null)
            {
                var buildings = PatchStorageUtil.GetPRFMapComponent(t.MapHeld).ColdStorageBuildings;
                foreach(var building in buildings)
                {
                    if (building.StoredItems.Contains(t))
                    {
                        reason = null;
                        __result = true;
                        return false;
                    }
                }
            }
            else if (t.MapHeld is null)
            {
                Log.Warning($"Report to Rimfactory with HugsLog(CTRL & F12) - TradeDeal InSellablePosition {t}.MapHeld is Null");
            }


            reason = null;
            return true;
        }


    }
}
