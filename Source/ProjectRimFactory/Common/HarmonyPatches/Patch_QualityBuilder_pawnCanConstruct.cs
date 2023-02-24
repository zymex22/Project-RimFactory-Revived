using ProjectRimFactory.Drones;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    class Patch_QualityBuilder_pawnCanConstruct
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (!__result && pawn is Pawn_Drone && (pawn.workSettings?.WorkIsActive(WorkTypeDefOf.Construction) ?? false))
            {
                __result = true;
            }
        }
    }
}
