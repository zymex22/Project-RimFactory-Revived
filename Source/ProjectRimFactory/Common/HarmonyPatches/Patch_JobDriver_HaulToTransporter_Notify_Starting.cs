using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch(typeof(JobDriver_HaulToTransporter), "Notify_Starting")]
    class Patch_JobDriver_HaulToTransporter_Notify_Starting
    {

        public static void Postfix(JobDriver_HaulToTransporter __instance)
        {
            var pawnPos = __instance.pawn.Position;
            var thingPos = __instance.job.targetA.Cell;
            var transporterPos = __instance.job.targetB.Cell;
            
            var thingDist = AdvancedIO_PatchHelper.CalculatePath(pawnPos, thingPos, transporterPos);
            
            var closest = AdvancedIO_PatchHelper.GetClosestPort(__instance.pawn.Map, pawnPos, transporterPos, __instance.job.targetA.Thing, thingDist);
            var closestPort = closest.Value;

            if (closestPort is null) return;

            __instance.job.targetA.Thing.Position = closestPort.Position;

        }
    }
}
