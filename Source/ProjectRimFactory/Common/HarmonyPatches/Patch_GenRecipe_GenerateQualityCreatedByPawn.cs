using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using ProjectRimFactory.Drones;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches;

[HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn", new Type[] { typeof(Pawn), typeof(SkillDef), typeof(bool) })]
class Patch_GenRecipe_GenerateQualityCreatedByPawn
{
    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once UnusedParameter.Local
    static bool Prefix(ref QualityCategory __result, Pawn pawn, SkillDef relevantSkill , bool consumeInspiration)
    {
        var setQualityDirectly = PatchStorageUtil.Get<ISetQualityDirectly>(pawn.Map, pawn.Position);
        if (setQualityDirectly == null) return true;
        __result = setQualityDirectly.GetQuality(relevantSkill);
        return false;
    }

    //Prevent Biotech(Mech Changes) from interfering with Drone Skills
    // ReSharper disable once ArrangeTypeMemberModifiers
    // ReSharper disable once UnusedMember.Local
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {

        bool foundReplaceMarker = false;
        bool repacedCheck = false;
        int cnt = 0;
        foreach (var instruction in instructions)
        {
            cnt++;
            //Search for IL_0000: ldarg.0 -> Loading the Pawn
            if (instruction.opcode == OpCodes.Ldarg_0 && !repacedCheck)
            {
                repacedCheck = true;
                foundReplaceMarker = true;
                cnt = 0;
            }
            //remove IL_0001: callvirt instance class Verse.RaceProperties Verse.Pawn::get_RaceProps()
            if (instruction.opcode == OpCodes.Callvirt && foundReplaceMarker && cnt == 1)
            {
                continue;
            }
            if (instruction.opcode == OpCodes.Callvirt && foundReplaceMarker && cnt == 2)
            {
                //Replace IL_0006: callvirt instance bool Verse.RaceProperties::get_IsMechanoid()
                //with a call to Patch_GenRecipe_GenerateQualityCreatedByPawn:IsMechanoid(Pawn pawn)
                instruction.operand = AccessTools.Method(
                    typeof(Patch_GenRecipe_GenerateQualityCreatedByPawn),
                    nameof(IsMechanoid), [typeof(Pawn)]);
                foundReplaceMarker = false;
            }


            yield return instruction;
        }

    }

    /// <summary>
    /// if a Pawn is a Mechanoid then it's skills will be ignored by QualityUtility:GenerateQualityCreatedByPawn
    /// This prevents Drones from being detected as Mechanoid
    /// </summary>
    /// <param name="pawn"></param>
    /// <returns></returns>
    public static bool IsMechanoid(Pawn pawn)
    {
        return pawn is not Pawn_Drone && pawn.RaceProps.IsMechanoid;
    }
}

interface ISetQualityDirectly
{
    QualityCategory GetQuality(SkillDef relevantSkill);
}