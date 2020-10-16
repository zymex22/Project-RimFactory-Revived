using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ProjectRimFactory.Common
{
    class ModExtension_Skills : DefModExtension
    {
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

		public int GetSkillLevel(SkillRecord record)
        {
			return skills.FirstOrDefault(x => x.def == record.def)?.levelInt ?? BaseSkill;
		}

	}
}
