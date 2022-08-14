using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			var pawnpos = __instance.pawn.Position;
			var ThingPos = __instance.job.targetA.Cell;
			var TransporterPos = __instance.job.targetB.Cell;


			var ThingDist = AdvancedIO_PatchHelper.CalculatePath(pawnpos, ThingPos, TransporterPos); 
			Building_AdvancedStorageUnitIOPort closestPort = null;
			var mindist = ThingDist;


			var closest = AdvancedIO_PatchHelper.GetClosestPort(__instance.pawn.Map, pawnpos, TransporterPos, __instance.job.targetA.Thing, ThingDist);
			closestPort = closest.Value;

			if (closestPort is null) return;

			__instance.job.targetA.Thing.Position = closestPort.Position;

		}
    }
}
