using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_Slaughterhouse : Building_BaseRange<Pawn>, ISlaughterhouse
    {

        public Dictionary<ThingDef, SlaughterSettings> Settings => slaughterSettings;

        private Dictionary<ThingDef, SlaughterSettings> slaughterSettings = new();

        public override bool ProductLimitationDisable => true;

        public override IntVec3 OutputCell()
        {
            return CompOutputAdjustable.CurrentCell;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!PowerWorkSetting.Props.allowManualRangeTypeChange)
            {
                PowerWorkSetting.RangeTypeRot = Rotation;
            }

        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref slaughterSettings, "slaughterSettings", LookMode.Def, LookMode.Deep);
        }

        protected override void Reset()
        {
            if (Working != null && Working.jobs != null && Working.jobs.curJob != null && Working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                Working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            base.Reset();
        }
        
        // TODO take another look at this. this seems to get all pawns for Slaughter from the entire map
        private HashSet<Pawn> ShouldSlaughterPawns()
        {
            var mapPawns = Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            return slaughterSettings.Values.Where(s => s.doSlaughter).SelectMany(s =>
            {
                var pawns = mapPawns.Where(p => p.def == s.def);
                Func<Pawn, bool> where = (p) =>
                {
                    var result = true;
                    if (!s.hasBonds) result = p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond) == null;
                    if (result && !s.pregnancy) result = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Pregnant, true) == null;
                    if (result && !s.trained) result = !p.training.HasLearned(TrainableDefOf.Obedience);
                    return result;
                };
                Func<IEnumerable<Pawn>, bool, IOrderedEnumerable<Pawn>> orderBy = (e, adult) =>
                {
                    if (adult) return e.OrderByDescending(p => p.ageTracker.AgeChronologicalTicks);
                    else return e.OrderBy(p => p.ageTracker.AgeChronologicalTicks);
                };
                return new[] { new { Gender = Gender.Male, Adult = true }, new { Gender = Gender.Female, Adult = true }, new { Gender = Gender.Male, Adult = false }, new { Gender = Gender.Female, Adult = false }, new { Gender = Gender.None, Adult = false }, new { Gender = Gender.None, Adult = true } }
                    .Select(a => new { Group = a, Pawns = pawns.Where(p => p.gender == a.Gender && p.IsAdult() == a.Adult) })
                    .Select(g => new { Group = g.Group, Pawns = g.Pawns, SlaughterCount = g.Pawns.Count() - s.KeepCount(g.Group.Gender, g.Group.Adult) })
                    .Where(g => g.SlaughterCount > 0)
                    .SelectMany(g => orderBy(g.Pawns.Where(where), g.Group.Adult).Take(g.SlaughterCount));
            }).ToHashSet();
        }

        protected override bool WorkInterruption(Pawn working)
        {
            return working.Dead || !working.Spawned;
        }

        protected override bool TryStartWorking(out Pawn target, out float workAmount)
        {
            workAmount = 400f;
            target = null;
            var tmp = GetTargetCells()
                .SelectMany(c => c.GetThingList(Map))
                .Where(t => t.def.category == ThingCategory.Pawn)
                .SelectMany(t => Option(t as Pawn))
                .Where(p => slaughterSettings.ContainsKey(p.def));
            if (!tmp.FirstOption().HasValue)
            {
                return false;
            }
            var targets = ShouldSlaughterPawns();
            
            target = tmp.Where(p => targets.Contains(p))
                .FirstOption()
                .GetOrDefault(null);
            if (target != null)
            {
                PawnUtility.ForceWait(target, 15000, null, true);
            }
            return target != null;
        }

        protected override bool FinishWorking(Pawn working, out List<Thing> outputProducts)
        {
            if (working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            var num = Mathf.Max(GenMath.RoundRandom(working.BodySize * 8f), 1);
            for (var i = 0; i < num; i++)
            {
                working.health.DropBloodFilth();
            }
            Map.designationManager.AddDesignation(new Designation(working, DesignationDefOf.Slaughter));
            working.Kill(new DamageInfo(DamageDefOf.ExecutionCut, 0));
            outputProducts = new List<Thing>().Append(working.Corpse);
            working.Corpse.DeSpawn();
            working.Corpse.SetForbidden(false);
            return true;
        }

    }

}
