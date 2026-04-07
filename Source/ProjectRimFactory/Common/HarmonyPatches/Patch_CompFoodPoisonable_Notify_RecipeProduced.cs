using System;
using HarmonyLib;
using ProjectRimFactory.SAL3.Things.Assemblers;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    
    [HarmonyPatch(typeof(CompFoodPoisonable), "Notify_RecipeProduced")]
    // ReSharper disable once ClassNeverInstantiated.Global
    class Patch_CompFoodPoisonable_Notify_RecipeProduced
    {
        public static bool Prefix(CompFoodPoisonable __instance,  Pawn pawn)
        {
            if (assemblerReference is null) return true;
            if (Rand.Chance(assemblerReference.FoodPoisonChance))
            {
                __instance.SetPoisoned(FoodPoisonCause.FilthyKitchen);
            }
            return false;
        }

        private static Building_ProgrammableAssembler assemblerReference;

        public class AssemblerRef: IDisposable
        {
            public AssemblerRef(Building_ProgrammableAssembler reference)
            {
                assemblerReference =  reference; 
            }
            public void Dispose()
            {
                assemblerReference = null;
            }
        }
        
    }
    
    
}
