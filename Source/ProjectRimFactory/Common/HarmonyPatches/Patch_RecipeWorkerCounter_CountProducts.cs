using RimWorld;
using Verse;
using HarmonyLib;
using ProjectRimFactory.Storage;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //Art & maybe other things too need a separate patch
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    class Patch_RecipeWorkerCounter_CountProducts
    {
        static void Postfix(RecipeWorkerCounter __instance,ref int __result, Bill_Production bill)
        {
            //Only run if Check everywhere is set
            if (bill.includeFromZone == null) {
                int i = 0;
                ThingDef targetDef = __instance.recipe.products[0].thingDef;



                //Add Items form AssemblerQueue
                PRFGameComponent gamecomp = Current.Game.GetComponent<PRFGameComponent>();
                for (i = 0; i < gamecomp.AssemblerQueue.Count; i++)
                {
                    //Don't count Resources of other maps
                    if (bill.Map != gamecomp.AssemblerQueue[i].Map) continue;
                    foreach (Thing heldThing in gamecomp.AssemblerQueue[i].GetThingQueue())
                    {
                        TryUpdateResult(ref __result, targetDef, heldThing);
                    }
                }

                //Add Items stored in ColdStorage
                foreach (ILinkableStorageParent dsu in TradePatchHelper.AllPowered(bill.Map))
                {
                    //Only for Cold Storage
                    if (dsu.AdvancedIOAllowed) continue;
                    foreach(var thing in dsu.StoredItems)
                    {
                        TryUpdateResult(ref __result, targetDef, thing);
                    }
                }

            }
        }

        private static void TryUpdateResult(ref int __result, ThingDef targetDef, Thing heldThing)
        {
            Thing innerIfMinified = heldThing.GetInnerIfMinified();
            if (innerIfMinified.def == targetDef)
            {
                __result += innerIfMinified.stackCount;
            }
        }
    }
}
