using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ProjectRimFactory.SAL3.Tools;
using Verse;

namespace ProjectRimFactory.Common
{
    public static class PRF_ChangedDefTracker
    {
        public static List<ThingDef> OriginalThingDefs = [];
        public static List<ResearchProjectDef> OriginalResearchProjectDefs = [];


    }


    class PRF_CustomizeDefs
    {

        public static readonly string[] ExcludeScenario = ["PRF_FactoryEntrepreneur"];
        public static void ToggleLiteMode(bool remove = true)
        {

            string[] excludeListPrimary =
            [
                "PRF_DeepQuarry_mkI", "PRF_DeepQuarry_mkII" , "PRF_DeepQuarry_mkIII" , "PRF_BillTypeMiner_I" ,
                "PRF_AutoCrafterSimple", "PRF_AutoCrafter", "PRF_TheArtMachine", "PRF_TheArtMaster", "PRF_OverclockedAutoAssembler", 
                "PRF_OverclockedAutoAssemblerII", "PRF_SALAutoCooker", "PRF_SALAutoMinerI", "PRF_GodlyCrafter" ,
                "PRF_StoneWorks","PRF_Recycler","PRF_MrTsArtMachine","PRF_MetalRefinery","PRF_PartAssembler","PRF_DroneCultivator_I",
                "PRF_DroneCultivator_II","PRF_DroneCultivator_II_sun","PRF_DroneCultivator_III","PRF_OldTypeCultivator_I",
                "PRF_OldTypeCultivator_Sun","PRF_OldTypeCultivator_Xl","PRF_SelfCookerI","PRF_SelfCookerII","PRF_SelfCookerIII",
                "PRF_MeatGrinder","PRF_FermentingBarrel","PRF_Slaughterhouse","PRF_AssemblerGroup","PRF_RecipeDatabase",
                "PRF_Shearer","PRF_Milker","PRF_GenericAnimalHarvester","PRF_GenericAnimalHarvester_II",
                "PRF_Sprinkler_I","PRF_Sprinkler_II","PRF_MineShaft","PRF_MiniHelper","PRF_MiniDroneColumn","PRF_TypeOneAssembler_I",
                "PRF_TypeTwoAssembler_I","PRF_TypeTwoAssembler_II","PRF_TypeTwoAssembler_III","PRF_FurnaceI","PRF_FurnaceII",
                "PRF_FurnaceIII","PRF_SelfPrepper",
                "PRF_Factory_Supplier", "PRF_ResearchTerminal" , "TableRoboticMachining", "PRF_4k_Battery","PRF_16k_Battery",
                "PRF_64k_Battery","PRF_256k_Battery"
            ];
            string[] excludeListSigns = ["PRF_FloorLampArrow", "PRF_RedFloorLampArrow", "PRF_GreenFloorLampArrow", 
                "PRF_FloorLampX", "PRF_FloorInput", "PRF_FloorOutput", "PRF_IconClothes", "PRF_IconSkull", "PRF_IconToxic", 
                "PRF_IconPower", "PRF_IconGears", "PRF_IconGun", "PRF_IconGasmask", "PRF_IconFire", "PRF_IconCold", 
                "PRF_IconDanger", "PRF_IconExit", "PRF_IconPrison", "PRF_IconResearch", "PRF_IconHospital", "PRF_IconBarbedWire"
            ];
            string[] excludeListPatches = ["PRF_MiniHelperAtomic", "PRF_MiniHelperFishing", "PRF_T1_Aquaculture", 
                "PRF_T2_Aquaculture", "PRF_FishFood"
            ];

            string[] excludeListRecipes = ["Make_PRF_FishFood"];

            string[] excludeListResearch =
            [
                "PRF_AutomaticFarmingI", "PRF_AutomaticFarmingII", "PRF_AutomaticFarmingIII", 
                "PRF_BasicDrones", "PRF_ImprovedDrones", "PRF_AdvancedDrones", "PRF_AutonomousMining", "PRF_AutonomousMiningII", 
                "PRF_AutonomousMiningIII", "PRF_SALResearchI", "PRF_SALResearchII", "PRF_SALResearchIII", "PRF_SALResearchIV", 
                "PRF_SALResearchV", "PRF_SALResearchVII", "PRF_SALResearchVI", "PRF_SALResearchVIII", "PRF_SALGodlyCrafting",
                "PRF_EnhancedBatteries", "PRF_LargeBatteries", "PRF_VeryLargeBatteries", "PRF_UniversalAutocrafting", 
                "PRF_SelfCorrectingAssemblers", "PRF_SelfCorrectingAssemblersII", "PRF_MetalRefining", "PRF_AnimalStations", 
                "PRF_AnimalStationsII", "PRF_AnimalStationsIII" ,
            "PRF_SelfCooking","PRF_SelfCookingII","PRF_MachineLearning","PRF_MagneticTape","PRF_CoreTierO","PRF_CoreTierI",
            "PRF_CoreTierII","PRF_CoreTierIII","PRF_CoreTierIV"
            ];
            string[] excludeListTrader = ["PRF_Factory_Supplier"];



            //Components are items that are used in the creation of other items
            string[] removeListComponents =
            [
                "PRF_RoboticArm", "PRF_ElectronicChip_I", "PRF_ElectronicChip_II", 
                "PRF_ElectronicChip_III", "PRF_DroneModule", "PRF_DataDisk", "PRF_MachineFrame_I", "PRF_MachineFrame_II", "PRF_MachineFrame_III"
            ];


            if (remove)
            {
                List<string> toRemove = excludeListPrimary.ToList();
                toRemove.AddRange(excludeListSigns.ToList());
                toRemove.AddRange(excludeListPatches.ToList());
                //Remove stuff to Build
                RemoveBuildableThingDefs(toRemove);

                //Remove Stuff to Research
                RemoveResearch(excludeListResearch.ToList());

                //Remove Components used to build stuff
                RemoveComponents(removeListComponents.ToList());

                //remove the Trader
                RemoveTrader(excludeListTrader.ToList());

                //Remove Scenario
                RemoveScenario(ExcludeScenario.ToList());

                RemoveRecipe(excludeListRecipes.ToList());
            }
            else
            {
                foreach (var def in ProjectRimFactory_ModSettings.defTracker.GetAllWithKeylet<ResearchProjectDef>("res"))
                {
                    DefDatabase<ResearchProjectDef>.AllDefsListForReading.Add(def);
                }
                //prerequisitesBase
                foreach (var def in ProjectRimFactory_ModSettings.defTracker.GetAllWithKeylet<ResearchProjectDef>("prerequisitesBase"))
                {
                    DefDatabase<ResearchProjectDef>.AllDefsListForReading[DefDatabase<ResearchProjectDef>.AllDefsListForReading.FirstIndexOf(c => c.defName == def.defName)].prerequisites = ProjectRimFactory_ModSettings.defTracker.GetDefaultValue<List<ResearchProjectDef>>(def.defName, "prerequisites");

                }
                //requiredResearchFacilities
                foreach (var def in ProjectRimFactory_ModSettings.defTracker.GetAllWithKeylet<ResearchProjectDef>("requiredResearchFacilitiesBase"))
                {
                    DefDatabase<ResearchProjectDef>.AllDefsListForReading[DefDatabase<ResearchProjectDef>.AllDefsListForReading.FirstIndexOf(c => c.defName == def.defName)].requiredResearchFacilities = ProjectRimFactory_ModSettings.defTracker.GetDefaultValue<List<ThingDef>>(def.defName, "requiredResearchFacilities");

                }


                foreach (var def in ProjectRimFactory_ModSettings.defTracker.GetAllWithKeylet<ThingDef>("def"))
                {
                    DefDatabase<ThingDef>.AllDefsListForReading.Add(def);
                }


                foreach (var def in ProjectRimFactory_ModSettings.defTracker.GetAllWithKeylet<ThingDef>("alterdDef"))
                {
                    DefDatabase<ThingDef>.AllDefsListForReading[DefDatabase<ThingDef>.AllDefsListForReading.FirstIndexOf(c => c.defName == def.defName)].costList = ProjectRimFactory_ModSettings.defTracker.GetDefaultValue<List<ThingDefCountClass>>(def.defName, "costList");

                }

                foreach (var def in ProjectRimFactory_ModSettings.defTracker.GetAllWithKeylet<TraderKindDef>("trader"))
                {
                    DefDatabase<TraderKindDef>.AllDefsListForReading.Add(def);
                }

                foreach (var def in ProjectRimFactory_ModSettings.defTracker.GetAllWithKeylet<ScenarioDef>("scenario"))
                {
                    DefDatabase<ScenarioDef>.AllDefsListForReading.Add(def);
                }

                foreach (var def in ProjectRimFactory_ModSettings.defTracker.GetAllWithKeylet<RecipeDef>("RecipeDef"))
                {
                    DefDatabase<RecipeDef>.AllDefsListForReading.Add(def);
                }
                ClearRecipesCache();


                ArchitectMenu_ClearCache();

                ModSupportResetLayout();
            }




        }

