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

                var backstoryDefs = DefDatabase<BackstoryDef>.AllDefsListForReading;
                TryAddBackstoryDef(backstoryDefs, childhood);
                TryAddBackstoryDef(backstoryDefs, adulthood);
            });
        }

        private static void TryAddBackstoryDef(List<BackstoryDef> backstoryDefs, BackstoryDef backstoryDef)
        {
            //this check is required to avoid an issue with "BetterLoading" as it calls "LongEventHandler.ExecuteWhenFinished" twice 
            if (!backstoryDefs.Contains(backstoryDef))
            {
                backstoryDefs.Add(backstoryDef);
            }
        }
    }
}
