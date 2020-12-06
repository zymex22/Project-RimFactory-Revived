using RimWorld;
using UnityEngine;

namespace ProjectRimFactory.Storage.UI
{
    public class ITab_IOPortStorage : ITab_Storage
    {
        public ITab_IOPortStorage()
        {
            size = new Vector2(300f, 480f);
            labelKey = "TabStorage";
        }

        public Building_StorageUnitIOPort SelBuilding => (Building_StorageUnitIOPort) SelThing;
        public override bool IsVisible => SelBuilding.mode == StorageIOMode.Output;
    }
}