        private static void ModSupportResetLayout()
        {
            if (ProjectRimFactory_ModComponent.ModSupport_ReserchPal)
            {
                ProjectRimFactory_ModComponent.ModSupport_ReserchPal_ResetLayout.Invoke(null, null);
            }
            else if (ProjectRimFactory_ModComponent.ModSupport_ReserchPowl)
            {
                ProjectRimFactory_ModComponent.ModSupport_ReserchPowl_ResetLayout.Invoke(null, null);
            }
            else if (ProjectRimFactory_ModComponent.ModSupport_ResearchTreeContinued)
            {
                ProjectRimFactory_ModComponent.ModSupport_ResearchTreeContinued_ResetLayout.Invoke(null, [true]);
            }
        }

        private static void ClearRecipesCache()
        {
            var thingDefs = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsWorkTable).ToList();
            foreach (var thingDef in thingDefs)
            {
                ReflectionUtility.AllRecipesCached.SetValue(thingDef, null);
            }
        }

        private static void RemoveRecipe(List<string> defNames)
        {
            var recipeDefs = DefDatabase<RecipeDef>.AllDefsListForReading.Where(d => defNames.Contains(d.defName)).ToList();
            foreach (var recipe in recipeDefs)
            {
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(recipe.defName, "RecipeDef", recipe);
            }
            DefDatabase<RecipeDef>.AllDefsListForReading.RemoveAll(d => defNames.Contains(d.defName));
            ClearRecipesCache();
        }

