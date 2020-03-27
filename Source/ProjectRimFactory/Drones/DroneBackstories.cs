using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
                childhood = new Backstory()
                {
                    title = "PRFDroneName".Translate(),
                    titleShort = "PRFDroneName".Translate(),
                    identifier = "PRFNoneBracketsC",
                    workDisables = WorkTags.Social,
                    slot = BackstorySlot.Childhood,
                    baseDesc = "NoneBrackets".Translate()
                };
                BackstoryDatabase.AddBackstory(childhood);
                adulthood = new Backstory()
                {
                    title = "PRFDroneName".Translate(),
                    titleShort = "PRFDroneName".Translate(),
                    identifier = "PRFNoneBracketsA",
                    workDisables = WorkTags.Social,
                    slot = BackstorySlot.Adulthood,
                    baseDesc = "NoneBrackets".Translate()
                };
                BackstoryDatabase.AddBackstory(adulthood);
            });
        }
    }
}
