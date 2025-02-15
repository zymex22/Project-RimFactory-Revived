using HarmonyLib;
using ProjectRimFactory.SAL3.Things.Assemblers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit; // for OpCodes in Harmony Transpiler
using System.Runtime.CompilerServices;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    
    // Building_SimpleAssembler assembler   -> Use the Room of The output cell instead of the Pawn
    // .def.defName == "PRF_SelfCookerIII"  -> Skip
    [HarmonyPatch(typeof(CompFoodPoisonable), "Notify_RecipeProduced")]
    class Patch_CompFoodPoisonable_Notify_RecipeProduced
    {
        public static bool Prefix(CompFoodPoisonable __instance,  Pawn pawn)
        {
            if (AssemblerRefrence is null) return true;
            if (AssemblerRefrence.def.defName == "PRF_SelfCookerIII") return false;
            if (AssemblerRefrence is not Building_SimpleAssembler) return true;
            if (Rand.Chance(RegionAndRoomQuery.RoomAt(AssemblerRefrence.OutputCell(), 
                        AssemblerRefrence.Map, RegionType.Set_Passable)?.
                    GetStat(RoomStatDefOf.FoodPoisonChance) ?? RoomStatDefOf.FoodPoisonChance.roomlessScore))
            {
                __instance.SetPoisoned(FoodPoisonCause.FilthyKitchen);
            }
            return false;
        }
        
        public static Building_ProgrammableAssembler AssemblerRefrence = null;
    }
}
