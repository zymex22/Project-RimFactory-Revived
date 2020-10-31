using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using ProjectRimFactory.Drones;
using ProjectRimFactory.AutoMachineTool;


namespace ProjectRimFactory.Common
{
    class ReserchSkillModifier
    {

		private static int reserchSkillLevelDefault = 10; //Set if no other Reserch takes Effect


		/// <summary>
		/// Check The Resech for the apropriate Level to return
		/// </summary>
		/// <param name="record">Optional for future Updates</param>
		/// <returns></returns>
		public static int GetResechSkillLevel(SkillDef skillDef = null)
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


		public static int GetResechSkillLevel(Type callertype, SkillDef skillDef = null)
		{
			//Type.GetType("ProjectRimFactory.Drones.Pawn_Drone, ProjectRimFactory", false)
			if (callertype == typeof(Pawn_Drone) || callertype == typeof(Building_DroneStation))
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
			}



			return reserchSkillLevelDefault;
		}

	}
}
