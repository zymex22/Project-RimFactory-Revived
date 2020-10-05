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
using ProjectRimFactory.Common;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_ItemPuller : Building_BaseLimitation<Thing>, IStorageSetting, IStoreSettingsParent
    {
        protected bool active = false;
        protected bool takeForbiddenItems=true;
        public override Graphic Graphic => this.def.GetModExtension<ModExtension_Graphic>()?.GetByName(GetGraphicName()) ?? base.Graphic;

        private string GetGraphicName()
        {
            string name = null;
            if (this.OutputSides)
            {
                name += this.right ? "Right" : "Left";
            }
            if (this.active)
            {
                name += "Working";
            }
            return name;
        }

        public bool StorageTabVisible => true;

        public StorageSettings settings;

        public StorageSettings GetStoreSettings()
        {
            if (settings == null)
            {
                settings = new StorageSettings();
                //To "Prevent" a null Refrence as GetParentStoreSettings() seems to be null on first Placing the Building
                if (GetParentStoreSettings() != null) { 
                    settings.CopyFrom(GetParentStoreSettings());
                }
            }
            return settings;
        }
        // See ExposeData for what this is:
        private ThingFilter backCompatibilityFilter;

        public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;


        [Unsaved]
        protected StorageSettings storageSettings;
        public StorageSettings StorageSettings => this.storageSettings;

        private bool ForcePlace => this.def.GetModExtension<ModExtension_Testing>()?.forcePlacing ?? false;

        private bool right = false;

        private bool OutputSides => this.def.GetModExtension<ModExtension_Puller>()?.outputSides ?? false;

        private bool pickupConveyor = false;

        protected override LookMode WorkingLookMode { get => LookMode.Deep; } // despawned
        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.pickupConveyor, "pickupConveyor", false);

            base.ExposeData();

            Scribe_Values.Look<bool>(ref this.active, "active", false);
            Scribe_Values.Look<bool>(ref this.right, "right", false);
            Scribe_Deep.Look(ref settings, "settings", new object[] { this });
            Scribe_Values.Look<bool>(ref this.takeForbiddenItems, "takeForbidden", true);

            if (Scribe.mode != LoadSaveMode.Saving) {
                // The old filter settings were saved as a ThingFilter under the key 'filter'
                //   We test for that filter on load and if they exist, we populate the settings 
                //   with it so no one complains about "oh my puller filter went away!"
                //   We can phase this out any time after 1 Feb 2021 - I won't feel bad about
                //   losing someone's settings if they don't play for 6 months. Or, you know,
                //    sometime after that.   --LWM
                //   (also remove the field above when removing this)
                Scribe_Deep.Look(ref this.backCompatibilityFilter, "filter");
                if (backCompatibilityFilter!=null && Scribe.mode==LoadSaveMode.ResolvingCrossRefs) {
                    // filter should be done loading by now.
                    Log.Message("Project RimFactory: updating puller filter to new settings");
                    if (settings==null) {
                        settings = new StorageSettings(this);
                    }
                    settings.filter = backCompatibilityFilter;
                }
            }

        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.settings = GetStoreSettings(); // force init
            this.forcePlace = ForcePlace;
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

        protected virtual Option<Thing> TargetThing()
        {
            var conveyor = this.GetPickableConveyor();
            if (conveyor.HasValue)
            {
                this.pickupConveyor = true;
                return Option(conveyor.Value.GetThingBy(null)); // already verified it's what we want
            }
            if (this.takeForbiddenItems)
                return (this.Position + this.Rotation.Opposite.FacingCell).SlotGroupCells(this.Map)
                    .SelectMany(c => c.GetThingList(this.Map))
                    .Where(t => t.def.category == ThingCategory.Item)
                    .Where(t => this.settings.AllowedToAccept(t))
                    .Where(t => !this.IsLimit(t))
                    .FirstOption();
            else
                return (this.Position + this.Rotation.Opposite.FacingCell).SlotGroupCells(this.Map)
                    .SelectMany(c => c.GetThingList(this.Map))
                    .Where(t => t.def.category == ThingCategory.Item)
                    .Where(t => !t.IsForbidden(Faction.OfPlayer))
                    .Where(t => this.settings.AllowedToAccept(t))
                    .Where(t => !this.IsLimit(t))
                    .FirstOption();
        }

        private Option<Building_BeltConveyor> GetPickableConveyor()
        {
            if (this.takeForbiddenItems)
                return (this.Position + this.Rotation.Opposite.FacingCell).GetThingList(this.Map)
                    .OfType<Building_BeltConveyor>() // get any conveyors, also casts to conveyors
                    .Where(b => !b.IsUnderground && b.Carrying() != null)
                    .Where(b => this.settings.AllowedToAccept(b.Carrying()))
                    .Where(b => !this.IsLimit(b.Carrying()))
                    .FirstOption();
            else
                return (this.Position + this.Rotation.Opposite.FacingCell).GetThingList(this.Map)
                    .OfType<Building_BeltConveyor>() // get any conveyors, also casts to conveyors
                    .Where(b => !b.IsUnderground && b.Carrying() != null)
                    .Where(b => !b.Carrying().IsForbidden(Faction.OfPlayer))
                    .Where(b => this.settings.AllowedToAccept(b.Carrying()))
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
            foreach (Gizmo g2 in StorageSettingsClipboard.CopyPasteGizmosFor(settings))
                yield return g2;
            yield return new Command_Toggle()
            {
                isActive = () => this.active,
                toggleAction = () => this.active = !this.active,
                defaultLabel = "PRF.AutoMachineTool.Puller.SwitchActiveLabel".Translate(),
                defaultDesc = "PRF.AutoMachineTool.Puller.SwitchActiveDesc".Translate(),
                icon = RS.PlayIcon
            };
            yield return new Command_Toggle()
            {
                isActive = () => this.takeForbiddenItems,
                toggleAction = () => this.takeForbiddenItems = !this.takeForbiddenItems,
                defaultLabel = "PRF.Puller.TakeForbiddenItems".Translate(),
                defaultDesc  = "PRF.Puller.TakeForbiddenItemsDesc".Translate(),
                icon = TexCommand.ForbidOff
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
            return false;
            //return this.pickupConveyor ? !this.GetPickableConveyor().HasValue : !working.Spawned || working.Destroyed;
        }

        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            workAmount = 120;
            target = TargetThing().GetOrDefault(null);
            if (target?.Spawned == true) target.DeSpawn();
            return target != null;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            // why do we need to create a *new* list???  Why not just append
            //   directly to this.products()??  It IS the C# object-oriented
            //   way (altho, if Nobo comes from a background where variables
            //   are immutable that might explain the choice?) Nevertheless,
            //   I will use and return the current instantiation of products
            this.products.Append(working);
            products = this.products;
            return true;
        }
        protected override void Placing() {
            // unforbid any items picked up before they are put down:
            if (!products.NullOrEmpty()) {
                foreach (Thing t in products)
                    if (t.IsForbidden(Faction.OfPlayer))
                        t.SetForbidden(false);
            }
            base.Placing();
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
