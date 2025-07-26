using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Storage
{
    /// <summary>
    /// Interface for buildings that can have IO Ports link to them
    /// </summary>
    public interface ILinkableStorageParent
    {
        public List<Thing> StoredItems { get; }

        public bool AdvancedIOAllowed { get; }

        public void HandleNewItem(Thing item);

        public void HandleMoveItem(Thing item);

        public bool CanReceiveThing(Thing item);

        public bool HoldsPos(IntVec3 pos);

        //What is that even for ??
        public void DeregisterPort(Building_StorageUnitIOBase port);
        public void RegisterPort(Building_StorageUnitIOBase port);

        public StorageSettings GetSettings { get; }

        public IntVec3 GetPosition { get; }
        
        public bool CanReceiveIO { get; }
        public Map Map { get; }

        public int StoredItemsCount { get; }

        public string GetITabString(int itemsSelected);

        public LocalTargetInfo GetTargetInfo { get; }

        public bool OutputItem(Thing item);

        public bool Powered { get; }

        public bool CanUseIOPort { get; }

    }
}