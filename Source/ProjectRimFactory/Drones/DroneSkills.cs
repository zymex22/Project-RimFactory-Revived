using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using ProjectRimFactory.Common;
using Verse;
using UnityEngine;

namespace ProjectRimFactory.Drones
{
    class DroneSkills
    {

        private static int reserchSkillLevelDefault = 10; //Set if no other Reserch takes Effect

        /// <summary>
        /// Check The Resech for the apropriate Level to return
        /// </summary>
        /// <param name="record">Optional for future Updates</param>
        /// <returns></returns>
        public static int GetResechSkillLevel(SkillRecord record = null)
        {
            //Order Matters
            if (PRFDefOf.PRF_AdvancedDrones.IsFinished)
            {
                return 20;//Advanced Version
            }
            else if (PRFDefOf.PRF_ImprovedDrones.IsFinished)
            {
                return 15; //Improved Version
            }
            else if (PRFDefOf.PRF_BasicDrones.IsFinished)
            {
                return 10; //Basic
            }
            
            return reserchSkillLevelDefault;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="skill">Pawn_SkillTracker To Set</param>
        /// <param name="skillRecords">Skill Record Cache</param>
        /// <param name="modExtension_Skills">Optional ModExtension_Skills</param>
        /// <param name="forceUpdate">Enforce a reload of the Skills</param>
        public static void UpdateSkills(Pawn_SkillTracker skill , List<SkillRecord> skillRecords, ModExtension_Skills modExtension_Skills = null , bool forceUpdate = false)
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
                                    record.levelInt = modExtension_Skills.GetSkillLevel(record);
                                    break;
                                }
                            case ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ReserchIsCapping:
                                {
                                    record.levelInt = Mathf.Clamp(modExtension_Skills.GetSkillLevel(record), 0, GetResechSkillLevel());
                                    break;
                                }
                            case ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ThisIsCapping:
                                {
                                    record.levelInt = Mathf.Clamp(GetResechSkillLevel(), 0, modExtension_Skills.GetSkillLevel(record));
                                    break;
                                }
                            case ModExtension_Skills.enum_ModExtension_SkillsskillUsage.ReserchOverrides:
                                {
                                    record.levelInt = DroneSkills.GetResechSkillLevel();
                                    break;
                                }
                            default:
                                {
                                    record.levelInt = modExtension_Skills.GetSkillLevel(record);
                                    break;
                                }
                        }

                        
                    }
                    else
                    {
                        record.levelInt = DroneSkills.GetResechSkillLevel(); //No Settings Found use the Reserch Directly
                    }


                    record.passion = Passion.None;
                    if (record.xpSinceLastLevel > 1f)
                    {
                        record.xpSinceMidnight = 100f;
                        record.xpSinceLastLevel = 100f;
                    }
                }

                skillRecords = skill.skills;
            }
            else
            {
                skill.skills = skillRecords;
            }


        }

    }
}
