using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Xml;

namespace ProjectRimFactory.Common
{
    public class ModExtension_Miner : DefModExtension
    {
        public List<ThingDef> excludeOres;

        public bool IsExcluded(ThingDef def)
        {
            return this.excludeOres?.Contains(def) ?? false;
        }
    }
}
