using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    internal class ModExtension_Skills : DefModExtension
    {
        public enum enum_ModExtension_SkillsskillUsage
        {
            ThisOverrides = 0,
            ReserchIsCapping = 1,
            ThisIsCapping = 2,
            ReserchOverrides = 3
        }

        public int BaseSkill = 0; //The Base levelInt to uses in case another one is not specified
        public List<SkillRecord> skills = null; //List of specific Skill levelInts

        public enum_ModExtension_SkillsskillUsage SkillUsage = enum_ModExtension_SkillsskillUsage.ReserchOverrides;
        /* Usage Sample -----------------------------------------------------------------------
         <skills>
            <li>
                <def>Shooting</def>
                <levelInt>5</levelInt>
            </li>
            <li>
                <def>Melee</def>
                <levelInt>5</levelInt>
            </li>
            <li>
                <def>Construction</def>
                <levelInt>5</levelInt>
            </li>
            <li>
                <def>Mining</def>
                <levelInt>5</levelInt>
            </li>
            <li>
                <def>Cooking</def>
                <levelInt>2</levelInt>
            </li>
            <li>
                <def>Plants</def>
                <levelInt>1</levelInt>
            </li>
            <li>
                <def>Animals</def>
                <levelInt>5</levelInt>
            </li>
            <li>
                <def>Crafting</def>
                <levelInt>12</levelInt>
            </li>
            <li>
                <def>Artistic</def>
                <levelInt>5</levelInt>
            </li>
            <li>
                <def>Medicine</def>
                <levelInt>3</levelInt>
            </li>
            <li>
                <def>Social</def>
                <levelInt>6</levelInt>
            </li>
            <li>
                <def>Intellectual</def>
                <levelInt>11</levelInt>
            </li>
        </skills>
        */

        public int GetSkillLevel(SkillDef skillDef)
        {
            return skills.FirstOrDefault(x => x.def == skillDef)?.levelInt ?? BaseSkill;
        }

        public int GetExtendedSkillLevel(SkillDef skillDef, Type type)
        {
            var returnval = 0;
            switch (SkillUsage)
            {
                case enum_ModExtension_SkillsskillUsage.ThisOverrides:
                {
                    returnval = GetSkillLevel(skillDef);
                    break;
                }
                case enum_ModExtension_SkillsskillUsage.ReserchIsCapping:
                {
                    returnval = Mathf.Clamp(GetSkillLevel(skillDef), 0, ReserchSkillModifier.GetResechSkillLevel(type));
                    break;
                }
                case enum_ModExtension_SkillsskillUsage.ThisIsCapping:
                {
                    returnval = Mathf.Clamp(ReserchSkillModifier.GetResechSkillLevel(type), 0, GetSkillLevel(skillDef));
                    break;
                }
                case enum_ModExtension_SkillsskillUsage.ReserchOverrides:
                {
                    returnval = ReserchSkillModifier.GetResechSkillLevel(type);
                    break;
                }
                default:
                {
                    returnval = GetSkillLevel(skillDef);
                    break;
                }
            }

            return returnval;
        }
    }
}