        private static void RemoveTrader(List<string> defNames)
        {
            var traderKindDef = DefDatabase<TraderKindDef>.AllDefsListForReading.Where(d => defNames.Contains(d.defName)).ToList();
            foreach (var traderKind in traderKindDef)
            {
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(traderKind.defName, "trader", traderKind);
            }
            DefDatabase<TraderKindDef>.AllDefsListForReading.RemoveAll(d => defNames.Contains(d.defName));
        }

        public static void RemoveScenario(List<string> defNames)
        {
            var scenarioDef = DefDatabase<ScenarioDef>.AllDefsListForReading.Where(d => defNames.Contains(d.defName)).ToList();
            foreach (var scenario in scenarioDef)
            {
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(scenario.defName, "scenario", scenario);
            }
            DefDatabase<ScenarioDef>.AllDefsListForReading.RemoveAll(d => defNames.Contains(d.defName));
        }
        private static void RemoveResearch(List<string> defNames)
        {
            var researchProjectsPre = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(d => d?.prerequisites?.Where(c => defNames.Contains(c.defName)).Any() ?? false).ToList();
            foreach (var def in researchProjectsPre)
            {
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(def.defName, "prerequisitesBase", def);
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(def.defName, "prerequisites", def.prerequisites.ToList());
                //PRF_ChangedDefTracker.OriginalResearchProjectDefs.Add(def);
                def.prerequisites.RemoveAll(c => defNames.Contains(c.defName));
            }

            var researchProjects = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(d => defNames.Contains(d.defName)).ToList();
            foreach (var def in researchProjects)
            {
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(def.defName, "res", def);
                //PRF_ChangedDefTracker.OriginalResearchProjectDefs.Add(def);


                RemoveDefFromUse(def);
            }

            //TODO remove the Hardcoded Def
            var researchProjectsFacilities = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(d => d.requiredResearchFacilities?.Any(f => f.defName == "PRF_ResearchTerminal") ?? false).ToList();
            foreach (var def in researchProjectsFacilities)
            {
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(def.defName, "requiredResearchFacilities", def.requiredResearchFacilities.ToList());
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(def.defName, "requiredResearchFacilitiesBase", def);


                def.requiredResearchFacilities.RemoveAll(d => d.defName == "PRF_ResearchTerminal");
            }


            resetResearchManager_progress();
        }

