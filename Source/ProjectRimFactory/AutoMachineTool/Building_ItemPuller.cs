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
        public override Graphic Graphic => Option(base.Graphic as Graphic_Selectable).Fold(base.Graphic)(g => g.Get(this.GetTexPath()));

        public string GetTexPath()
        {
            var path = this.def.graphicData.texPath + "/Puller" + (this.active ? "1" : "0");
            if (this.OutputSides)
            {
                path += this.right ? "_r" : "_l";
            }
            return path;
        }

        [Unsaved]
        private StorageSettings storageSettings;
        public StorageSettings StorageSettings => this.storageSettings;

        private bool ForcePlace => this.def.GetModExtension<ModExtension_Testing>()?.forcePlacing ?? false;

        private bool right = false;

        private bool OutputSides => this.def.GetModExtension<ModExtension_Puller>()?.outputSides ?? false;

        private bool pickupConveyor = false;

        protected override bool WorkingIsDespawned()
        {
            return false;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.pickupConveyor, "pickupConveyor", false);

            base.ExposeData();

            Scribe_Deep.Look<ThingFilter>(ref this.filter, "filter");
            Scribe_Values.Look<bool>(ref this.active, "active", false);
            Scribe_Values.Look<bool>(ref this.right, "right", false);

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
            this.forcePlace = this.ForcePlace;
        }

        protected override void Reset()
        {
            base.Reset();
            this.pickupConveyor = false;
        }

        protected override TargetInfo ProgressBarTarget()
        {
            return this;
        }

        private Option<Thing> TargetThing()
        {
            var conveyor = this.GetPickableConveyor();
            if (conveyor.HasValue)
            {
                this.pickupConveyor = true;
                return Option(conveyor.Value.Carrying());
            }
            return (this.Position + this.Rotation.Opposite.FacingCell).SlotGroupCells(this.Map)
                .SelectMany(c => c.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Item)
                .Where(t => this.filter.Allows(t))
                .Where(t => !this.IsLimit(t))
                .FirstOption();
        }

        private Option<Building_BeltConveyor> GetPickableConveyor()
        {
            return (this.Position + this.Rotation.Opposite.FacingCell).GetThingList(this.Map)
                .Where(t => t.def.category == ThingCategory.Building)
                .SelectMany(t => Option(t as Building_BeltConveyor))
                .Where(b => !b.IsUnderground)
                .Where(b => b.Carrying() != null)
                .Where(b => this.filter.Allows(b.Carrying()))
                .Where(b => !this.IsLimit(b.Carrying()))
                .FirstOption();
        }

        public override IntVec3 OutputCell()
        {
            if(this.OutputSides)
            {
                RotationDirection dir = RotationDirection.Clockwise;
                if (!this.right)
                {
                    dir = RotationDirection.Counterclockwise;
                }
                return this.Position + this.Rotation.RotateAsNew(dir).FacingCell;
            }
            else
            {
                return this.Position + this.Rotation.FacingCell;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            yield return new Command_Toggle()
            {
                isActive = () => this.active,
                toggleAction = () => this.active = !this.active,
                defaultLabel = "PRF.AutoMachineTool.Puller.SwitchActiveLabel".Translate(),
                defaultDesc = "PRF.AutoMachineTool.Puller.SwitchActiveDesc".Translate(),
                icon = RS.PlayIcon
            };
            if (this.OutputSides)
            {
                yield return new Command_Action()
                {
                    action = () => this.right = !this.right,
                    defaultLabel = "PRF.AutoMachineTool.Puller.SwitchOutputSideLabel".Translate(),
                    defaultDesc = "PRF.AutoMachineTool.Puller.SwitchOutputSideDesc".Translate(),
                    icon = RS.OutputDirectionIcon
                };
            }
        }

        protected override bool IsActive()
        {
            return base.IsActive() && this.active;
        }

        protected override bool WorkInterruption(Thing working)
        {
            return this.pickupConveyor ? !this.GetPickableConveyor().HasValue : !working.Spawned || working.Destroyed;
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
            if (this.pickupConveyor)
            {
                var pickup = GetPickableConveyor().Select(c => c.Pickup());
                if (pickup.HasValue)
                {
                    target.Append(pickup.Value);
                }
                else
                {
                    this.ForceReady();
                }
            }
            else
            {
                target.Append(working);
            }
            products = target;
            return true;
        }
    }

    public class Building_ItemPullerInputCellResolver : IInputCellResolver
    {
        public ModExtension_WorkIORange Parent { get; set; }

        public Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern)
        {
            return this.Parent.GetCellPatternColor(cellPattern);
        }

        public Option<IntVec3> InputCell(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return Option(FacingCell(center, size, rot.Opposite));
        }

        private static readonly List<IntVec3> EmptyList = new List<IntVec3>();

        public IEnumerable<IntVec3> InputZoneCells(ThingDef def, IntVec3 cell, IntVec2 size, Map map, Rot4 rot)
        {
            return InputCell(def, cell, size, map, rot).Select(c => c.SlotGroupCells(map)).GetOrDefault(EmptyList);
        }
    }

    public class Building_ItemPullerOutputCellResolver : ProductOutputCellResolver
    {
        public override Option<IntVec3> OutputCell(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            IntVec3 defaultCell = IntVec3.Zero;
            if (def.GetModExtension<ModExtension_Puller>()?.outputSides ?? false)
            {
                defaultCell = center + rot.RotateAsNew(RotationDirection.Counterclockwise).FacingCell;

            }
            else
            {
                defaultCell = center + rot.FacingCell;
            }

            return Option(base.OutputCell(def, center, size, map, rot).GetOrDefault(defaultCell));
        }
    }
}
