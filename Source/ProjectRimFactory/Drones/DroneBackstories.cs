using ProjectRimFactory.Common;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Drones
{
    [StaticConstructorOnStartup]
    public static class DroneBackstories
    {
        public static BackstoryDef childhood;
        public static BackstoryDef adulthood;



        static DroneBackstories()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                childhood = new BackstoryDef()
                {
                    title = "PRFDroneName".Translate(),
                    titleShort = "PRFDroneName".Translate(),
                    identifier = "PRFNoneBracketsC",
                    workDisables = WorkTags.Social,
                    slot = BackstorySlot.Childhood,
                    baseDesc = "NoneBrackets".Translate(),
                    description = "NoneBrackets".Translate(),
                    modContentPack = LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().Content
                };

                adulthood = new BackstoryDef()
                {
                    title = "PRFDroneName".Translate(),
                    titleShort = "PRFDroneName".Translate(),
                    identifier = "PRFNoneBracketsA",
                    workDisables = WorkTags.Social,
                    slot = BackstorySlot.Adulthood,
                    baseDesc = "NoneBrackets".Translate(),
                    description = "NoneBrackets".Translate(),
                    modContentPack = LoadedModManager.GetMod<ProjectRimFactory_ModComponent>().Content
                };

                var BackstoryDefs = DefDatabase<BackstoryDef>.AllDefsListForReading;
                TryAddBacksoryDef(BackstoryDefs, childhood);
                TryAddBacksoryDef(BackstoryDefs, adulthood);
            });
        }

        private static void TryAddBacksoryDef(List<BackstoryDef> BackstoryDefs, BackstoryDef backstoryDef)
        {
            //this check is required to avoid an issue with "BetterLoading" as it calls "LongEventHandler.ExecuteWhenFinished" twice 
            if (!BackstoryDefs.Contains(backstoryDef))
            {
                BackstoryDefs.Add(backstoryDef);
            }
        }
    }
}
