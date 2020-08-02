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
        public int SquareJobRadius = 0; //0 Means infinite
        public string Sleeptimes = ""; //Comma seperated List of sleep Times
    }
}
