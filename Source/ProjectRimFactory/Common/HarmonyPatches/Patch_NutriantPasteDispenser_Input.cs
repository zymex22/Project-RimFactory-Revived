using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;


namespace ProjectRimFactory.Common.HarmonyPatches
{
    public interface INutrientPasteDispenserInput
    {
        Thing NPDI_Item { get; }
    }


    [HarmonyPatch(typeof(Building_NutrientPasteDispenser), "FindFeedInAnyHopper")]
    class Patch_NutriantPasteDispenser_Input
    {

        static void Postfix(Building_NutrientPasteDispenser __instance, ref Thing __result)
        {
            if (__result == null)
            {
                //loop over Inputs
                for (int i = 0; i < __instance.AdjCellsCardinalInBounds.Count; i++)
                {
                    //Get list of Items
                    INutrientPasteDispenserInput input = (INutrientPasteDispenserInput)__instance.AdjCellsCardinalInBounds[i].GetThingList(__instance.Map).Where(thing => thing is INutrientPasteDispenserInput).FirstOrDefault();
                    if (input == null || input.NPDI_Item == null) continue;
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(input.NPDI_Item.def))
                    {
                        __result = input.NPDI_Item;
                    }
                    if (__result != null) break;
                }
            }
        }



    }

    //HasEnoughFeedstockInHoppers
    [HarmonyPatch(typeof(Building_NutrientPasteDispenser), "HasEnoughFeedstockInHoppers")]
    class Patch_HasEnoughFeedstockInHoppers
    {
        static void Postfix(Building_NutrientPasteDispenser __instance, ref bool __result)
        {
            //Sadly witth the current valilla implementation adding support between mods is difficult.
            //Each mod needs to patch this but cant build on the progress of the other one.
            //
            //If for example Rimfrige supplies 50% of the nutriants then we will never know about it
            //If we want to support the input of other mods we need a patch for them.

            if (__result == false)
            {
                //Support for Rimfrige
                object rimFridgeCache = null;
                if (ProjectRimFactory_ModComponent.ModSupport_RrimFrige_Dispenser)
                {
                    rimFridgeCache = ProjectRimFactory_ModComponent.ModSupport_RrimFridge_GetFridgeCache.Invoke(null, new object[] { (object)__instance.Map });
                }


                float num = 0f;
                for (int i = 0; i < __instance.AdjCellsCardinalInBounds.Count; i++)
                {
                    IntVec3 c = __instance.AdjCellsCardinalInBounds[i];
                    INutrientPasteDispenserInput input = (INutrientPasteDispenserInput)c.GetThingList(__instance.Map).Where(thing => thing is INutrientPasteDispenserInput).FirstOrDefault();
                    if (input != null && input.NPDI_Item != null && Building_NutrientPasteDispenser.IsAcceptableFeedstock(input.NPDI_Item.def))
                    {
                        num += (float)input.NPDI_Item.stackCount * input.NPDI_Item.GetStatValue(StatDefOf.Nutrition);
                    }
                    else
                    {
                        Thing thing = null;
                        Thing thing2 = null;
                        List<Thing> thingList = c.GetThingList(__instance.Map);
                        for (int j = 0; j < thingList.Count; j++)
                        {
                            Thing thing3 = thingList[j];
                            if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(thing3.def))
                            {
                                thing = thing3;
                            }

                            //Support for Rimfrige
                            if (rimFridgeCache != null)
                            {
                                bool isfc = (bool)ProjectRimFactory_ModComponent.ModSupport_RrimFridge_HasFridgeAt.Invoke(rimFridgeCache, new object[] { (object)c });
                                if (isfc)
                                {
                                    thing2 = thing3;
                                }
                            }

                            if (thing3.def == ThingDefOf.Hopper)
                            {
                                thing2 = thing3;
                            }
                        }
                        if (thing != null && thing2 != null)
                        {
                            num += (float)thing.stackCount * thing.GetStatValue(StatDefOf.Nutrition);
                        }

                    }

                    if (num >= __instance.def.building.nutritionCostPerDispense)
                    {
                        __result = true;
                        break;
                    }
                }

            }


        }
    }


}
