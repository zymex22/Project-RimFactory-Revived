using RimWorld;
using UnityEngine;

namespace ProjectRimFactory.Storage.UI
{
    public class ITab_IOPortStorage : ITab_Storage
    {
        public Building_StorageUnitIOBase SelBuilding => (Building_StorageUnitIOBase)SelThing;
        public override bool IsVisible => SelBuilding != null && SelBuilding.mode == StorageIOMode.Output;
        public ITab_IOPortStorage()
        {
            size = new Vector2(300f, 480f);
            this.labelKey = "TabStorage";
        }
    }
}