        private static void resetResearchManager_progress()
        {
            var progress = new Dictionary<ResearchProjectDef, float>();
            if (Current.Game?.researchManager != null)
            {
                progress = (Dictionary<ResearchProjectDef, float>)ReflectionUtility.ResearchManagerProgress.GetValue(Current.Game.researchManager);
            }
            var researchProjects = ProjectRimFactory_ModSettings.defTracker.GetAllWithKeylet<ResearchProjectDef>("res").ToList();

            foreach (var def in researchProjects)
            {
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(def.defName, "res", def);

                if (Current.Game?.researchManager != null)
                {
                    progress.Remove(def);
                }
            }
            if (Current.Game?.researchManager != null) ReflectionUtility.ResearchManagerProgress.SetValue(Current.Game.researchManager, progress);

            ModSupportResetLayout();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defNames"></param>
        private static void RemoveBuildableThingDefs(List<string> defNames)
        {
            var thingDefs = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => defNames.Contains(d.defName)).ToList();
            foreach (var def in thingDefs)
            {
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(def.defName, "def", def);
                //PRF_ChangedDefTracker.OriginalThingDefs.Add(def);
                RemoveDefFromUse(def);
            }
            ArchitectMenu_ClearCache();
        }

        /// <summary>
        /// This is needed to Copy a List of ThingDefCountClass by value
        /// </summary>
        /// <param name="datain"></param>
        /// <returns></returns>
        private static List<ThingDefCountClass> CostListCopy(List<ThingDefCountClass> datain)
        {
            var dataOut = new List<ThingDefCountClass>();
            for (var i = 0; i < datain.Count; i++)
            {
                dataOut.Add(new ThingDefCountClass(datain[i].thingDef, datain[i].count));
            }
            return dataOut;



        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defNames"></param>
        private static void RemoveComponents(List<string> defNames)
        {
            var checkRecipeLabel = new Regex(@"^.+? x\d+$", RegexOptions.Compiled);

            var replacementDict = new Dictionary<ThingDef, List<IngredientCount>>();

            var recipeDefs2 = DefDatabase<RecipeDef>.AllDefsListForReading.Where(d => d.products.Select(s => s.thingDef).ToList().Where(c => defNames.Contains(c.defName)).Any()).ToList();
            foreach (var def in recipeDefs2)
            {
                if (checkRecipeLabel.IsMatch(def.label)) continue;
                replacementDict.Add(def.ProducedThingDef, def.ingredients);
            }

            var thingDefsRecipes = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d?.costList?.Any(c => defNames.Contains(c.thingDef.defName)) ?? false).ToList();
            foreach (var def in thingDefsRecipes)
            {
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(def.defName, "alterdDef", def);
                ProjectRimFactory_ModSettings.defTracker.AddDefaultValue(def.defName, "costList", CostListCopy(def.costList));
                var safety = 10;
                while (true)
                {
                    safety--;
                    var toRemove = def.costList.Where(c => defNames.Contains(c.thingDef.defName)).ToList();


                    if (toRemove.Count <= 0 || safety <= 0)
                    {
                        break;
                    }

                    foreach (var thing in toRemove)
                    {
                        var ingredients = replacementDict[thing.thingDef];
                        if (ingredients == null) continue;
                        foreach (var ingredientCount in ingredients)
                        {
                            if (def.costList.Any(d => d.thingDef == ingredientCount.FixedIngredient))
                            {
                                def.costList.First(d => d.thingDef == ingredientCount.FixedIngredient).count
                                    += (int)ingredientCount.GetBaseCount() * thing.count;
                            }
                            else
                            {
                                def.costList.Add(new ThingDefCountClass(ingredientCount.FixedIngredient,
                                    (int)ingredientCount.GetBaseCount() * thing.count));
                            }

                        }
                    }
                    def.costList.RemoveAll(d => toRemove.Contains(d));

                }
            }
            ArchitectMenu_ClearCache();
        }



