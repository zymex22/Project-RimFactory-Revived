using ProjectRimFactory.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Verse;

namespace ProjectRimFactory.Storage
{
    /// <summary>
    /// Helper Class for Storage Buildings to handle manually dropping Things
    /// </summary>
    public class StorageOutputUtil
    {

        public StorageOutputUtil(Building building)
        {
            cells = building.OccupiedRect().Cells.ToList();
            map = building.Map;

            //Select outputCell via CompOutputAdjustable or set it to 2 below current pos
            //Note: While 2 below the current post could be outside them map we probably don't need to handle this as that would be outside the build zone
            outputCell = building.GetComp<CompOutputAdjustable>()?.CurrentCell ?? building.Position + new IntVec3(0, 0, -2);
        }

        private List<IntVec3> cells = null;
        private IntVec3 outputCell = IntVec3.Invalid;
        private Map map;

        /// <summary>
        /// Used to Prevent TryPlaceThing(..., Near) form selecting a cells belonging to the storage itself
        /// 
        /// Note: It would be still possible for a item to be place inside another storage
        /// Not sure if we should check for that as well
        /// </summary>
        /// <param name="intVec3"></param>
        /// <returns></returns>
        private bool ValidatePos(IntVec3 intVec3 )
        {
            return !cells?.Contains(intVec3) ?? true;
        }

        public bool OutputItem(Thing item)
        {
            return GenPlace.TryPlaceThing(item.SplitOff(item.stackCount), outputCell, map, ThingPlaceMode.Near,null, ValidatePos);
        }

    }
}
