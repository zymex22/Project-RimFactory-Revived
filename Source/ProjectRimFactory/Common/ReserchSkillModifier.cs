using ProjectRimFactory.Drones;
using RimWorld;
using System;

namespace ProjectRimFactory.Common
{
    class ReserchSkillModifier
    {
        private const int ResearchSkillLevelDefault = 10; //Set if no other Reserch takes Effect


        /// <summary>
        /// Check The Research for the appropriate Level to return
        /// </summary>
        /// <returns></returns>
        public static int GetResearchSkillLevel(SkillDef _ = null)
        {
            //Order Matters
            if (PRFDefOf.PRF_AdvancedDrones.IsFinished)
            {
                return 20;//Advanced Version
            }

            if (PRFDefOf.PRF_ImprovedDrones.IsFinished)
            {
                return 15; //Improved Version
            }

            if (PRFDefOf.PRF_BasicDrones.IsFinished)
            {
                return 10; //Basic
            }

            return ResearchSkillLevelDefault;
        }


        public static int GetResearchSkillLevel(Type callerType, SkillDef _ = null)
        {
            //Type.GetType("ProjectRimFactory.Drones.Pawn_Drone, ProjectRimFactory", false)
            if (callerType == typeof(Pawn_Drone) || callerType == typeof(Building_DroneStation))
            {
                //Order Matters
                if (PRFDefOf.PRF_AdvancedDrones.IsFinished)
                {
                    return 20;//Advanced Version
                }

                if (PRFDefOf.PRF_ImprovedDrones.IsFinished)
                {
                    return 15; //Improved Version
                }

                if (PRFDefOf.PRF_BasicDrones.IsFinished)
                {
                    return 10; //Basic
                }
            }
            
            return ResearchSkillLevelDefault;
        }

    }
}
