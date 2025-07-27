using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_TraderTracker), "ColonyThingsWillingToBuy")]
    class Patch_Pawn_TraderTracker_ColonyThingsWillingToBuy
    {
        // ReSharper disable once UnusedMember.Local
        static void Postfix(Pawn playerNegotiator, ref IEnumerable<Thing> __result)
        {
            var map = playerNegotiator.Map;
            if (map is null) return;

            var yieldedThings = new HashSet<Thing>();
            yieldedThings.AddRange(__result);
            foreach (var dsu in TradePatchHelper.AllPowered(map))
            {
                //Only for Cold Storage
                if (dsu.AdvancedIOAllowed) continue;

                yieldedThings.AddRange(dsu.StoredItems);
            }
            __result = yieldedThings;

        }

    }
}
