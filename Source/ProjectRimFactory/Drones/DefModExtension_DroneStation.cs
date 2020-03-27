using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Drones
{
    public class DefModExtension_DroneStation : DefModExtension
    {
        public int maxNumDrones;
        public bool displayDormantDrones;
        public List<WorkTypeDef> workTypes;
    }
}
