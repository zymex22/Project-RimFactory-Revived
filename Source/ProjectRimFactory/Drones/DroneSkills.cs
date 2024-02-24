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
        /// <param name="modExtension_Skills">Optional ModExtension_Skills</param>
        /// <param name="forceUpdate">Enforce a reload of the Skills</param>
        /// <returns>Cache Output</returns>
        public static List<SkillRecord> UpdateSkills(Pawn_SkillTracker skill, List<SkillRecord> skillRecords, ModExtension_Skills modExtension_Skills = null, bool forceUpdate = false)
        {
            if (skillRecords.Count == 0 || forceUpdate)
            {

                foreach (SkillRecord record in skill.skills)
                {
                    if (modExtension_Skills != null)
                    {
                        //Additional Logic 
                        switch (modExtension_Skills.SkillUsage)
                        {
                            case ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ThisOverrides:
                                {
                                    record.levelInt = modExtension_Skills.GetSkillLevel(record.def);
                                    break;
                                }
                            case ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ReserchIsCapping:
                                {
                                    record.levelInt = Mathf.Clamp(modExtension_Skills.GetSkillLevel(record.def), 0, ReserchSkillModifier.GetResechSkillLevel());
                                    break;
                                }
                            case ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ThisIsCapping:
                                {
                                    record.levelInt = Mathf.Clamp(ReserchSkillModifier.GetResechSkillLevel(), 0, modExtension_Skills.GetSkillLevel(record.def));
                                    break;
                                }
                            case ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ReserchOverrides:
                                {
                                    record.levelInt = ReserchSkillModifier.GetResechSkillLevel();
                                    break;
                                }
                            default:
                                {
                                    record.levelInt = modExtension_Skills.GetSkillLevel(record.def);
                                    break;
                                }
                        }


                    }
                    else
                    {
                        record.levelInt = ReserchSkillModifier.GetResechSkillLevel(); //No Settings Found use the Reserch Directly
                    }


                    record.passion = Passion.None;
                    if (record.xpSinceLastLevel > 1f)
                    {
                        record.xpSinceMidnight = 100f;
                        record.xpSinceLastLevel = 100f;
                    }
                }
            }
            else
            {
                skill.skills = skillRecords;
            }

            return skill.skills;
        }

    }
}
