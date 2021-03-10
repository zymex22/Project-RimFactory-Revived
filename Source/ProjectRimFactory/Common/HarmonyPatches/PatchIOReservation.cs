using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using ProjectRimFactory.Storage;
using UnityEngine;
using HarmonyLib;


namespace ProjectRimFactory.Common.HarmonyPatches
{

    [HarmonyPatch(typeof(Verse.AI.ReservationManager), "Reserve")]
    class Patch_Reservation_Reservation_IO
    {
        static bool Prefix(LocalTargetInfo target ,ref bool __result , Map ___map)
        {
            if (target.HasThing == false && (Building_StorageUnitIOBase)target.Cell.GetThingList(___map).Where(t => t is Building_StorageUnitIOBase).FirstOrDefault() != null)
            {
                __result = true;
                return false;
            }

            return true;


        }
    }
}