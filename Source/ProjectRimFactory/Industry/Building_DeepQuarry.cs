using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Industry
{
    public class Building_DeepQuarry : Building
    {
        public float GetProduceMtbHours { get { return def.GetModExtension<DeepQuarryDefModExtension>().TickCount; } }
        static IEnumerable<ThingDef> cachedPossibleRockDefCandidates;
        protected static IEnumerable<ThingDef> PossibleRockDefCandidates
        {
            get
            {
                if (cachedPossibleRockDefCandidates != null)
                {
                    return cachedPossibleRockDefCandidates;
                }
                return cachedPossibleRockDefCandidates = from def in DefDatabase<ThingDef>.AllDefs
                                                         where def.building != null && def.building.isNaturalRock && def.building.mineableThing != null
                                                         select def;
            }
        }

        public int ProducedChunksTotal = 0;

        public override void Tick()
        {
            base.Tick();
            var power = GetComp<CompPowerTrader>();
            var fuel = GetComp<CompRefuelable>();
            var flickable = GetComp<CompFlickable>();

            if (Find.TickManager.TicksGame % GenTicks.TickLongInterval == 0)
            {
                if (power?.PowerOn != false && fuel?.HasFuel != false && flickable.SwitchIsOn != false && Rand.MTBEventOccurs(GetProduceMtbHours, GenDate.TicksPerHour, GenTicks.TickLongInterval))
                {
                    GenerateChunk();
                }
            }
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref ProducedChunksTotal, "producedTotal");
            base.ExposeData();
        }

        public virtual void GenerateChunk()
        {
            GenPlace.TryPlaceThing(GetChunkThingToPlace(), GetComp<CompOutputAdjustable>().CurrentCell, Map, ThingPlaceMode.Near);
            ProducedChunksTotal++;
        }

        protected virtual Thing GetChunkThingToPlace()
        {
            ThingDef rock = PossibleRockDefCandidates
                .Where(d => !this.def.GetModExtension<ModExtension_Miner>()?.IsExcluded(d.building.mineableThing) ?? true)
                .RandomElementByWeight(d => d.building.isResourceRock ? d.building.mineableScatterCommonality * d.building.mineableScatterLumpSizeRange.Average * d.building.mineableDropChance : 3f);

            var bonus = this.def.GetModExtension<ModExtension_Miner>()?.GetBonusYield(rock.building.mineableThing);
            if (bonus != null)
            {
                return bonus;
            }

            Thing t = ThingMaker.MakeThing(rock.building.mineableThing);
            t.stackCount = rock.building.mineableYield;
            return t;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string s = base.GetInspectString();
            if (!string.IsNullOrEmpty(s))
            {
                stringBuilder.AppendLine(s);
            }
            stringBuilder.Append("DeepQuarry_TotalSoFar".Translate(ProducedChunksTotal));
            return stringBuilder.ToString().TrimEndNewlines();
        }

#if DEBUG
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach(var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            const int trials = 100000;

            yield return new Command_Action()
            {
                defaultLabel = $"Output simulation result log of mining {trials} times",
                defaultDesc = $"Output simulation result log of mining {trials} times",
                action = () =>
                    Log.Message("number of trials : " + trials + System.Environment.NewLine +
                        Enumerable.Range(0, trials)
                            .Select(i => GetChunkThingToPlace())
                            .GroupBy(t => Tuple.Create(t.def, t.stackCount))
                            .ToList()
                            .Select(g => new { Def = g.Key.Item1, StackCount = g.Key.Item2, Count = g.Count() })
                            .OrderByDescending(p => p.Count)
                            .Aggregate("", (t, i) => t + "def : " + i.Def.defName + " / stackCount : " + i.StackCount + " / minedCount : " + i.Count + " / mine rate(%) : " + (((float)i.Count * 100f) / (float)trials) + System.Environment.NewLine))
            };
        }
#endif
    }
}
