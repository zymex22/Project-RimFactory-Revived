using ProjectRimFactory.Common;
using Verse;

namespace ProjectRimFactory.Storage
{
    /// <summary>
    /// Helper Class for Storage Buildings to handle manually dropping Things
    /// </summary>
    public class StorageOutputUtil(Building building)
    {
        //Select outputCell via CompOutputAdjustable or set it to 2 below current pos
        //Note: While 2 below the current post could be outside them map we probably don't need to handle this as that would be outside the build zone
        
        private readonly CompOutputAdjustable compOutputAdjustable = building.GetComp<CompOutputAdjustable>();
        private IntVec3 OutputCell => compOutputAdjustable?.CurrentCell ?? building.Position + new IntVec3(0, 0, -2);
        private readonly Map map = building.Map;

        /// <summary>
        /// Used to Prevent TryPlaceThing(..., Near) form selecting a cells belonging to ILinkableStorageParent
        /// 
        /// Note: There might be other cases to consider such as belts
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool ValidatePos(IntVec3 position)
        {
            return !position.GetThingList(map).Any(e => e is ILinkableStorageParent);
        }

        public bool OutputItem(Thing item)
        {
            return GenPlace.TryPlaceThing(item.SplitOff(item.stackCount), OutputCell, map, ThingPlaceMode.Near,null, ValidatePos);
        }

    }
}
