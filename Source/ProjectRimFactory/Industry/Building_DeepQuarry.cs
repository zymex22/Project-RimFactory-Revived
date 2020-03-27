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
        public const float ProduceMtbHours = 6f;
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

        public override void TickLong()
        {
            if (GetComp<CompPowerTrader>()?.PowerOn != false && Rand.MTBEventOccurs(ProduceMtbHours, GenDate.TicksPerHour, GenTicks.TickLongInterval))
            {
                GenerateChunk();
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
            ThingDef rock = PossibleRockDefCandidates.RandomElementByWeight(d => d.building.isResourceRock ? d.building.mineableScatterCommonality * d.building.mineableScatterLumpSizeRange.Average * d.building.mineableDropChance : 3f);
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
    }
}
