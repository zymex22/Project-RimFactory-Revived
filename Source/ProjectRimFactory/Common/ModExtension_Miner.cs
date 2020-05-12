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
        public MiningBonusYieldList bonusYields;

        public bool IsExcluded(ThingDef def)
        {
            return this.excludeOres?.Contains(def) ?? false;
        }
        public Thing GetBonusYield(ThingDef mineThingDef)
        {
            return this.bonusYields?.GetBonusYield(mineThingDef);
        }
    }

    public class MiningBonusYieldList
    {
        public List<MiningBonusYield> yields;

        public float chance;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if(xmlRoot.Attributes["Chance"] != null)
            {
                float.TryParse(xmlRoot.Attributes["Chance"].Value, out this.chance);
            }
            this.yields = xmlRoot.ChildNodes.Cast<XmlNode>().Where(n => n.NodeType == XmlNodeType.Element)
                    .Select(n => n as XmlElement)
                    .Where(e => e != null)
                    .Select(e => DirectXmlToObject.ObjectFromXml<MiningBonusYield>(e, false))
                    .ToList();
        }

        public Thing GetBonusYield(ThingDef miningDef)
        {
            if (Rand.Chance(this.chance))
            {
                var yield = this.yields?
//                    .Where(y => y.def != miningDef)
                    .RandomElementByWeightWithFallback(y => y.Weight, null);
                if(yield != null)
                {
                    var t = ThingMaker.MakeThing(yield.def);
                    t.stackCount = yield.Count;
                    return t;
                }
            }
            return null;
        }
    }

    public class MiningBonusYield
    {
        public ThingDef def;
        public float weight;
        public int count;

        public int Count => Mathf.Min(this.count, this.def.stackLimit);

        public float Weight => this.weight;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "def", xmlRoot.Name, null, null);
            if (xmlRoot.Attributes["Weight"] != null)
            {
                float.TryParse(xmlRoot.Attributes["Weight"].Value, out this.weight);
            }
            if (xmlRoot.Attributes["Count"] != null)
            {
                int.TryParse(xmlRoot.Attributes["Count"].Value, out this.count);
            }
        }
    }
}
