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

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class Building_BaseLimitation<T> : Building_BaseMachine<T>, IProductLimitation where T : Thing
    {
        public int ProductLimitCount { get => this.productLimitCount; set => this.productLimitCount = value; }
        public bool ProductLimitation { get => this.productLimitation; set => this.productLimitation = value; }
        private Option<SlotGroup> targetSlotGroup = Nothing<SlotGroup>();
        public Option<SlotGroup> TargetSlotGroup { get => targetSlotGroup; set => targetSlotGroup = value; }

        private int productLimitCount = 100;
        private bool productLimitation = false;

        private ILoadReferenceable slotGroupParent = null;
        private string slotGroupParentLabel = null;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.productLimitCount, "productLimitCount", 100);
            Scribe_Values.Look<bool>(ref this.productLimitation, "productLimitation", false);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.slotGroupParentLabel = this.targetSlotGroup.Select(s => s.parent.SlotYielderLabel()).GetOrDefault(null);
                this.slotGroupParent = this.targetSlotGroup.Select(s => s.parent).Select(p => p as ILoadReferenceable).GetOrDefault(null);
            }
            Scribe_References.Look<ILoadReferenceable>(ref this.slotGroupParent, "slotGroupParent");
            Scribe_Values.Look<string>(ref this.slotGroupParentLabel, "slotGroupParentLabel", null);
        }

        public override void PostMapInit()
        {
            this.targetSlotGroup = this.Map.haulDestinationManager.AllGroups.Where(g => g.parent.SlotYielderLabel() == this.slotGroupParentLabel).Where(g => Option(slotGroupParent).Fold(true)(p => p == g.parent)).FirstOption();
            base.PostMapInit();
        }

        public bool IsLimit(ThingDef def)
        {
            if (!this.ProductLimitation)
            {
                return false;
            }
            this.targetSlotGroup = this.targetSlotGroup.Where(s => this.Map.haulDestinationManager.AllGroups.Any(a => a == s));
            return this.targetSlotGroup.Fold(() => this.Map.resourceCounter.GetCount(def) >= this.ProductLimitCount)
                (s => s.HeldThings.Where(t => t.def == def).Select(t => t.stackCount).Sum() >= this.ProductLimitCount || !s.Settings.filter.Allows(def) || !s.CellsList.Any(c => c.GetFirstItem(this.Map) == null || c.GetFirstItem(this.Map).def == def));
        }

        public bool IsLimit(Thing thing)
        {
            if (!this.ProductLimitation)
            {
                return false;
            }
            this.targetSlotGroup = this.targetSlotGroup.Where(s => this.Map.haulDestinationManager.AllGroups.Any(a => a == s));
            return this.targetSlotGroup.Fold(() => this.Map.resourceCounter.GetCount(thing.def) >= this.ProductLimitCount)
                (s => s.HeldThings.Where(t => t.def == thing.def).Select(t => t.stackCount).Sum() >= this.ProductLimitCount || !s.CellsList.Any(c => c.IsValidStorageFor(this.Map, thing)));
        }
    }
}
