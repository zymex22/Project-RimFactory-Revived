using ProjectRimFactory.Common;
using ProjectRimFactory.Storage;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Industry
{
    public class Building_PaperclipDuplicator : Building
    {
        long paperclipCount;
        int lastTick = Find.TickManager.TicksGame;
        public Building_MassStorageUnit boundStorageUnit;

        CompOutputAdjustable outputComp;
        CompPowerTrader powerComp;
        public long PaperclipsActual
        {
            get
            {
                long result = 0;
                if (paperclipCount != long.MaxValue)
                {
                    try
                    {
                        checked
                        {
                            result = (long)(paperclipCount * Math.Pow(1.05, (Find.TickManager.TicksGame - lastTick).TicksToDays()));
                        }
                    }
                    catch (OverflowException)
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox("PRF_ArchoCipher_BankOverflow".Translate()));
                        PaperclipsActual = long.MaxValue;
                        result = long.MaxValue;
                    }
                }
                return result;
            }
            set
            {
                paperclipCount = value;
                lastTick = Find.TickManager.TicksGame;
            }
        }
        public virtual void DepositPaperclips(int count)
        {
            PaperclipsActual = PaperclipsActual + count;
        }
        public virtual void WithdrawPaperclips(int count)
        {
            PaperclipsActual = PaperclipsActual - count;
        }
        public override void PostMake()
        {
            base.PostMake();
            outputComp = GetComp<CompOutputAdjustable>();
            powerComp = GetComp<CompPowerTrader>();
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            outputComp = GetComp<CompOutputAdjustable>();
            powerComp = GetComp<CompPowerTrader>();
        }
        protected override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
        }
        public override string GetInspectString()
        {
            StringBuilder builder = new StringBuilder();
            string str = base.GetInspectString();
            if (!string.IsNullOrEmpty(str))
            {
                builder.AppendLine(str);
            }
            builder.AppendLine("PaperclipsInDuplicator".Translate(PaperclipsActual.ToString()));
            if (boundStorageUnit != null)
            {
                builder.AppendLine("PaperclipsInStorageUnit".Translate(boundStorageUnit.StoredItems.Where(t => t.def == PRFDefOf.Paperclip).Sum(t => t.stackCount)));
            }
            else
            {
                builder.AppendLine("PRFNoBoundStorageUnit".Translate());
            }
            return builder.ToString().TrimEndNewlines();
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "DEBUG: Double paperclip amount",
                    action = () =>  
                    {
                        try
                        {
                            checked
                            {
                                if (PaperclipsActual != long.MaxValue)
                                {
                                    PaperclipsActual = PaperclipsActual * 2;
                                }
                            }
                        }
                        catch (OverflowException)
                        {
                            Find.WindowStack.Add(new Dialog_MessageBox("PRF_ArchoCipher_BankOverflow".Translate()));
                            PaperclipsActual = long.MaxValue;
                        }
                    }
                };
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref paperclipCount, "paperclipCount");
            Scribe_Values.Look(ref lastTick, "lastTick");
            Scribe_References.Look(ref boundStorageUnit, "boundStorageUnit");
        }
    }
}
