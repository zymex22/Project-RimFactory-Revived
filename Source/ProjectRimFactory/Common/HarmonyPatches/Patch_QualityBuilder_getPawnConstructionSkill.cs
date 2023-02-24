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
    internal class Patch_QualityBuilder_getPawnConstructionSkill
    {
        static public bool Prefix(out int __result, Pawn pawn)
        {
            __result = 0;
            if (pawn is Pawn_Drone)
            {
                __result = pawn.skills.GetSkill(SkillDefOf.Construction).Level;
                return false;
            }
            return true;
        }
    }
}
