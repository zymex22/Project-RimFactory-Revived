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
        public bool CountStacks { get => this.countStacks; set => this.countStacks = value; }

        private int productLimitCount = 100;
        private bool productLimitation = false;
        private bool countStacks = false;

        private ILoadReferenceable slotGroupParent = null;
        private string slotGroupParentLabel = null;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.productLimitCount, "productLimitCount", 100);
            Scribe_Values.Look<bool>(ref this.productLimitation, "productLimitation", false);
            Scribe_Values.Look<bool>(ref this.countStacks, "countStacks", false);
            
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

        /* Use IsLimit(Thing thing) below
        [Obsolete("Warning, using IsLimit(ThingDef def) instead of (Thing t) does not work with all storage mods.")]
        public bool IsLimit(ThingDef def)
        {
            if (!this.ProductLimitation)
            {
                return false;
            }
            this.targetSlotGroup = this.targetSlotGroup.Where(s => this.Map.haulDestinationManager.AllGroups.Contains(s));
            return this.targetSlotGroup.Fold(() => this.CountFromMap(def) >= this.ProductLimitCount) // no slotGroup
                (s => !s.Settings.filter.Allows(def)
                || this.CountFromSlot(s, def) >= this.ProductLimitCount 
                || !s.CellsList.Any(c => c.GetFirstItem(this.Map) == null 
                || c.GetFirstItem(this.Map).def == def)); // this is broken anyway.  What if it's a full stack?
        }
        */

        // TODO: This may need to be cached somehow! (possibly by map?)
        // returns true if there IS something that limits adding this thing to storage.
        public bool IsLimit(Thing thing)
        {
            if (!this.ProductLimitation)
            {
                return false;
            }
            //IsGoodStoreCell also checks for fire.  Let's use IsValidStorageFor instead!
            this.targetSlotGroup = this.targetSlotGroup.Where(s => this.Map.haulDestinationManager.AllGroups.Contains(s);
            return this.targetSlotGroup.Fold(() => this.CountFromMap(thing.def) >= this.ProductLimitCount) // no slotGroup
                (s => this.CountFromSlot(s, thing.def) >= this.ProductLimitCount 
                || !s.CellsList.Any(c => c.IsValidStorageFor(this.Map, thing)));
        }

        private int CountFromMap(ThingDef def)
        {
            return this.countStacks ? this.Map.listerThings.ThingsOfDef(def).Count : this.Map.resourceCounter.GetCount(def);
        }

        private int CountFromSlot(SlotGroup s, ThingDef def)
        {
            return this.countStacks ? s.HeldThings.Where(t => t.def == def).Count() : s.HeldThings.Where(t => t.def == def).Select(t => t.stackCount).Sum();
        }
    }
}
