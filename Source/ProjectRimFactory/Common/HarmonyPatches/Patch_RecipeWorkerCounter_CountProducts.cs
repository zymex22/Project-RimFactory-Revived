using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /// <summary>
    /// This Patch Counts additional Items for the Do until X Type Bills
    /// Currently adds Items from:
    /// - AssemblerQueue
    /// - Cold STorage
    /// 
    /// Old Note: Art & maybe other things to need a separate patch
    /// </summary>
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    class Patch_RecipeWorkerCounter_CountProducts
    {
        // ReSharper disable once UnusedMember.Local
        static void Postfix(RecipeWorkerCounter __instance, ref int __result, Bill_Production bill)
        {
            //Only run if Check everywhere is set
            // TODO Check if that is the correct replacement
            if (bill.GetIncludeSlotGroup() != null) return;
            
            Map billMap = bill.Map;
            ThingDef targetDef = __instance.recipe.products[0].thingDef;

            //Add Items form AssemblerQueue
            var prfGameComponent = Current.Game.GetComponent<PRFGameComponent>();
            for (var i = 0; i < prfGameComponent.AssemblerQueue.Count; i++)
            {
                //Don't count Resources of other maps
                if (billMap != prfGameComponent.AssemblerQueue[i].Map) continue;
                foreach (var heldThing in prfGameComponent.AssemblerQueue[i].GetThingQueue())
                {
                    TryUpdateResult(ref __result, targetDef, heldThing);
                }
            }

            //Add Items stored in ColdStorage
            var units = PatchStorageUtil.GetPRFMapComponent(billMap).ColdStorageBuildings
                .Select(ILinkableStorageParent (b) => b).ToList();

            foreach (var dsu in units)
            {
                foreach (var thing in dsu.StoredItems)
                {
                    TryUpdateResult(ref __result, targetDef, thing);
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
