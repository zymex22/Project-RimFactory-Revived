using HarmonyLib;
using ProjectRimFactory.SAL3.Things.Assemblers;
using RimWorld;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    //This Patch ensures that Assemblers using the Recipe DB only allow Recipes to be imported that are available to them
    [HarmonyPatch(typeof(BillStack), "AddBill")]
    class Patch_BillStack_AddBill
    {
        // ReSharper disable once ArrangeTypeMemberModifiers
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedParameter.Local
        static bool Prefix(BillStack __instance, Bill bill, IBillGiver ___billGiver)
        {
            if (___billGiver is Building_ProgrammableAssembler assembler)
            {
                
                Log.Message("BillStack.AddBill called");
                return true;
                //Is an Assembler
                //return assembler.GetDynamicRecipes().Any(r => bill.recipe == r);
            }
            return true;
        }
    }
}
