using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class ModExtension_Conveyor : DefModExtension
    {
        public bool underground = false;
        public bool toUnderground = false;
        // // // // // // // Graphics // // // // // // //
        public float carriedItemScale = 0.75f; // 0.0 - 1.0 are expected, but you can try above 1? 0.6 looks good for UG
        // public float carriedItemDrawHeight = 0.15f; // how high above the "True Center" of the belt to draw the item.
        public Vector2 arrowDrawOffset;
        public Vector2 arrowEastDrawOffset;
        public Vector2 arrowWestDrawOffset;
        public Vector2 arrowNorthDrawOffset;
        public Vector2 arrowSouthDrawOffset;
        // These are dependent on the specific graphic class being used
        public string texPath2 = null;  // splitter building, wall edges, whatever
        public List<ThingDef> specialLinkDefs;

    }
}
