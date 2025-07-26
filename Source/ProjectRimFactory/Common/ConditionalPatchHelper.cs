using HarmonyLib;
using ProjectRimFactory.Storage;
using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common
{
    public static class ConditionalPatchHelper
    {
        public class TogglePatch
        {
            public bool Status { get; private set; }

            private readonly MethodInfo baseMethodInfo;
            private readonly HarmonyMethod transpilerHarmonyMethod;
            private readonly HarmonyMethod prefixHarmonyMethod;
            private readonly HarmonyMethod postfixHarmonyMethod;
            private readonly MethodInfo transpilerMethodInfo;
            private readonly MethodInfo prefixMethodInfo;
            private readonly MethodInfo postfixMethodInfo;

            public TogglePatch(MethodInfo baseMethod, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null)
            {
                baseMethodInfo = baseMethod;
                if (transpiler != null) transpilerHarmonyMethod = new HarmonyMethod(transpiler);
                if (prefix != null) prefixHarmonyMethod = new HarmonyMethod(prefix);
                if (postfix != null) postfixHarmonyMethod = new HarmonyMethod(postfix);
                transpilerMethodInfo = transpiler;
                prefixMethodInfo = prefix;
                postfixMethodInfo = postfix;
            }

            public void PatchHandler(bool patch)
            {
                if (patch && !Status)
                {
                    harmonyInstance.Patch(baseMethodInfo, prefixHarmonyMethod, postfixHarmonyMethod, transpilerHarmonyMethod);
                    Status = true;
                }
                else if (Status && !patch)
                {
                    if (transpilerMethodInfo != null) harmonyInstance.Unpatch(baseMethodInfo, transpilerMethodInfo);
                    if (prefixMethodInfo != null) harmonyInstance.Unpatch(baseMethodInfo, prefixMethodInfo);
                    if (postfixMethodInfo != null) harmonyInstance.Unpatch(baseMethodInfo, postfixMethodInfo);
                    Status = false;
                }
            }

        }

        //conditional
        private static Harmony harmonyInstance;

        public static readonly TogglePatch PatchReachabilityCanReach = new(
            AccessTools.Method(typeof(Reachability), "CanReach", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) }),
            null,
            AccessTools.Method(typeof(HarmonyPatches.Patch_Reachability_CanReach), "Postfix")
            );

        //Storage Patches
        private static readonly TogglePatch PatchMinifiedThingPrint = new(
            AccessTools.Method(typeof(MinifiedThing), "Print", new Type[] { typeof(SectionLayer) }),
            AccessTools.Method(typeof(HarmonyPatches.Patch_MinifiedThing_Print), "Prefix")
            );

        private static readonly TogglePatch PatchThingPrint = new(
            AccessTools.Method(typeof(Thing), "Print", new Type[] { typeof(SectionLayer) }),
            AccessTools.Method(typeof(HarmonyPatches.Patch_Thing_Print), "Prefix")
            );

        private static readonly TogglePatch PatchThingWithCompsDrawGUIOverlay = new(
           AccessTools.Method(typeof(ThingWithComps), "DrawGUIOverlay"),
           AccessTools.Method(typeof(HarmonyPatches.Patch_ThingWithComps_DrawGUIOverlay), "Prefix")
           );

        private static readonly TogglePatch PatchThingDrawGUIOverlay = new(
           AccessTools.Method(typeof(Thing), "DrawGUIOverlay"),
           AccessTools.Method(typeof(HarmonyPatches.Patch_Thing_DrawGUIOverlay), "Prefix")
           );

        private static readonly TogglePatch PatchFloatMenuMakerMapChoicesAtFor = new(
          AccessTools.Method(typeof(FloatMenuMakerMap), "GetOptions"),
          AccessTools.Method(typeof(HarmonyPatches.Patch_FloatMenuMakerMap_GetOptions), "Prefix")
          );

        private static readonly TogglePatch PatchBuildingStorageAccepts = new(
         AccessTools.Method(typeof(Building_Storage), "Accepts", new Type[] { typeof(Thing) }),
         AccessTools.Method(typeof(HarmonyPatches.Patch_Building_Storage_Accepts), "Prefix")
         );

        private static readonly TogglePatch PatchStorageSettingsAllowedToAccept = new(
         AccessTools.Method(typeof(StorageSettings), "AllowedToAccept", new Type[] { typeof(Thing) }),
         AccessTools.Method(typeof(HarmonyPatches.Patch_StorageSettings_AllowedToAccept), "Prefix")
         );

        private static readonly TogglePatch PatchForbidUtilityIsForbidden = new(
         AccessTools.Method(typeof(ForbidUtility), "IsForbidden", new Type[] { typeof(Thing), typeof(Pawn) }),
         AccessTools.Method(typeof(HarmonyPatches.Patch_ForbidUtility_IsForbidden), "Prefix")
         );

        public static void InitHarmony(Harmony harmony)
        {
            harmonyInstance = harmony;
        }

        private static readonly List<Building_MassStorageUnit> BuildingMassStorages = [];

        private static void UpdatePatchStorage()
        {
            var state = BuildingMassStorages.Count > 0;
            PatchMinifiedThingPrint.PatchHandler(state);
            PatchThingPrint.PatchHandler(state);
            PatchThingWithCompsDrawGUIOverlay.PatchHandler(state);
            PatchThingDrawGUIOverlay.PatchHandler(state);
            PatchFloatMenuMakerMapChoicesAtFor.PatchHandler(state);
            PatchBuildingStorageAccepts.PatchHandler(state);
            PatchForbidUtilityIsForbidden.PatchHandler(state);
            PatchStorageSettingsAllowedToAccept.PatchHandler(state);
        }

        public static void Register(Building_MassStorageUnit building)
        {
            BuildingMassStorages.Add(building);
            UpdatePatchStorage();
        }
        public static void Deregister(Building_MassStorageUnit building)
        {
            BuildingMassStorages.Remove(building);
            UpdatePatchStorage();
        }

    }
}
