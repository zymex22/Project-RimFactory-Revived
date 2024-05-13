using HarmonyLib;
using ProjectRimFactory.AutoMachineTool;
using System.Linq;
using Verse;
using Verse.AI;

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
                    var thing = target.Thing;
                    if (thing != null && thing is Pawn)
                    {
                        // SAL do not work on Pawns
                        return;
                    }
                    var interact = thing.InteractionCell;
                    if (interact == null)
                    {
                        return;
                    }
                    var thinglist = interact.GetThingList(p.Map);
                    
                    Building_AutoMachineTool building = (Building_AutoMachineTool)thinglist.Where(t => t is Building_AutoMachineTool).FirstOrDefault();
                    if (building != null)
                    {
                        __result = false;
                    }
                }
            }
        }
    }
}
