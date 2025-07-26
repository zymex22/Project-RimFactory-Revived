using ProjectRimFactory.Common;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectRimFactory.Drones
{
    class DroneSkills
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skill">Pawn_SkillTracker To Set</param>
        /// <param name="skillRecords">Skill Record Cache</param>
        /// <param name="modExtensionSkills">Optional ModExtension_Skills</param>
        /// <param name="forceUpdate">Enforce a reload of the Skills</param>
        /// <returns>Cache Output</returns>
        public static List<SkillRecord> UpdateSkills(Pawn_SkillTracker skill, List<SkillRecord> skillRecords, 
            ModExtension_Skills modExtensionSkills = null, bool forceUpdate = false)
        {
            if (skillRecords.Count != 0 && !forceUpdate)
            {
                skill.skills = skillRecords;
                return skill.skills;
            }

            foreach (var record in skill.skills)
            {
                record.passion = Passion.None;
                if (record.xpSinceLastLevel > 1f)
                {
                    record.xpSinceMidnight = 100f;
                    record.xpSinceLastLevel = 100f;
                }
                
                if (modExtensionSkills != null)
                {
                    //Additional Logic 
                    record.levelInt = modExtensionSkills.SkillUsage switch
                    {
                        ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ThisOverrides =>
                            modExtensionSkills.GetSkillLevel(record.def),
                        ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ReserchIsCapping =>
                            Mathf.Clamp(modExtensionSkills.GetSkillLevel(record.def), 0,
                                ReserchSkillModifier.GetResechSkillLevel()),
                        ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ThisIsCapping =>
                            Mathf.Clamp(ReserchSkillModifier.GetResechSkillLevel(), 0,
                                modExtensionSkills.GetSkillLevel(record.def)),
                        ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ReserchOverrides =>
                            ReserchSkillModifier.GetResechSkillLevel(),
                        _ => modExtensionSkills.GetSkillLevel(record.def)
                    };
                    continue;
                }

                record.levelInt = ReserchSkillModifier.GetResechSkillLevel(); //No Settings Found use the Reserch Directly
                
            }

            return skill.skills;
        }

    }
}
