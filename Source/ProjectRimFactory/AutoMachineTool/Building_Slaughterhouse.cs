using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_Slaughterhouse : Building_BaseRange<Pawn>, PRF_SettingsContentLink, ISlaughterhouse
    {
        PRF_SettingsContent PRF_SettingsContentLink.PRF_SettingsContentOb => new ITab_Slaughterhouse_Def(this);

        public Dictionary<ThingDef, SlaughterSettings> Settings { get => this.slaughterSettings; }

    

        private Dictionary<ThingDef, SlaughterSettings> slaughterSettings = new Dictionary<ThingDef, SlaughterSettings>();

        private int outputIndex = 0;

        private IntVec3[] adjacent;

        public override IntVec3 OutputCell()
        {
            return this.adjacent[this.outputIndex];
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.adjacent = GenAdj.CellsAdjacent8Way(this).ToArray();
            if (!respawningAfterLoad)
            {
                this.outputIndex = Mathf.Max(0, this.adjacent.FirstIndexOf(c => c == base.OutputCell()));
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            var direction = new Command_Action();
            direction.action = () =>
            {
                if (this.outputIndex + 1 >= this.adjacent.Count())
                {
                    this.outputIndex = 0;
                }
                else
                {
                    this.outputIndex++;
                }
            };
            direction.activateSound = SoundDefOf.Checkbox_TurnedOn;
            direction.defaultLabel = "PRF.AutoMachineTool.SelectOutputDirectionLabel".Translate();
            direction.defaultDesc = "PRF.AutoMachineTool.SelectOutputDirectionDesc".Translate();
            direction.icon = RS.OutputDirectionIcon;
            yield return direction;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.outputIndex, "outputIndex");
            Scribe_Collections.Look<ThingDef, SlaughterSettings>(ref this.slaughterSettings, "slaughterSettings", LookMode.Def, LookMode.Deep);
        }

        protected override void Reset()
        {
            if (this.Working != null && this.Working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                this.Working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            base.Reset();
        }

        private HashSet<Pawn> ShouldSlaughterPawns()
        {
            var mapPawns = this.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            return this.slaughterSettings.Values.Where(s => s.doSlaughter).SelectMany(s =>
            {
                var pawns = mapPawns.Where(p => p.def == s.def);
                Func<Pawn, bool> where = (p) =>
                {
                    bool result = true;
                    if (result && !s.hasBonds) result = p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond) == null;
                    if (result && !s.pregnancy) result = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Pregnant, true) == null;
                    if (result && !s.trained) result = !p.training.HasLearned(TrainableDefOf.Obedience);
                    return result;
                };
                Func<IEnumerable<Pawn>, bool, IOrderedEnumerable<Pawn>> orderBy = (e, adult) =>
                {
                    if (adult) return e.OrderByDescending(p => p.ageTracker.AgeChronologicalTicks);
                    else return e.OrderBy(p => p.ageTracker.AgeChronologicalTicks);
                };
                return new[] { new { Gender = Gender.Male, Adult = true }, new { Gender = Gender.Female, Adult = true }, new { Gender = Gender.Male, Adult = false }, new { Gender = Gender.Female, Adult = false } }
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
                .SelectMany(c => c.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Pawn)
                .SelectMany(t => Option(t as Pawn))
                .Where(p => !InWorking(p))
                .Where(p => this.slaughterSettings.ContainsKey(p.def));
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

        protected override bool FinishWorking(Pawn working, out List<Thing> products)
        {
            if (working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            int num = Mathf.Max(GenMath.RoundRandom(working.BodySize * 8f), 1);
            for (int i = 0; i < num; i++)
            {
                working.health.DropBloodFilth();
            }
            Map.designationManager.AddDesignation(new Designation(working, DesignationDefOf.Slaughter));
            working.Kill(new DamageInfo(DamageDefOf.ExecutionCut, 0));
            products = new List<Thing>().Append(working.Corpse);
            working.Corpse.DeSpawn();
            working.Corpse.SetForbidden(false);
            return true;
        }


      
    }

    public class Building_SlaughterhouseTargetCellResolver : BaseTargetCellResolver
    {
        public override IEnumerable<IntVec3> GetRangeCells(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot, int range)
        {
            return FacingRect(center, size, rot, range)
                .Where(c => FacingCell(center, size, rot).GetRoom(map) == c.GetRoom(map));
        }

        public override int GetRange(float power)
        {
            return Mathf.RoundToInt(power / 500) + 1;
        }

        public override bool NeedClearingCache => true;
    }
}
