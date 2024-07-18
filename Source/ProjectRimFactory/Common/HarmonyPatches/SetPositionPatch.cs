using HarmonyLib;
using ProjectRimFactory.Storage;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    /// <summary>
    /// Notifies Building_MassStorageUnit via Notify_LostThing(t) if a contained Item was moved away
    /// </summary>
    [HarmonyPatch(typeof(Thing), "set_Position")]
    public static class SetPositionPatch
    {
        public static void Prefix(Thing __instance, out Building_MassStorageUnit __state)
        {
            __state = null;
            IntVec3 pos = __instance.Position;
            if (__instance.def.category == ThingCategory.Item && pos.IsValid)
            {
                var map = __instance.Map;
                if (map != null)
                {
                    __state = pos.GetFirst<Building_MassStorageUnit>(map);
                }
            }
        }
        public static void Postfix(Thing __instance, Building_MassStorageUnit __state)
        {
            __state?.Notify_LostThing(__instance);
        }
    }
}
