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
    // ReSharper disable once InconsistentNaming
    class Patch_CanReserve_SAL
    {

        // ReSharper disable once UnusedMember.Local
        static void Postfix(Pawn p, LocalTargetInfo target, bool ignoreOtherReservations, ref bool __result)
        {
            if (__result && ignoreOtherReservations)
            {
                if (target is { HasThing: true, Thing: not null } && p != null && p != PRFGameComponent.PRF_StaticPawn)
                {
                    var thing = target.Thing;
                    if (thing is Pawn)
                    {
                        // SAL do not work on Pawns
                        return;
                    }
                    var interact = thing.InteractionCell;
                    var thingList = interact.GetThingList(p.Map);
                    
                    var building = (Building_AutoMachineTool)thingList.FirstOrDefault(t => t is Building_AutoMachineTool);
                    if (building != null)
                    {
                        __result = false;
                    }
                }
            }
        }
    }
}
