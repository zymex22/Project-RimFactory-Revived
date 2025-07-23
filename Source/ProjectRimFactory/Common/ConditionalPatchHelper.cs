using HarmonyLib;
using ProjectRimFactory.Storage;
using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Common
{
    public static class ConditionalPatchHelper
    {
        public class TogglePatch
        {
            private bool patched = false;
            public bool Status => patched;

            private readonly MethodInfo baseMethodInfo;
            private readonly HarmonyMethod transpilerHarmonyMethod = null;
            private readonly HarmonyMethod prefixHarmonyMethod = null;
            private readonly HarmonyMethod postfixHarmonyMethod = null;
            private readonly MethodInfo transpilerMethodInfo = null;
            private readonly MethodInfo prefixMethodInfo = null;
            private readonly MethodInfo postfixMethodInfo = null;

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
                if (patch && !patched)
                {
                    harmony_instance.Patch(baseMethodInfo, prefixHarmonyMethod, postfixHarmonyMethod, transpilerHarmonyMethod);
                    patched = true;
                }
                else if (patched && !patch)
                {
                    if (transpilerMethodInfo != null) harmony_instance.Unpatch(baseMethodInfo, transpilerMethodInfo);
                    if (prefixMethodInfo != null) harmony_instance.Unpatch(baseMethodInfo, prefixMethodInfo);
                    if (postfixMethodInfo != null) harmony_instance.Unpatch(baseMethodInfo, postfixMethodInfo);
                    patched = false;
                }
            }

        }

        //conditional
        private static Harmony harmony_instance = null;

        public static TogglePatch Patch_Reachability_CanReach = new TogglePatch(
            AccessTools.Method(typeof(Reachability), "CanReach", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) }),
            null,
            AccessTools.Method(typeof(HarmonyPatches.Patch_Reachability_CanReach), "Postfix")
            );

        //Storage Patches
        public static TogglePatch Patch_MinifiedThing_Print = new TogglePatch(
            AccessTools.Method(typeof(MinifiedThing), "Print", new Type[] { typeof(SectionLayer) }),
            AccessTools.Method(typeof(HarmonyPatches.Patch_MinifiedThing_Print), "Prefix")
            );
        public static TogglePatch Patch_Thing_Print = new TogglePatch(
            AccessTools.Method(typeof(Thing), "Print", new Type[] { typeof(SectionLayer) }),
            AccessTools.Method(typeof(HarmonyPatches.Patch_Thing_Print), "Prefix")
            );
        public static TogglePatch Patch_ThingWithComps_DrawGUIOverlay = new TogglePatch(
           AccessTools.Method(typeof(ThingWithComps), "DrawGUIOverlay"),
           AccessTools.Method(typeof(HarmonyPatches.Patch_ThingWithComps_DrawGUIOverlay), "Prefix")
           );
        public static TogglePatch Patch_Thing_DrawGUIOverlay = new TogglePatch(
           AccessTools.Method(typeof(Thing), "DrawGUIOverlay"),
           AccessTools.Method(typeof(HarmonyPatches.Patch_Thing_DrawGUIOverlay), "Prefix")
           );
        public static TogglePatch Patch_FloatMenuMakerMap_ChoicesAtFor = new TogglePatch(
          AccessTools.Method(typeof(FloatMenuMakerMap), "GetOptions"),
          AccessTools.Method(typeof(HarmonyPatches.Patch_FloatMenuMakerMap_GetOptions), "Prefix")
          );
        public static TogglePatch Patch_Building_Storage_Accepts = new TogglePatch(
         AccessTools.Method(typeof(Building_Storage), "Accepts", new Type[] { typeof(Thing) }),
         AccessTools.Method(typeof(HarmonyPatches.Patch_Building_Storage_Accepts), "Prefix")
         );
        public static TogglePatch Patch_StorageSettings_AllowedToAccept = new TogglePatch(
         AccessTools.Method(typeof(StorageSettings), "AllowedToAccept", new Type[] { typeof(Thing) }),
         AccessTools.Method(typeof(HarmonyPatches.Patch_StorageSettings_AllowedToAccept), "Prefix")
         );
        public static TogglePatch Patch_ForbidUtility_IsForbidden = new TogglePatch(
         AccessTools.Method(typeof(ForbidUtility), "IsForbidden", new Type[] { typeof(Thing), typeof(Pawn) }),
         AccessTools.Method(typeof(HarmonyPatches.Patch_ForbidUtility_IsForbidden), "Prefix")
         );

        public static void InitHarmony(Harmony harmony)
        {
            harmony_instance = harmony;
        }

        static List<Building_MassStorageUnit> building_MassStorages = [];

        private static void UpdatePatchStorage()
        {
            var state = building_MassStorages.Count > 0;
            Patch_MinifiedThing_Print.PatchHandler(state);
            Patch_Thing_Print.PatchHandler(state);
            Patch_ThingWithComps_DrawGUIOverlay.PatchHandler(state);
            Patch_Thing_DrawGUIOverlay.PatchHandler(state);
            Patch_FloatMenuMakerMap_ChoicesAtFor.PatchHandler(state);
            Patch_Building_Storage_Accepts.PatchHandler(state);
            Patch_ForbidUtility_IsForbidden.PatchHandler(state);
            Patch_StorageSettings_AllowedToAccept.PatchHandler(state);
        }

        public static void Register(Building_MassStorageUnit building)
        {
            building_MassStorages.Add(building);
            UpdatePatchStorage();
        }
        public static void Deregister(Building_MassStorageUnit building)
        {
            building_MassStorages.Remove(building);
            UpdatePatchStorage();
        }

    }
}
