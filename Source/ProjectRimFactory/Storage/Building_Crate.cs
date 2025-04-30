using ProjectRimFactory.Storage.Editables;
using Verse;

namespace ProjectRimFactory.Storage
{
    public class Building_Crate : Building_MassStorageUnit
    {
        public override bool CanStoreMoreItems => Position.GetThingList(Map).Count(t => t.def.category == ThingCategory.Item) < MaxNumberItemsInternal;
        public DefModExtension_Crate Extension => def.GetModExtension<DefModExtension_Crate>();
        public override string GetITabString(int itemsSelected)
        {
            return "PRFItemsTabLabel_Crate".Translate(StoredItemsCount, Extension.limit, itemsSelected);
        }
        public override string GetUIThingLabel()
        {
            return "PRFCrateUIThingLabel".Translate(StoredItemsCount, Extension.limit);
        }
    }
}
