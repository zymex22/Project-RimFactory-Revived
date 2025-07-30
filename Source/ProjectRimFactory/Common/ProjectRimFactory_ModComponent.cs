using HarmonyLib;
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
                ConditionalPatchHelper.PatchReachabilityCanReach.PatchHandler(ProjectRimFactory_ModSettings.PRF_Patch_Reachability_CanReach);
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
        public static System.Reflection.MethodInfo ModSupport_ResearchTreeContinued_ResetLayout = null;
        public static bool ModSupport_ReserchPal = false;
        public static bool ModSupport_ReserchPowl = false;
        public static bool ModSupport_ResearchTreeContinued = false;

        public static bool ModSupport_SeedsPlease = false;
        public static bool ModSupport_SeedsPleaseLite = false;

        public static bool ModSupport_VEF_DualCropExtension = false;
        
        private static MethodInfo ModSupport_SOS2_MoveShipFlagGetter = null;
        public static bool ModSupport_SOS2_MoveShip
        {
            get
            {
                if (ModSupport_SOS2_MoveShipFlagGetter is null) return false;
                return (bool)ModSupport_SOS2_MoveShipFlagGetter.Invoke(null, new object[] { });
            }
        }


        private void LoadModSupport()
        {
            if (ModLister.HasActiveModWithName("RimFridge: Now with Shelves!"))
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
            }else if (ModLister.HasActiveModWithName("Research Tree (Continued)"))
            {
                ModSupport_ResearchTreeContinued_ResetLayout = AccessTools.Method("FluffyResearchTree.Tree:Reset");
                if (ModSupport_ResearchTreeContinued_ResetLayout != null)
                {
                    Log.Message("Project Rimfactory - added Support for Research Tree (Continued) when using Lite Mode");
                    ModSupport_ResearchTreeContinued = true;
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to added Support for Research Tree (Continued) when using Lite Mode");
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
            if (ModLister.HasActiveModWithName("Vanilla Expanded Framework"))
            {
                ModSupport_VEF_DualCropExtension = true;
            }
            if (ModLister.HasActiveModWithName("QualityBuilder"))
            {
                MethodBase QualityBuilder_pawnCanConstruct = AccessTools.Method("QualityBuilder.QualityBuilder:pawnCanConstruct");
                MethodBase QualityBuilder_getPawnConstructionSkill = AccessTools.Method("QualityBuilder.QualityBuilder:getPawnConstructionSkill");

                if (QualityBuilder_pawnCanConstruct != null && QualityBuilder_getPawnConstructionSkill != null)
                {
                    var postfix_pawnCanConstruct = typeof(HarmonyPatches.Patch_QualityBuilder_PawnCanConstruct).GetMethod("Postfix");
                    var prefix_getPawnConstructionSkill = typeof(HarmonyPatches.Patch_QualityBuilder_GetPawnConstructionSkill).GetMethod("Prefix");
                    this.HarmonyInstance.Patch(QualityBuilder_pawnCanConstruct, null, new HarmonyMethod(postfix_pawnCanConstruct));
                    this.HarmonyInstance.Patch(QualityBuilder_getPawnConstructionSkill, new HarmonyMethod(prefix_getPawnConstructionSkill));
                    Log.Message("Project Rimfactory - Added Support for QualityBuilder");
                }   
                else
                {
                    Log.Warning("Project Rimfactory - Failed to add Support for QualityBuilder");
                }
                
            }
            if (ModLister.HasActiveModWithName("Save Our Ship 2"))
            {
                try
                {
                    var shipInteriorMod2 = Type.GetType("SaveOurShip2.ShipInteriorMod2, ShipsHaveInsides", true);
                    ModSupport_SOS2_MoveShipFlagGetter = shipInteriorMod2.GetMethod("get_MoveShipFlag", BindingFlags.Public | BindingFlags.Static);
                    Log.Message("Project Rimfactory - Added Support for Save Our Ship 2");
                }
                catch (Exception e)
                {
                    Log.Warning("Project Rimfactory - ailed to add Support for Save Our Ship 2");
                    Log.Error(e.Message);
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

        // I am happy enough to make this static; it's not like there will be more than once
        //   instance of the mod loaded or anything.
        public static List<SpecialSculpture> availableSpecialSculptures; // loaded on startup in SpecialScupture; see above
    }
}
