using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_ItemPuller : Building_BaseLimitation<Thing>, IStorageSetting
    {
        public ThingFilter Filter { get => this.filter; }

        private ThingFilter filter = new ThingFilter();
        private bool active = false;
        public override Graphic Graphic => Option(base.Graphic as Graphic_Selectable).Fold(base.Graphic)(g => g.Get(this.def.graphicData.texPath + "/Puller" + (this.active ? "1" : "0")));

        [Unsaved]
        private StorageSettings storageSettings;
        public StorageSettings StorageSettings => this.storageSettings;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look<ThingFilter>(ref this.filter, "filter");
            Scribe_Values.Look<bool>(ref this.active, "active", false);

            if (this.filter == null) this.filter = new ThingFilter();

            this.storageSettings = new StorageSettings { filter = this.filter };
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                this.filter = new ThingFilter();
                this.filter.SetAllowAll(null);
                this.storageSettings = new StorageSettings { filter = this.filter };
            }
        }

        protected override TargetInfo ProgressBarTarget()
        {
            return this;
        }

        private Option<Thing> TargetThing()
        {
            return (this.Position + this.Rotation.Opposite.FacingCell).SlotGroupCells(this.Map)
                .SelectMany(c => c.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Item)
                .Where(t => this.filter.Allows(t))
                .Where(t => !this.IsLimit(t))
                .FirstOption();
        }

        public override IntVec3 OutputCell()
        {
            return (this.Position + this.Rotation.FacingCell);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            var act = new Command_Toggle();
            act.isActive = () => this.active;
            act.toggleAction = () => this.active = !this.active;
            act.defaultLabel = "PRF.AutoMachineTool.Puller.SwitchActiveLabel".Translate();
            act.defaultDesc = "PRF.AutoMachineTool.Puller.SwitchActiveDesc".Translate();
            act.icon = RS.PlayIcon;
            yield return act;
        }

        protected override bool IsActive()
        {
            return base.IsActive() && this.active;
        }

        protected override bool WorkInterruption(Thing working)
        {
            return !working.Spawned || working.Destroyed;
        }

        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            workAmount = 120;
            target = TargetThing().GetOrDefault(null);
            return target != null;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            var target = new List<Thing>();
            target.Append(working);
            products = target;
            return true;
        }
    }

    public class Building_ItemPullerCellResolver : IOutputCellResolver, IInputCellResolver
    {
        public ModExtension_WorkIORange Parent { get; set; }

        public Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern)
        {
            return this.Parent.GetCellPatternColor(cellPattern);
        }

        public Option<IntVec3> InputCell(IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return Option(FacingCell(center, size, rot.Opposite));
        }

        private static readonly List<IntVec3> EmptyList = new List<IntVec3>();

        public IEnumerable<IntVec3> InputZoneCells(IntVec3 cell, IntVec2 size, Map map, Rot4 rot)
        {
            return InputCell(cell, size, map, rot).Select(c => c.SlotGroupCells(map)).GetOrDefault(EmptyList);
        }

        public Option<IntVec3> OutputCell(IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return Option(FacingCell(center, size, rot));
        }

        public IEnumerable<IntVec3> OutputZoneCells(IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return OutputCell(center, size, map, rot).Select(c => c.SlotGroupCells(map)).GetOrDefault(EmptyList);
        }
    }
}
