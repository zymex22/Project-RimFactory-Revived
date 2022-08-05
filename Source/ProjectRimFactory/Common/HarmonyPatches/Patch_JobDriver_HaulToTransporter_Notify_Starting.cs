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
			var ThingDist = (pawnpos - __instance.job.targetA.Cell).LengthManhattan;
			var dict = __instance.pawn.Map?.GetComponent<PRFMapComponent>()?.GetadvancedIOLocations?.Where(l => l.Value.CanGetNewItem);
			if (dict == null || dict.Count() == 0) return;
			Building_AdvancedStorageUnitIOPort closestPort = null;
			var mindist = ThingDist;
			foreach (var port in dict)
			{
				var currentDist = (pawnpos - port.Key).LengthManhattan;
				
				if (currentDist < mindist)
				{
					mindist = currentDist;
					closestPort = port.Value;
				}
			}

			if (closestPort is null) return;

			__instance.job.targetA.Thing.Position = closestPort.Position;

		}
    }
}
