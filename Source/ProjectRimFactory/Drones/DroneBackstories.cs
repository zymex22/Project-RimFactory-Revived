using RimWorld;
using Verse;

namespace ProjectRimFactory.Drones
{
    [StaticConstructorOnStartup]
    public static class DroneBackstories
    {
        public static Backstory childhood;
        public static Backstory adulthood;

        static DroneBackstories()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                childhood = new Backstory
                {
                    title = "PRFDroneName".Translate(),
                    titleShort = "PRFDroneName".Translate(),
                    identifier = "PRFNoneBracketsC",
                    workDisables = WorkTags.Social,
                    slot = BackstorySlot.Childhood,
                    baseDesc = "NoneBrackets".Translate()
                };
                //this check is required to avoid an issue with "BetterLoading" as it calls "LongEventHandler.ExecuteWhenFinished" twice 
                if (!BackstoryDatabase.allBackstories.ContainsKey(childhood.identifier))
                    BackstoryDatabase.AddBackstory(childhood);
                adulthood = new Backstory
                {
                    title = "PRFDroneName".Translate(),
                    titleShort = "PRFDroneName".Translate(),
                    identifier = "PRFNoneBracketsA",
                    workDisables = WorkTags.Social,
                    slot = BackstorySlot.Adulthood,
                    baseDesc = "NoneBrackets".Translate()
                };
                //this check is required to avoid an issue with "BetterLoading" as it calls "LongEventHandler.ExecuteWhenFinished" twice 
                if (!BackstoryDatabase.allBackstories.ContainsKey(adulthood.identifier))
                    BackstoryDatabase.AddBackstory(adulthood);
            });
        }
    }
}