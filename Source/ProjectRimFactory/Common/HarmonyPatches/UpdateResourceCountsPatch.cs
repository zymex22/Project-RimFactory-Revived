using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Threading.Tasks;
using Verse.Noise;

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

        static void Postfix(ResourceCounter __instance, Dictionary<ThingDef, int> ___countedAmounts, Map ___map )
        {
            int i = 0;
            PRFGameComponent gamecomp = Current.Game.GetComponent<PRFGameComponent>();
            for (i = 0; i < gamecomp.AssemblerQueue.Count; i++)
            {
                //Don't count Recorces of other maps
                if (gamecomp.AssemblerQueue[i].Map != ___map) continue;

                foreach (Thing heldThing in gamecomp.AssemblerQueue[i].GetThingQueue())
                {
                    Thing innerIfMinified = heldThing.GetInnerIfMinified();
                    //Added Should Count Checks
                    //EverStorable is form HeldThings
                    //Fresh Check is from ShouldCount (maybe we can hit that via harmony/reflection somhow)
                    if (innerIfMinified.def.EverStorable(false) && !innerIfMinified.IsNotFresh())
                    {
                        //Causes an error otherwise #345 (seems to be clothing that causes it)
                        if (___countedAmounts.ContainsKey(innerIfMinified.def)){
                            ___countedAmounts[innerIfMinified.def] += innerIfMinified.stackCount;
                        }
                    }
                    
                }
            }
        }
    }




}
