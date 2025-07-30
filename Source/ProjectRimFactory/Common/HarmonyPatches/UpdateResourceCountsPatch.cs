using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{

    public interface IAssemblerQueue
    {
        Map Map { get; }
        List<Thing> GetThingQueue();
    }



    [HarmonyPatch(typeof(ResourceCounter), "UpdateResourceCounts")]
    class Patch_UpdateResourceCounts_AssemblerQueue
    {

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedParameter.Local
        // ReSharper disable once UnusedMember.Local
        static void Postfix(ResourceCounter __instance, Dictionary<ThingDef, int> ___countedAmounts, Map ___map)
        {
            var gameComponent = Current.Game.GetComponent<PRFGameComponent>();
            for (var i = 0; i < gameComponent.AssemblerQueue.Count; i++)
            {
                var currentQueue = gameComponent.AssemblerQueue[i];
                
                //Don't count Recorces of other maps
                if (currentQueue.Map != ___map) continue;
                var thingQueue = currentQueue.GetThingQueue();
                foreach (var heldThing in thingQueue)
                {
                    var innerIfMinified = heldThing.GetInnerIfMinified();
                    //Added Should Count Checks
                    //EverStorable is form HeldThings
                    //Fresh Check is from ShouldCount (maybe we can hit that via harmony/reflection somhow)
                    if (innerIfMinified.def.EverStorable(false) && !innerIfMinified.IsNotFresh())
                    {
                        //Causes an error otherwise #345 (seems to be clothing that causes it)
                        if (___countedAmounts.ContainsKey(innerIfMinified.def))
                        {
                            ___countedAmounts[innerIfMinified.def] += innerIfMinified.stackCount;
                        }
                    }
                }
            }
        }
    }




}
