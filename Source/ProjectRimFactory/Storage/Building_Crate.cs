using Verse;

namespace ProjectRimFactory.Storage
{
    public class Building_Crate : Building_MassStorageUnit
    {
        protected override bool CanStoreMoreItems => Position.GetThingList(Map).Count(t => t.def.category == ThingCategory.Item) < MaxNumberItemsInternal;

        public override string GetITabString(int itemsSelected)
        {
            return "PRFItemsTabLabel_Crate".Translate(StoredItemsCount, ModExtensionCrate.limit, itemsSelected);
        }

        protected override string GetUIThingLabel()
        {
            return "PRFCrateUIThingLabel".Translate(StoredItemsCount, ModExtensionCrate.limit);
        }
    }
}
