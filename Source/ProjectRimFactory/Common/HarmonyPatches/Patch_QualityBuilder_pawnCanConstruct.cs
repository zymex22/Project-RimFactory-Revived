using ProjectRimFactory.Drones;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    class Patch_QualityBuilder_PawnCanConstruct
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