        /// <summary>
        /// Method created by lilwhitemouse and licenced under GPL-3.0 / LGPL
        /// Original Method: https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/abad0836d3c9764e52ecff1d6efe9f680bd7eb1a/DeepStorage/ModSettings.cs
        /// 
        /// Changes:
        /// Added PRF Specific Code
        /// Code Quality changes
        /// </summary>
        public static void ArchitectMenu_ClearCache()
        {
            string[] panels = ["Industrial", "Power"];

            if ((MainTabWindow_Architect)MainButtonDefOf.Architect?.TabWindow == null) return;

            foreach (var designationCategoryDef in DefDatabase<DesignationCategoryDef>.AllDefs.Where(d => d != null && panels.Contains(d.defName)))
            {
                designationCategoryDef.ResolveReferences();
            }

            // Clear the architect menu cache:
            //   Run the main Architect.TabWindow.CacheDesPanels()

            typeof(MainTabWindow_Architect).GetMethod("CacheDesPanels", System.Reflection.BindingFlags.NonPublic |
                                                                     System.Reflection.BindingFlags.Instance)!
                .Invoke((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow, null);

            //Architect Icons
            if (ModLister.HasActiveModWithName("Architect Icons"))
            {
                var tab = Type.GetType("ArchitectIcons.MainTabWindow_Architect, ArchitectIcons, Version=1.2.0.0, Culture=en, PublicKeyToken=null", false);
                if (tab == null)
                {
                    Log.Warning("Could not get type ArchitectIcons.MainTabWindow_Architect");
                }
                else
                {
                    tab.GetMethod("CacheDesPanels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                        .Invoke(DefDatabase<MainButtonDef>.AllDefs.Where(i => i.defName == "Architect" && i.order == 1).First().TabWindow, null);
                }
            }




        }

        /// <summary>
        /// Method created by lilwhitemouse and licenced under GPL-3.0 / LGPL
        /// Original Method: https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/df276d8183afc3262cfa1eeb4b806983d4e00c7b/DeepStorage/ModSettings_Per_DSU.cs
        /// 
        /// Changes:
        /// Excluded DS specific Logs
        /// spelling
        /// Code Quality changes
        /// </summary>
        /// <param name="def">ThingDef to remove</param>
        private static void RemoveDefFromUse(ThingDef def)
        {
            // Remove from DefDatabase:
            //   equivalent to DefDatabase<DesignationCategoryDef>.Remove(def);
            //                  that's a private method, of course ^^^^^^
            //   #reflection #magic
            typeof(DefDatabase<>).MakeGenericType(typeof(ThingDef))
                    .GetMethod("Remove", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(null, [def]);

            DefDatabase<ThingDef>.AllDefsListForReading.Remove(def);
            var tester = DefDatabase<ThingDef>.GetNamed(def.defName, false);
            if (tester != null) Log.Error("Tried to remove " + def.defName + " from DefDatabase, but it's still there???");
            // remove from architect menu
            //Log.Error("Tried to remove " + def.defName + " and def.designationCategory != null");
            def.designationCategory?.AllResolvedDesignators.RemoveAll(x => ((x is Designator_Build build) &&
                                                                            build.PlacingDef == def));
        }
        /// <summary>
        /// Method created by lilwhitemouse and licenced under GPL-3.0 / LGPL
        /// Original Method: https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/df276d8183afc3262cfa1eeb4b806983d4e00c7b/DeepStorage/ModSettings_Per_DSU.cs
        /// 
        /// Changes:
        /// Excluded DS specific Logs
        /// Changed Type
        /// Code Quality changes
        /// </summary>
        /// <param name="def"></param>
        private static void RemoveDefFromUse(ResearchProjectDef def)
        {
            // Remove from DefDatabase:
            //   equivalent to DefDatabase<ResearchProjectDef>.Remove(def);
            //                  that's a private method, of course ^^^^^^
            //   #reflection #magic
            typeof(DefDatabase<>).MakeGenericType(new Type[] { typeof(ResearchProjectDef) })
                    .GetMethod("Remove", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(null, [def]);

            DefDatabase<ResearchProjectDef>.AllDefsListForReading.Remove(def);
            var tester = DefDatabase<ResearchProjectDef>.GetNamed(def.defName, false);
            if (tester != null) Log.Error("Tried to remove " + def.defName + " from DefDatabase, but it's stil there???");
        }




    }
}
