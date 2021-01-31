using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using RimWorld;
using Verse.AI;
using ProjectRimFactory.AutoMachineTool;

namespace ProjectRimFactory.Common.HarmonyPatches
{

    /// <summary>
    /// Prevent the Player from Reserving a Startion that has a SAL Setup
    /// </summary>
    [HarmonyPatch(typeof(ReservationUtility), "CanReserve")]
    class Patch_CanReserve_SAL
    {

        static void Postfix(Pawn p, LocalTargetInfo target, bool ignoreOtherReservations, ref bool __result)
        {
            if (__result == true && ignoreOtherReservations == true)
            {
                if (target.HasThing && target.Thing != null && p != null && p != PRFGameComponent.PRF_StaticPawn)
                {
                    Building_AutoMachineTool building = (Building_AutoMachineTool)target.Thing.InteractionCell.GetThingList(p.Map).Where(t => t is Building_AutoMachineTool).FirstOrDefault();
                    if (building != null)
                    {
                        __result = false;
                    }

                }



            }


        }
    }
}
