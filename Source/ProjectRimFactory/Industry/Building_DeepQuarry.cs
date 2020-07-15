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
    /// <summary>
    /// Deep Quarry
    /// 
    /// Note: Can be used as either Tick(), TickRare(), or TickLong()
    /// See below
    /// Note: If someone patches CompRefuelable's Notify_UsedThisTick()
    ///       it does NOT get called if set to Rare or Long.
    ///       (VERY unlikely to matter)
    /// 
    /// TODO: Set this up as an abstract parent "produce something
    ///       every unit of time" thing?
    /// </summary>
    public class Building_DeepQuarry : Building
    {
        public float GetProduceMtbHours { get { return def.GetModExtension<DeepQuarryDefModExtension>().TickCount; } }
        static IEnumerable<ThingDef> cachedPossibleRockDefCandidates;
        protected int productionTime=0; // number of ticks this has been running since last production check
        public CompFlickable flick;
        public CompPowerTrader power;
        public CompRefuelable fuel;
        public int ProducedChunksTotal = 0;

        public Building_DeepQuarry() : base() {
            flick = GetComp<CompFlickable>();
            power = GetComp<CompPowerTrader>();
            fuel = GetComp<CompRefuelable>();
        }

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

        /// <summary>
        /// Ticks, a Weighing of Benefits
        /// 
        /// When you create one of these, you must pick what Tick
        ///  interval it follows. The vanilla CompRefuelable only
        ///  runs at Tick but any of these Ticks will consume the
        ///  correct amount of fuel. So choose the interval based
        ///  on how you want the machine to behave
        /// 
        /// Shorter Tick duration gives you:
        ///  Good view of how much fuel is left (since it goes
        ///    down every tick)
        ///  
        /// Longer Tick duration gives you:
        ///  Better performance
        ///  Much better performance if you have a lot of these
        /// 
        /// </summary>
        public override void Tick() {
            base.Tick();
            if (flick == null || flick.SwitchIsOn) {
                if (power == null || power.PowerOn) {
                    if (fuel != null) {
                        fuel.Notify_UsedThisTick();
                        if (fuel.HasFuel) {
                            TryGenerateResource(1);
                        }
                    } else {  //fuel==null
                        TryGenerateResource(1);
                    }
                }
            }
        }
        public override void TickRare()
        {
            base.TickRare();
            if (flick == null || flick.SwitchIsOn) {
                if (power==null || power.PowerOn) {
                    if (fuel != null) {
                        // This option would not catch any harmony patches to
                        //   Notify_UsedThisTick();

                        // The fuel consuption per Tick is fuel.Props.fuelConsumptionRate/60000
                        //   (per CompRefuelable - note that we will miss any Harmony patches
                        //    that target fuelConsumptionRate. Grabbing the private result is
                        //    a TODO for later)
                        fuel.ConsumeFuel(fuel.Props.fuelConsumptionRate / 60000 * 250);
                        // Another (rather silly) option would be:
                        // for (int i=0; i<250; i++) fuel.Notify_UsedThisTick();
                        // As per Thornsworth:
                        //    dear god. do the calculation yourself
                        //    geeze
                        if (fuel.HasFuel)
                            TryGenerateResource(250);
                    } else {// fuel == null
                        TryGenerateResource(250);
                    }
                }
            }
            // possible TODO: allow consuming fuel during idle as an option?
            // possible TODO: make a comp that only consumes when powered/etc.
        }
        public override void TickLong()
        {
            base.TickLong();
            // Again, like rare, we must manually consume fuel. See notes above.
            if (flick == null || flick.SwitchIsOn) {
                if (power == null || power.PowerOn) {
                    if (fuel != null) {
                        // 2000 ticks in a long
                        fuel.ConsumeFuel(fuel.Props.fuelConsumptionRate / 60000 * 2000);
                        if (fuel.HasFuel)
                            TryGenerateResource(2000);
                    } else {// fuel == null
                        TryGenerateResource(2000);
                    }
                }
            }
        }

        public void TryGenerateResource(int ticksInInterval) {
            productionTime += ticksInInterval;
            if (productionTime >= 2000) {
                productionTime = 0;
                if (Rand.MTBEventOccurs(GetProduceMtbHours, GenDate.TicksPerHour, GenTicks.TickLongInterval)) {
                    GenerateChunk();
                }
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ProducedChunksTotal, "producedTotal");
            Scribe_Values.Look(ref productionTime, "PRF_productionTime");
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
