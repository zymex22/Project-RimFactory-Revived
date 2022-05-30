using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;
using ProjectRimFactory.Drones;
using UnityEngine;

namespace ProjectRimFactory.Common
{
    class ModExtension_Skills : DefModExtension , IXMLThingDescription
	{
        public enum enum_ModExtension_SkillsskillUsage
		{
			ThisOverrides = 0,
			ReserchIsCapping = 1,
			ThisIsCapping = 2,
			ReserchOverrides = 3
        }

		public enum_ModExtension_SkillsskillUsage SkillUsage = enum_ModExtension_SkillsskillUsage.ReserchOverrides;
        public int BaseSkill = 0; //The Base levelInt to uses in case another one is not specified
        public List<SkillRecord> skills = null; //List of specific Skill levelInts
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
			int returnval = 0;
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

        public string GetDescription(ThingDef def)
        {

			string text = "";


            switch (this.SkillUsage)
            {
                case enum_ModExtension_SkillsskillUsage.ThisOverrides:
					text += "PRF_UTD_ModExtension_Skills_ThisOverrides".Translate();
					break;
                case enum_ModExtension_SkillsskillUsage.ReserchIsCapping:
					text += "PRF_UTD_ModExtension_Skills_ReserchIsCapping".Translate();
					break;
                case enum_ModExtension_SkillsskillUsage.ThisIsCapping:
					text += "PRF_UTD_ModExtension_Skills_ThisIsCapping".Translate();
					break;
                case enum_ModExtension_SkillsskillUsage.ReserchOverrides:
					//Return early as it is entirely defined by Reserch
					text += "PRF_UTD_ModExtension_Skills_ReserchOverrides".Translate();
					return text;
            }

			text += "PRF_UTD_ModExtension_Skills_Base".Translate(BaseSkill);
			
			if (skills != null)
            {
				text += "\r\n";
				foreach (SkillRecord skillRecord in skills)
				{
					text += $"{skillRecord.def.LabelCap}: {skillRecord.levelInt}";
					text += "\r\n";
				}
			}

			return text;
        }
    }


}
