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
        public List<MiningBonusYield> bonusYields;
        public bool multiBonus = false;

        public bool IsExcluded(ThingDef def)
        {
            return this.excludeOres?.Contains(def) ?? false;
        }

        public IEnumerable<Thing> GetBonusYields(ThingDef mineThingDef)
        {
            return this.bonusYields
                ?.OrderBy(a => Guid.NewGuid())
                .Where(b => b.def != mineThingDef)
                .Where(b => Rand.Chance(b.Chance))
                .Where((b, i) => this.multiBonus || i == 0)
                .Select(this.MakeThing)
                ?? Enumerable.Empty<Thing>();
        }

        private Thing MakeThing(MiningBonusYield bonus)
        {
            var t = ThingMaker.MakeThing(bonus.def);
            t.stackCount = bonus.Count;
            return t;
        }
    }

    public class MiningBonusYield
    {
        public ThingDef def;
        public float chance;
        public int count;

        public int Count => Mathf.Min(this.count, this.def.stackLimit);

        public float Chance => this.chance;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "def", xmlRoot.Name, null, null);
            if (xmlRoot.Attributes["Chance"] != null)
            {
                float.TryParse(xmlRoot.Attributes["Chance"].Value, out this.chance);
            }
            if (xmlRoot.Attributes["Count"] != null)
            {
                int.TryParse(xmlRoot.Attributes["Count"].Value, out this.count);
            }
        }
    }
}
