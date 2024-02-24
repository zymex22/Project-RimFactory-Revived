using HarmonyLib;
using ProjectRimFactory.SAL3.Things.Assemblers;
using RimWorld;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //This Patch ensures that Assemblers using the Recipie DB only allow Recipies to be imported that are available to them
    [HarmonyPatch(typeof(BillStack), "AddBill")]
    class Patch_BillStack_AddBill
    {
        static bool Prefix(BillStack __instance, Bill bill, IBillGiver ___billGiver)
        {
            Building_ProgrammableAssembler assembler = ___billGiver as Building_ProgrammableAssembler;
            if (assembler != null)
            {
                //Is an Assembler
                return assembler.GetAllRecipes().Any(r => bill.recipe == r);
            }
            return true;
        }
    }
}
