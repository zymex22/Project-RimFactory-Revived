using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Threading.Tasks;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    
    public interface IAssemblerQueue
    {
        List<Thing> GetThingQueue();
    }


    [HarmonyPatch(typeof(ResourceCounter), "UpdateResourceCounts")]
    class Patch_UpdateResourceCounts_AssemblerQueue
    {

        static void Postfix(ResourceCounter __instance, Dictionary<ThingDef, int> ___countedAmounts)
        {
            int i = 0;
            for (i = 0; i < PRFGameComponent.AssemblerQueue.Count; i++)
            {
                foreach (Thing heldThing in PRFGameComponent.AssemblerQueue[i].GetThingQueue())
                {
                    Thing innerIfMinified = heldThing.GetInnerIfMinified();
                    ___countedAmounts[innerIfMinified.def] += innerIfMinified.stackCount;
                }
            }
        }
    }




}
