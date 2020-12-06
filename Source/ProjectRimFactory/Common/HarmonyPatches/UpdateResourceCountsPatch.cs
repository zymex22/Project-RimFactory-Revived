using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    public interface IAssemblerQueue
    {
        List<Thing> GetThingQueue();
    }


    //Art & maybe other things too need a patch for public virtual int CountProducts(Bill_Production bill)

    [HarmonyPatch(typeof(ResourceCounter), "UpdateResourceCounts")]
    internal class Patch_UpdateResourceCounts_AssemblerQueue
    {
        private static void Postfix(ResourceCounter __instance, Dictionary<ThingDef, int> ___countedAmounts)
        {
            var i = 0;
            for (i = 0; i < PRFGameComponent.AssemblerQueue.Count; i++)
                foreach (var heldThing in PRFGameComponent.AssemblerQueue[i].GetThingQueue())
                {
                    var innerIfMinified = heldThing.GetInnerIfMinified();
                    //Added Should Count Checks
                    //EverStorable is form HeldThings
                    //Fresh Check is from ShouldCount (maybe we can hit that via harmony/reflection somhow)
                    if (innerIfMinified.def.EverStorable(false) && !innerIfMinified.IsNotFresh())
                        ___countedAmounts[innerIfMinified.def] += innerIfMinified.stackCount;
                }
        }
    }
}