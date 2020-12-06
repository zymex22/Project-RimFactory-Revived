using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class ModExtension_Conveyor : DefModExtension
    {
        // public float carriedItemDrawHeight = 0.15f; // how high above the "True Center" of the belt to draw the item.
        public Vector2 arrowDrawOffset;
        public Vector2 arrowEastDrawOffset;
        public Vector2 arrowNorthDrawOffset;
        public Vector2 arrowSouthDrawOffset;

        public Vector2 arrowWestDrawOffset;

        // // // // // // // Graphics // // // // // // //
        public float carriedItemScale = 0.75f; // 0.0 - 1.0 are expected, but you can try above 1? 0.6 looks good for UG

        public List<ThingDef> specialLinkDefs;

        // These are dependent on the specific graphic class being used
        public string texPath2 = null; // splitter building, wall edges, whatever
        public bool toUnderground = false;
        public bool underground = false;
    }
}