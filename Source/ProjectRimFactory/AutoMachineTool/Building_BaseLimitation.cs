using System.Linq;
using RimWorld;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class Building_BaseLimitation<T> : Building_BaseMachine<T>, IProductLimitation where T : Thing
    {
        private bool countStacks;
        private bool productLimitation;

        private int productLimitCount = 100;

        private ILoadReferenceable slotGroupParent;
        private string slotGroupParentLabel;

        public int ProductLimitCount
        {
            get => productLimitCount;
            set => productLimitCount = value;
        }

        public bool ProductLimitation
        {
            get => productLimitation;
            set => productLimitation = value;
        }

        public Option<SlotGroup> TargetSlotGroup { get; set; } = Nothing<SlotGroup>();

        public bool CountStacks
        {
            get => countStacks;
            set => countStacks = value;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref productLimitCount, "productLimitCount", 100);
            Scribe_Values.Look(ref productLimitation, "productLimitation");
            Scribe_Values.Look(ref countStacks, "countStacks");

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                slotGroupParentLabel = TargetSlotGroup.Select(s => s.parent.SlotYielderLabel()).GetOrDefault(null);
                slotGroupParent = TargetSlotGroup.Select(s => s.parent).Select(p => p as ILoadReferenceable)
                    .GetOrDefault(null);
            }

            Scribe_References.Look(ref slotGroupParent, "slotGroupParent");
            Scribe_Values.Look(ref slotGroupParentLabel, "slotGroupParentLabel");
        }

        public override void PostMapInit()
        {
            TargetSlotGroup = Map.haulDestinationManager.AllGroups
                .Where(g => g.parent.SlotYielderLabel() == slotGroupParentLabel)
                .Where(g => Option(slotGroupParent).Fold(true)(p => p == g.parent)).FirstOption();
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
            if (!ProductLimitation) return false;
            //IsGoodStoreCell also checks for fire.  Let's use IsValidStorageFor instead!
            TargetSlotGroup = TargetSlotGroup.Where(
                s => Map.haulDestinationManager.AllGroups.Contains(s));
            return TargetSlotGroup.Fold(() => CountFromMap(thing.def) >= ProductLimitCount) // no slotGroup
            (s => CountFromSlot(s, thing.def) >= ProductLimitCount
                  || !s.CellsList.Any(c => c.IsValidStorageFor(Map, thing)));
        }

        private int CountFromMap(ThingDef def)
        {
            return countStacks ? Map.listerThings.ThingsOfDef(def).Count : Map.resourceCounter.GetCount(def);
        }

        private int CountFromSlot(SlotGroup s, ThingDef def)
        {
            return countStacks
                ? s.HeldThings.Where(t => t.def == def).Count()
                : s.HeldThings.Where(t => t.def == def).Select(t => t.stackCount).Sum();
        }
    }
}