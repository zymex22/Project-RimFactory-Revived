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
    public class Building_Slaughterhouse : Building_BaseRange<Pawn>, PRF_SettingsContent
    {


        public Dictionary<ThingDef, SlaughterSettings> Settings { get => this.slaughterSettings; }

        public float ITab_Settings_Minimum_x => 800f;

        public float ITab_Settings_Additional_y => 600f;

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


        private static readonly float[] ColumnWidth = new float[] { 0.2f, 0.05f, 0.05f, 0.05f, 0.05f, 0.15f, 0.15f, 0.15f, 0.15f };

        private Func<float, Rect> CutLeftFunc(Rect rect)
        {
            var curX = 0f;
            return (pct) =>
            {
                var r = new Rect(curX, rect.y, rect.width * pct, rect.height);
                curX += rect.width * pct;
                return r;
            };
        }

        private static TipSignal slaughterTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.DoSlaughterTip".Translate());
        private static TipSignal hasBondsTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.BondsTip".Translate());
        private static TipSignal pregnancyTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.PregnancyTip".Translate());
        private static TipSignal trainedTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.TrainedTip".Translate());

        private static TipSignal keepMaleChildCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Male".Translate(), "PRF.AutoMachineTool.Young".Translate()));
        private static TipSignal keepFemaleChildCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Female".Translate(), "PRF.AutoMachineTool.Young".Translate()));
        private static TipSignal keepMaleAdultCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Male".Translate(), "PRF.AutoMachineTool.Adult".Translate()));
        private static TipSignal keepFemaleAdultCountTip = new TipSignal("PRF.AutoMachineTool.Slaughterhouse.Setting.KeepCountTip".Translate("PRF.AutoMachineTool.Female".Translate(), "PRF.AutoMachineTool.Adult".Translate()));

        private Vector2 scrollPosition;
        private string description = "PRF.AutoMachineTool.Slaughterhouse.Setting.Description".Translate();

        private List<ThingDef> defs;
        public Listing_Standard ITab_Settings_AppendContent(Listing_Standard list)
        {
            defs = Find.CurrentMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal && p.RaceProps.IsFlesh && p.SpawnedOrAnyParentSpawned).Select(p => p.def).Distinct().ToList();

            var rect = list.GetRect(40f);
            Rect outRect = new Rect(0f, 0f, ITab_Settings_Minimum_x,ITab_Settings_Additional_y).ContractedBy(10f);
            Widgets.Label(rect, this.description);

            var headerRect = list.GetRect(24f);
            headerRect.width -= 30f;

            var cutLeftHeader = CutLeftFunc(headerRect);

            int colIndex = 0;
            var col = cutLeftHeader(ColumnWidth[colIndex++]);
            Widgets.Label(col, "PRF.AutoMachineTool.RaceName".Translate());

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.SlaughterIcon);
            TooltipHandler.TipRegion(col, slaughterTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.BondIcon);
            TooltipHandler.TipRegion(col, hasBondsTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.PregnantIcon);
            TooltipHandler.TipRegion(col, pregnancyTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.TrainedIcon);
            TooltipHandler.TipRegion(col, trainedTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.MaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.YoungIcon);
            TooltipHandler.TipRegion(col, keepMaleChildCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.FemaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.YoungIcon);
            TooltipHandler.TipRegion(col, keepFemaleChildCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.MaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.AdultIcon);
            TooltipHandler.TipRegion(col, keepMaleAdultCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), RS.FemaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), RS.AdultIcon);
            TooltipHandler.TipRegion(col, keepFemaleAdultCountTip);

            var scrollOutRect = list.GetRect(outRect.height - list.CurHeight);
            var scrollViewRect = new Rect(scrollOutRect.x, scrollOutRect.y, scrollOutRect.width - 30f, this.defs.Count() * 36f);

            

            Widgets.BeginScrollView(scrollOutRect, ref this.scrollPosition, scrollViewRect);
            var innerlist = new Listing_Standard();
            innerlist.Begin(scrollViewRect);
            this.defs.ForEach(d =>
            {
                innerlist.GapLine();

                var rowRect = innerlist.GetRect(24f);

                SlaughterSettings s = null;
                Settings.TryGetValue(d, out s);
                if (s == null)
                {
                    s = new SlaughterSettings();
                    s.def = d;
                    Settings[d] = s;
                }
                var cutLeft = CutLeftFunc(rowRect);

                colIndex = 0;
                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Label(col, s.def.label);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.doSlaughter);
                TooltipHandler.TipRegion(col, slaughterTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.hasBonds, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, hasBondsTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.pregnancy, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, pregnancyTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.trained, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, trainedTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                string buf1 = s.keepMaleYoungCount.ToString();
                Widgets.TextFieldNumeric<int>(col, ref s.keepMaleYoungCount, ref buf1, 0, 1000);
                TooltipHandler.TipRegion(col, keepMaleChildCountTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                string buf2 = s.keepFemaleYoungCount.ToString();
                Widgets.TextFieldNumeric<int>(col, ref s.keepFemaleYoungCount, ref buf2, 0, 1000);
                TooltipHandler.TipRegion(col, keepFemaleChildCountTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                string buf3 = s.keepMaleAdultCount.ToString();
                Widgets.TextFieldNumeric<int>(col, ref s.keepMaleAdultCount, ref buf3, 0, 1000);
                TooltipHandler.TipRegion(col, keepMaleAdultCountTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                string buf4 = s.keepFemaleAdultCount.ToString();
                Widgets.TextFieldNumeric<int>(col, ref s.keepFemaleAdultCount, ref buf4, 0, 1000);
            });

            innerlist.End();
            Widgets.EndScrollView();
            return list;


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
