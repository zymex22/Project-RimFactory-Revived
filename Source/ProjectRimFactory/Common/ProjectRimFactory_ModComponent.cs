using HarmonyLib;
using ProjectRimFactory.Storage;
using SimpleFixes;
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
                NoMessySpawns.Instance.Add(ShouldSuppressDisplace, (Building_MassStorageUnit b, Map map) => true);
                availableSpecialSculptures = SpecialSculpture.LoadAvailableSpecialSculptures(content);
                LoadModSupport();
                ConditionalPatchHelper.InitHarmony(this.HarmonyInstance);
                ConditionalPatchHelper.Patch_Reachability_CanReach.PatchHandler(ProjectRimFactory_ModSettings.PRF_Patch_Reachability_CanReach);
                ConditionalPatchHelper.Patch_WealthWatcher_CalculateWealthItems.PatchHandler(ProjectRimFactory_ModSettings.PRF_Patch_WealthWatcher_CalculateWealthItems);

            }
            catch (Exception ex)
            {
                Log.Error("Project RimFactory Core :: Caught exception: " + ex);
            }
        }

        //Mod Support
        //Cached MethodInfo as Reflection is Slow
        public static System.Reflection.MethodInfo ModSupport_RrimFridge_GetFridgeCache = null;
        public static System.Reflection.MethodInfo ModSupport_RrimFridge_HasFridgeAt = null;
        public static bool ModSupport_RrimFrige_Dispenser = false;

        // Typo...
        public static System.Reflection.MethodInfo ModSupport_ReserchPal_ResetLayout = null;
        // Matching Typo...
        public static System.Reflection.MethodInfo ModSupport_ReserchPowl_ResetLayout = null;
        // Typo..
        public static bool ModSupport_ReserchPal = false;
        // Matching Typo..
        public static bool ModSupport_ReserchPowl = false;

        public static System.Reflection.FieldInfo ModSupport_SimpleFridge_fridgeCache = null;

        private void LoadModSupport()
        {
            if (ModLister.HasActiveModWithName("[KV] RimFridge") || ModLister.HasActiveModWithName("RimFridge Updated"))
            {
                ModSupport_RrimFridge_GetFridgeCache = AccessTools.Method("RimFridge.FridgeCache:GetFridgeCache");
                ModSupport_RrimFridge_HasFridgeAt = AccessTools.Method("RimFridge.FridgeCache:HasFridgeAt");
                if (ModSupport_RrimFridge_GetFridgeCache != null && ModSupport_RrimFridge_HasFridgeAt != null)
                {
                    Log.Message("Project Rimfactory - added Support for shared Nutrient Dispenser with [KV] RimFridge or RimFridge Updated");
                    ModSupport_RrimFrige_Dispenser = true;
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to add Support for shared Nutrient Dispenser with [KV] RimFridge or RimFridge Updated");
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
                ModSupport_SimpleFridge_fridgeCache = AccessTools.Field("SimpleFridge.Mod_SimpleFridge:fridgeCache");

                MethodBase SimpleFridge_Patch_GameComponentTick_Postfix = null;
                Type Patch_GameComponentTick = Type.GetType("SimpleFridge.Patch_GameComponentTick, SimpleUtilitiesFridge", false);
                if (Patch_GameComponentTick != null)
                {
                    SimpleFridge_Patch_GameComponentTick_Postfix = AccessTools.Method(Patch_GameComponentTick, "Postfix");
                }
                if (SimpleFridge_Patch_GameComponentTick_Postfix != null)
                {
                    var postfix = typeof(HarmonyPatches.Patch_Patch_GameComponentTick_Postfix).GetMethod("Postfix");
                    this.HarmonyInstance.Patch(SimpleFridge_Patch_GameComponentTick_Postfix, null, new HarmonyMethod(postfix));

                    Log.Message("Project Rimfactory - added Support for Fridge DSU Power using Simple Utilities: Fridge");
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to add Support for Fridge DSU Power using Simple Utilities: Fridge");
                }
            }
            if (ModLister.HasActiveModWithName("ResearchPal - Forked"))
            {
                // typo...
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
                // Matching Typo...
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
            if (ModLister.HasActiveModWithName("[KV] Save Storage, Outfit, Crafting, Drug, & Operation Settings") || ModLister.HasActiveModWithName("[KV] Save Storage, Outfit, Crafting, Drug, & Operation Settings [1.4]"))
            {
                var Transpiler_Billstack = AccessTools.Method("ProjectRimFactory.Common.HarmonyPatches.SaveStorageSettings_Patch:Transpiler_Billstack");
                var Transpiler_IsWorkTable = AccessTools.Method("ProjectRimFactory.Common.HarmonyPatches.SaveStorageSettings_Patch:Transpiler_IsWorkTable");

                var sss_Assembly = LoadedModManager.RunningMods.Where(c => c.PackageId.ToLower() == "savestoragesettings.kv.rw".ToLower())
                    .First().assemblies.loadedAssemblies.Where(a => a.GetType("SaveStorageSettings.Patch_Building_GetGizmos") != null).First();
                if (sss_Assembly is not null)
                {

                    var toplevel_Class = sss_Assembly.GetType("SaveStorageSettings.Patch_Building_GetGizmos");
                    var AllNestedTpyes = toplevel_Class?.GetNestedTypes(HarmonyLib.AccessTools.all);
                    if (toplevel_Class is not null && AllNestedTpyes is not null)
                    {
                        HarmonyPatches.SaveStorageSettings_Patch.Patch_Building_Gizmos = AllNestedTpyes.FirstOrDefault(t => t.FullName.Contains("d__0"));

                        var BaseMethod_IsWorkTable = HarmonyPatches.SaveStorageSettings_Patch.Patch_Building_Gizmos?.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (HarmonyPatches.SaveStorageSettings_Patch.Patch_Building_Gizmos is not null && BaseMethod_IsWorkTable is not null)
                        {
                            this.HarmonyInstance.Patch(BaseMethod_IsWorkTable, null, null, new HarmonyMethod(Transpiler_IsWorkTable));

                            var BaseMethods_Billstack = AllNestedTpyes.FirstOrDefault(t => t.FullName.Contains("c__DisplayClass0_0"))
                                .GetMethods(HarmonyLib.AccessTools.all).Where(m => m.Name.Contains("b__"));

                            if (BaseMethods_Billstack is not null)
                            {
                                foreach (MethodBase BaseMethod_Billstack in BaseMethods_Billstack)
                                {
                                    if (BaseMethod_Billstack == null) continue;
                                    this.HarmonyInstance.Patch(BaseMethod_Billstack, null, null, new HarmonyMethod(Transpiler_Billstack));
                                }
                                Log.Message("Added Support for: [KV] Save Storage, Outfit, Crafting, Drug, & Operation Settings");
                            }
                            else
                            {
                                Log.Error("PRF Could not find savestoragesettings.kv.rw Patch_Building_Gizmos nested c__DisplayClass0_0 Class");
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

        public static bool ShouldSuppressDisplace(IntVec3 cell, Map map, bool respawningAfterLoad)
        {
            return !respawningAfterLoad || map?.thingGrid.ThingsListAtFast(cell).OfType<Building_MassStorageUnit>().Any() != true;
        }
        // I am happy enough to make this static; it's not like there will be more than once
        //   instance of the mod loaded or anything.
        public static List<SpecialSculpture> availableSpecialSculptures; // loaded on startup in SpecialScupture; see above
    }
}
