using ProjectRimFactory.Common;
using RimWorld;
using System.Linq;
using ProjectRimFactory.Common.HarmonyPatches;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class Building_BaseLimitation<T> : Building_BaseMachine<T>, IProductLimitation where T : Thing
    {
        public int ProductLimitCount { get => productLimitCount; set => productLimitCount = value; }
        public bool ProductLimitation { get => productLimitation; set => productLimitation = value; }
        private SlotGroup targetSlotGroup = null;
        public SlotGroup TargetSlotGroup { get => targetSlotGroup; set => targetSlotGroup = value; }
        public bool CountStacks { get => countStacks; set => countStacks = value; }
        public virtual bool ProductLimitationDisable => false;

        private int productLimitCount = 100;
        private bool productLimitation = false;
        private bool countStacks = false;

        private ILoadReferenceable slotGroupParent = null;
        private string slotGroupParentLabel = null;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref productLimitCount, "productLimitCount", 100);
            Scribe_Values.Look(ref productLimitation, "productLimitation", false);
            Scribe_Values.Look(ref countStacks, "countStacks", false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                slotGroupParentLabel = targetSlotGroup?.parent?.SlotYielderLabel();
                slotGroupParent = targetSlotGroup?.parent as ILoadReferenceable;
            }
            Scribe_References.Look(ref slotGroupParent, "slotGroupParent");
            Scribe_Values.Look(ref slotGroupParentLabel, "slotGroupParentLabel", null);
        }

        public override void PostMapInit()
        {
            //Maybe rewrite that
            //From my understanding this gets that saved slot group
            targetSlotGroup = Map.haulDestinationManager.AllGroups
                .Where(g => g.parent.SlotYielderLabel() == slotGroupParentLabel)
                .Where(g => Option(slotGroupParent).Fold(true)(p => p == g.parent)).FirstOption().Value;
            base.PostMapInit();
        }

        // TODO: This may need to be cached somehow! (possibly by map?)
        // returns true if there IS something that limits adding this thing to storage.
        public bool IsLimit(Thing thing)
        {
            if (!productLimitation) return false;
            
            if (targetSlotGroup == null)
            {
                return CountFromMap(thing.def) >= productLimitCount;
            }
            
            // Use the faster limitWatcher if Available
            if (targetSlotGroup.parent is ILimitWatcher limitWatcher)
            {
                if (limitWatcher.ItemIsLimit(thing.def,countStacks, productLimitCount)) return true;
            }
            else
            {
                if (CheckSlotGroup(targetSlotGroup, thing.def, productLimitCount)) return true;
            }
            
            // Disable Accepts Patch override for this call(s) of IsValidStorageFor
            PatchStorageUtil.SkippAcceptsPatch = true;
            var isValidCheck = !targetSlotGroup.CellsList.Any(c => c.IsValidStorageFor(Map, thing)); 
            PatchStorageUtil.SkippAcceptsPatch = false;
            return isValidCheck;

        }

        private int CountFromMap(ThingDef thingDef)
        {
            return countStacks ? Map.listerThings.ThingsOfDef(thingDef).Count : Map.resourceCounter.GetCount(thingDef);
        }

        private bool CheckSlotGroup(SlotGroup slotGroup, ThingDef thingDef, int limit = int.MaxValue)
        {
            var count = 0;
            foreach (var thing in slotGroup.HeldThings)
            {
                if (thing.def != thingDef) continue;
                if (countStacks)
                {
                    count++;
                }
                else
                {
                    count += thing.stackCount;
                }
                if (count >= limit) return true;
            }
            return false;
        }
    }
}
