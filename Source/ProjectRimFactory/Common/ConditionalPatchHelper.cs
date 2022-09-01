using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;
using Verse.AI;
using ProjectRimFactory.Storage;
using System.Reflection;

namespace ProjectRimFactory.Common
{
    public static class ConditionalPatchHelper
    {
        public class TogglePatch
        {
            private bool Patched = false;

            private readonly MethodInfo base_m;
            private readonly HarmonyMethod trans_hm = null;
            private readonly HarmonyMethod pre_hm = null;
            private readonly HarmonyMethod post_hm = null;
            private readonly MethodInfo trans_m = null;
            private readonly MethodInfo pre_m = null;
            private readonly MethodInfo post_m = null;

            public TogglePatch(MethodInfo base_method, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo Transpiler = null)
            {
                base_m = base_method;
                if (Transpiler != null)  trans_hm = new HarmonyMethod(Transpiler);
                if (prefix != null) pre_hm = new HarmonyMethod(prefix);
                if (postfix != null) post_hm = new HarmonyMethod(postfix);
                trans_m = Transpiler;
                pre_m = prefix;
                post_m = postfix;
            }

            public void PatchHandler(bool patch)
            {
                if (patch && !Patched)
                {
                    harmony_instance.Patch(base_m,pre_hm,post_hm,trans_hm);
                    Patched = true;
                }
                else if (Patched && !patch)
                {
                    if (trans_m != null) harmony_instance.Unpatch(base_m, trans_m);
                    if (pre_m != null) harmony_instance.Unpatch(base_m, pre_m);
                    if (post_m != null) harmony_instance.Unpatch(base_m, post_m);
                    Patched = false;
                }
            }

        }

        //conditional
        private static Harmony harmony_instance = null;

        public static TogglePatch Patch_Reachability_CanReach = new TogglePatch(
            AccessTools.Method(typeof(Verse.Reachability), "CanReach", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) }),
            AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_Reachability_CanReach), "Prefix")
            );

        //Storage Patches
        public static TogglePatch Patch_MinifiedThing_Print = new TogglePatch(
            AccessTools.Method(typeof(RimWorld.MinifiedThing), "Print", new Type[] { typeof(SectionLayer)}),
            AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_MinifiedThing_Print), "Prefix")
            );
        public static TogglePatch Patch_Thing_Print = new TogglePatch(
            AccessTools.Method(typeof(Verse.Thing), "Print", new Type[] { typeof(SectionLayer) }),
            AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_Thing_Print), "Prefix")
            );
        public static TogglePatch Patch_ThingWithComps_Draw = new TogglePatch(
           AccessTools.Method(typeof(Verse.ThingWithComps), "Print", new Type[] { typeof(SectionLayer) }),
           AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_ThingWithComps_Draw), "Prefix")
           );
        public static TogglePatch Patch_ThingWithComps_DrawGUIOverlay = new TogglePatch(
           AccessTools.Method(typeof(Verse.ThingWithComps), "DrawGUIOverlay"),
           AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_ThingWithComps_DrawGUIOverlay), "Prefix")
           );
        public static TogglePatch Patch_Thing_DrawGUIOverlay = new TogglePatch(
           AccessTools.Method(typeof(Verse.ThingWithComps), "DrawGUIOverlay"),
           AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_Thing_DrawGUIOverlay), "Prefix")
           );
        public static TogglePatch Patch_FloatMenuMakerMap_ChoicesAtFor = new TogglePatch(
          AccessTools.Method(typeof(RimWorld.FloatMenuMakerMap), "ChoicesAtFor", new Type[] { typeof(UnityEngine.Vector3), typeof(Pawn), typeof(bool) }),
          AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_FloatMenuMakerMap_ChoicesAtFor), "Prefix")
          );
        public static TogglePatch Patch_Building_Storage_Accepts = new TogglePatch(
         AccessTools.Method(typeof(RimWorld.Building_Storage), "Accepts", new Type[] { typeof(Verse.Thing)}),
         AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_Building_Storage_Accepts), "Prefix")
         );
        public static TogglePatch Patch_ForbidUtility_IsForbidden = new TogglePatch(
         AccessTools.Method(typeof(RimWorld.ForbidUtility), "IsForbidden", new Type[] { typeof(Thing), typeof(Pawn) }),
         AccessTools.Method(typeof(ProjectRimFactory.Common.HarmonyPatches.Patch_ForbidUtility_IsForbidden), "Prefix")
         );





        public static void InitHarmony(Harmony harmony)
        {
            harmony_instance = harmony;
        }

        static List<Building_MassStorageUnit> building_MassStorages = new List<Building_MassStorageUnit>();

        private static void updatePatchStorage()
        {
            bool state = building_MassStorages.Count > 0;

            Patch_MinifiedThing_Print.PatchHandler(state);
            Patch_Thing_Print.PatchHandler(state);
            Patch_ThingWithComps_Draw.PatchHandler(state);
            Patch_ThingWithComps_DrawGUIOverlay.PatchHandler(state);
            Patch_Thing_DrawGUIOverlay.PatchHandler(state);
            Patch_FloatMenuMakerMap_ChoicesAtFor.PatchHandler(state);
            Patch_Building_Storage_Accepts.PatchHandler(state);
            Patch_ForbidUtility_IsForbidden.PatchHandler(state);
        }

        public static void Register(Building_MassStorageUnit building)
        {
            building_MassStorages.Add(building);
            updatePatchStorage();
        }
        public static void Deregister(Building_MassStorageUnit building)
        {
            building_MassStorages.Remove(building);
            updatePatchStorage();
        }

    }
}
