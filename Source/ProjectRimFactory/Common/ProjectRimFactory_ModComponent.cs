using HarmonyLib;
using ProjectRimFactory.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ProjectRimFactory_ModComponent : Mod
    {
        public ProjectRimFactory_ModComponent(ModContentPack content) : base(content)
        {
            try
            {
                ProjectRimFactory_ModSettings.LoadXml(content);
                this.Settings = GetSettings<ProjectRimFactory_ModSettings>();
                this.HarmonyInstance = new Harmony("com.spdskatr.projectrimfactory");
                this.HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message($"Project RimFactory Core {typeof(ProjectRimFactory_ModComponent).Assembly.GetName().Version} - Harmony patches successful");
                availableSpecialSculptures = SpecialSculpture.LoadAvailableSpecialSculptures(content);
                
                ConditionalPatchHelper.InitHarmony(this.HarmonyInstance);
                ConditionalPatchHelper.Patch_Reachability_CanReach.PatchHandler(ProjectRimFactory_ModSettings.PRF_Patch_Reachability_CanReach);
                ConditionalPatchHelper.Patch_WealthWatcher_CalculateWealthItems.PatchHandler(ProjectRimFactory_ModSettings.PRF_Patch_WealthWatcher_CalculateWealthItems);
            }
            catch (Exception ex)
            {
                Log.Error("Project RimFactory Core :: Caught exception: " + ex);
            }

            try
            {
                LoadModSupport();
            }
            catch (Exception ex)
            {
                Log.Error("Project RimFactory Core :: LoadModSupport Caught exception: " + ex);
            }

        }

        //Mod Support
        //Cached MethodInfo as Reflection is Slow
        public static System.Reflection.MethodInfo ModSupport_RrimFridge_GetFridgeCache = null;
        public static System.Reflection.MethodInfo ModSupport_RrimFridge_HasFridgeAt = null;
        public static bool ModSupport_RrimFrige_Dispenser = false;

        public static System.Reflection.MethodInfo ModSupport_ReserchPal_ResetLayout = null;
        public static System.Reflection.MethodInfo ModSupport_ReserchPowl_ResetLayout = null;
        public static bool ModSupport_ReserchPal = false;
        public static bool ModSupport_ReserchPowl = false;

        public static bool ModSupport_SeedsPlease = false;
        public static bool ModSupport_SeedsPleaseLite = false;

        private void LoadModSupport()
        {
            if (ModLister.HasActiveModWithName("RimFridge Updated"))
            {
                ModSupport_RrimFridge_GetFridgeCache = AccessTools.Method("RimFridge.FridgeCache:GetFridgeCache");
                ModSupport_RrimFridge_HasFridgeAt = AccessTools.Method("RimFridge.FridgeCache:HasFridgeAt");
                if (ModSupport_RrimFridge_GetFridgeCache != null && ModSupport_RrimFridge_HasFridgeAt != null)
                {
                    Log.Message("Project Rimfactory - added Support for shared Nutrient Dispenser with RimFridge Updated");
                    ModSupport_RrimFrige_Dispenser = true;
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to add Support for shared Nutrient Dispenser with RimFridge Updated");
                }

                // if "Simple Utilities: Fridge" and "[KV] RimFridge" are loaded we use "Simple Utilities: Fridge" as it is faster
                if (!ModLister.HasActiveModWithName("Simple Utilities: Fridge"))
                {
                    MethodBase RrimFridge_CompRefrigerator_CompTickRare = AccessTools.Method("RimFridge.CompRefrigerator:CompTickRare");

                    if (RrimFridge_CompRefrigerator_CompTickRare != null)
                    {
                        var postfix = typeof(HarmonyPatches.Patch_CompRefrigerator_CompTickRare).GetMethod("Postfix");
                        this.HarmonyInstance.Patch(RrimFridge_CompRefrigerator_CompTickRare, null, new HarmonyMethod(postfix));

                        Log.Message("Project Rimfactory - added Support for Fridge DSU Power using RimFridge");
                    }
                    else
                    {
                        Log.Warning("Project Rimfactory - Failed to add Support for Fridge DSU Power using RimFridge");
                    }
                }



            }
            if (ModLister.HasActiveModWithName("Simple Utilities: Fridge"))
            {

                MethodBase SimpleFridge_FridgeUtility_Tick = null;
                Type FridgeUtility = Type.GetType("SimpleFridge.FridgeUtility, SimpleUtilitiesFridge", false);
                if (FridgeUtility != null)
                {
                    SimpleFridge_FridgeUtility_Tick = AccessTools.Method(FridgeUtility, "Tick");
                }
                if (SimpleFridge_FridgeUtility_Tick != null)
                {
                    var postfix = typeof(HarmonyPatches.Patch_FridgeUtility_Tick).GetMethod("Postfix");
                    this.HarmonyInstance.Patch(SimpleFridge_FridgeUtility_Tick, null, new HarmonyMethod(postfix));

                    Log.Message("Project Rimfactory - added Support for Fridge DSU Power using Simple Utilities: Fridge");
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to add Support for Fridge DSU Power using Simple Utilities: Fridge");
                }
            }
            if (ModLister.HasActiveModWithName("ResearchPal - Forked"))
            {
                ModSupport_ReserchPal_ResetLayout = AccessTools.Method("ResearchPal.Tree:ResetLayout");
                if (ModSupport_ReserchPal_ResetLayout != null)
                {
                    Log.Message("Project Rimfactory - added Support for ResearchPal when using Lite Mode");
                    ModSupport_ReserchPal = true;
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to added Support for ResearchPal when using Lite Mode");
                }
            }
            else if (ModLister.HasActiveModWithName("ResearchPowl"))
            {
                ModSupport_ReserchPowl_ResetLayout = AccessTools.Method("ResearchPowl.Tree:ResetLayout");
                if (ModSupport_ReserchPowl_ResetLayout != null)
                {
                    Log.Message("Project Rimfactory - added Support for ResearchPowl when using Lite Mode");
                    ModSupport_ReserchPowl = true;
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to added Support for ResearchPowl when using Lite Mode");
                }
            }
            if (ModLister.HasActiveModWithName("Locks 2: Lock Them Out!"))
            {
                MethodBase methodBase = null;
                Type ConfigRuleRace = Type.GetType("Locks2.Core.LockConfig, Locks2", false).GetNestedType("ConfigRuleRace");

                if (ConfigRuleRace != null)
                {
                    methodBase = AccessTools.Method(ConfigRuleRace, "Allows");
                }

                if (methodBase != null)
                {
                    var prefix = typeof(HarmonyPatches.Patch_Locks2_ConfigRuleRace_Allows).GetMethod("Prefix");
                    var postfix = typeof(HarmonyPatches.Patch_Locks2_ConfigRuleRace_Allows).GetMethod("Postfix");
                    this.HarmonyInstance.Patch(methodBase, new HarmonyMethod(prefix), new HarmonyMethod(postfix));

                    Log.Message("Project Rimfactory - added Support for Locks 2: Lock Them Out!");
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to added Support for Locks 2: Lock Them Out!");
                }
            }
            if (ModLister.HasActiveModWithName("[KV] Save Storage, Outfit, Crafting, Drug, & Operation Settings [1.4]"))
            {
                //Get the Local Transpilers
                //Billstack makes it function
                //IsWorkTable makes the Gizmos Visible
                var Transpiler_Billstack = AccessTools.Method("ProjectRimFactory.Common.HarmonyPatches.Patch_SaveStorageSettings_Patch_Building_GetGizmos:Transpiler_Billstack");
                var Transpiler_IsWorkTable = AccessTools.Method("ProjectRimFactory.Common.HarmonyPatches.Patch_SaveStorageSettings_Patch_Building_GetGizmos:Transpiler_IsWorkTable");

                //Get the Patch that is adding the Save Storage Gizmos
                var sss_Assembly = LoadedModManager.RunningMods.Where(c => c.PackageId.ToLower() == "savestoragesettings.kv.rw".ToLower())
                    .First().assemblies.loadedAssemblies.Where(a => a.GetType("SaveStorageSettings.Patch_Building_GetGizmos") != null).First();
                if (sss_Assembly is not null)
                {
                    //In the Compiled IL the code adding those Gizmos is hidden away in "new" Types
                    var toplevel_Class = sss_Assembly.GetType("SaveStorageSettings.Patch_Building_GetGizmos");
                    var AllNestedTpyes = toplevel_Class?.GetNestedTypes(HarmonyLib.AccessTools.all);
                    if (toplevel_Class is not null && AllNestedTpyes is not null)
                    {
                        //Get the Method BaseMethod_IsWorkTable
                        HarmonyPatches.Patch_SaveStorageSettings_Patch_Building_GetGizmos.Patch_Building_Gizmos = AllNestedTpyes.FirstOrDefault(t => t.FullName.Contains("d__0"));

                        var BaseMethod_IsWorkTable = HarmonyPatches.Patch_SaveStorageSettings_Patch_Building_GetGizmos.Patch_Building_Gizmos?.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);

                        //Check if we have found Patch_Building_Gizmos
                        if (HarmonyPatches.Patch_SaveStorageSettings_Patch_Building_GetGizmos.Patch_Building_Gizmos is not null && BaseMethod_IsWorkTable is not null)
                        {
                            //Patch Patch_Building_Gizmos
                            this.HarmonyInstance.Patch(BaseMethod_IsWorkTable, null, null, new HarmonyMethod(Transpiler_IsWorkTable));

                            //The Code adding the different Gizmos is hidden in another Display Class
                            //That Class Contains the relevant Code in Methods with a Simelar naming
                            var BaseMethods_Billstack = AllNestedTpyes.FirstOrDefault(t => t.FullName.Contains("c__DisplayClass0_1"))
                                .GetMethods(HarmonyLib.AccessTools.all).Where(m => m.Name.Contains("b__"));

                            if (BaseMethods_Billstack is not null)
                            {
                                //For each of them we want to alter how the Billstack (2. Parameter) is retrieved.
                                //Luckily we can use the Same Transpiler for this.
                                foreach (MethodBase BaseMethod_Billstack in BaseMethods_Billstack)
                                {
                                    if (BaseMethod_Billstack == null) continue;
                                    this.HarmonyInstance.Patch(BaseMethod_Billstack, null, null, new HarmonyMethod(Transpiler_Billstack));
                                }
                                Log.Message("Added Support for: [KV] Save Storage, Outfit, Crafting, Drug, & Operation Settings");
                            }
                            else
                            {
                                Log.Error("PRF Could not find savestoragesettings.kv.rw Patch_Building_Gizmos nested c__DisplayClass0_1 Class");
                            }
                        }
                        else
                        {
                            Log.Error("PRF Could not find savestoragesettings.kv.rw Patch_Building_Gizmos nested Class and or its MoveNext Method");
                        }
                    }
                    else
                    {
                        Log.Error("PRF Could not find savestoragesettings.kv.rw Patch_Building_GetGizmos Class and or SubTypes");
                    }
                }
                else
                {
                    Log.Error("PRF Could not find savestoragesettings.kv.rw assembly");
                }
            }
            if (ModLister.HasActiveModWithName("SeedsPlease"))
            {
                Log.Warning("PRF - SeedsPlease Detected - Compatibility probably requires an Update. Please Notify the PRF Team");
                ModSupport_SeedsPlease = true;
            }
            if (ModLister.HasActiveModWithName("SeedsPlease: Lite"))
            {
                ModSupport_SeedsPleaseLite = true;
            }

        }


        public Harmony HarmonyInstance { get; private set; }

        public ProjectRimFactory_ModSettings Settings { get; private set; }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "ProjectRimFactoryModName".Translate();
        }

        public override void WriteSettings()
        {
            this.Settings.Apply();
            Settings.Write();
            if (this.Settings.RequireReboot)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PRF.Settings.RequireReboot".Translate(), () => GenCommandLine.Restart()));
            }
        }

        // I am happy enough to make this static; it's not like there will be more than once
        //   instance of the mod loaded or anything.
        public static List<SpecialSculpture> availableSpecialSculptures; // loaded on startup in SpecialScupture; see above
    }
}
