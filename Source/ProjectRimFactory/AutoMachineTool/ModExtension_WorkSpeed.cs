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
    public class ModExtension_WorkSpeed : DefModExtension
    {
        public int minPower = 0;
        public int maxPower = 1000;
        public float speedFactor = 1;
    }
}
