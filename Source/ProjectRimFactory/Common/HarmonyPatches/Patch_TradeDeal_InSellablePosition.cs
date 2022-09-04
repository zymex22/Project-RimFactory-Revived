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
            if (!t.Spawned)
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
            
            reason = null;
            return true;
        }


    }
}
