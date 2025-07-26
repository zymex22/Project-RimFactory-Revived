using ProjectRimFactory.Common;
using RimWorld;
using System.Text;
using Verse;

namespace ProjectRimFactory.Archo.Things
{
    // ReSharper disable once UnusedType.Global
    public class Building_PaperclipSpawner : Building
    {
        private int workDone;
        private CompPowerTrader powerComp;
        private CompOutputAdjustable outputComp;
        public override void PostMake()
        {
            base.PostMake();
            powerComp = GetComp<CompPowerTrader>();
            outputComp = GetComp<CompOutputAdjustable>();
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            outputComp = GetComp<CompOutputAdjustable>();
        }

        protected override void Tick()
        {
            if (!Spawned) return;
            if (this.IsHashIntervalTick(10) && powerComp.PowerOn)
            {
                workDone++;
                if (workDone < 250) return;
                var result = ThingMaker.MakeThing(PRFDefOf.Paperclip); // Spawns 10000 paperclips per batch
                result.stackCount = 10000;
                GenPlace.TryPlaceThing(result, outputComp.CurrentCell, Map, ThingPlaceMode.Near);
                workDone = 0;
            }
        }
        public override string GetInspectString()
        {
            var builder = new StringBuilder();
            var str = base.GetInspectString();
            if (!string.IsNullOrEmpty(str))
            {
                builder.AppendLine(str);
            }
            builder.Append("NextSpawnedItemIn".Translate(
                GenLabel.ThingLabel(PRFDefOf.Paperclip, null, 10000)) + ": " + ((250 - workDone) * 10).ToStringTicksToPeriod());
            return builder.ToString().TrimEndNewlines();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workDone, "workDone");
        }
    }
}
