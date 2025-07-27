using RimWorld;
using UnityEngine;

namespace ProjectRimFactory.Storage.UI
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public class ITab_IOPortStorage : ITab_Storage
    {
        private Building_StorageUnitIOBase SelBuilding => (Building_StorageUnitIOBase)SelThing;
        public override bool IsVisible => SelBuilding is { Mode: StorageIOMode.Output };
        public ITab_IOPortStorage()
        {
            size = new Vector2(300f, 480f);
            labelKey = "TabStorage";
        }
    }
}
