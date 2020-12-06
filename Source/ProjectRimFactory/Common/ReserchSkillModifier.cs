﻿using System;
using ProjectRimFactory.Drones;
using RimWorld;

namespace ProjectRimFactory.Common
{
    internal class ReserchSkillModifier
    {
        private static readonly int reserchSkillLevelDefault = 10; //Set if no other Reserch takes Effect


        /// <summary>
        ///     Check The Resech for the apropriate Level to return
        /// </summary>
        /// <param name="record">Optional for future Updates</param>
        /// <returns></returns>
        public static int GetResechSkillLevel(SkillDef skillDef = null)
        {
            //Order Matters
            if (PRFDefOf.PRF_AdvancedDrones.IsFinished)
                return 20; //Advanced Version
            if (PRFDefOf.PRF_ImprovedDrones.IsFinished)
                return 15; //Improved Version
            if (PRFDefOf.PRF_BasicDrones.IsFinished) return 10; //Basic

            return reserchSkillLevelDefault;
        }


        public static int GetResechSkillLevel(Type callertype, SkillDef skillDef = null)
        {
            //Type.GetType("ProjectRimFactory.Drones.Pawn_Drone, ProjectRimFactory", false)
            if (callertype == typeof(Pawn_Drone) || callertype == typeof(Building_DroneStation))
            {
                //Order Matters
                if (PRFDefOf.PRF_AdvancedDrones.IsFinished)
                    return 20; //Advanced Version
                if (PRFDefOf.PRF_ImprovedDrones.IsFinished)
                    return 15; //Improved Version
                if (PRFDefOf.PRF_BasicDrones.IsFinished) return 10; //Basic
            }


            return reserchSkillLevelDefault;
        }
    }
}