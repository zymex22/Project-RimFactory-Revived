using HarmonyLib;
using ProjectRimFactory.Storage;
using Verse;
using Verse.AI;


namespace ProjectRimFactory.Common.HarmonyPatches
{

    [HarmonyPatch(typeof(ReservationManager), "Reserve")]
    class Patch_Reservation_Reservation_IO
    {
        // ReSharper disable once UnusedMember.Local
        static bool Prefix(Pawn claimant, Job job, LocalTargetInfo target, ref bool __result, Map ___map)
        {
            if (target.HasThing || ___map is null || !target.Cell.InBounds(___map)) return true;
            
            var buildingTarget = (Building_StorageUnitIOBase)target.Cell.GetThingList(___map).FirstOrDefault(t => t is Building_StorageUnitIOBase);
            if (buildingTarget is { Mode: StorageIOMode.Input })
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}