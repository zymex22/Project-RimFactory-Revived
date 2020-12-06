using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ModExtension_BonusYield : DefModExtension
    {
        //needs to be string instead of RecipeDef as most RecipeDef(s) are created in C# and that occurs after the XML causing a cant refrence error
        public Dictionary<string, BonusYieldList> billBonusYields;

        //Generic bill independent reward
        public BonusYieldList bonusYields;


        public Thing GetBonusYield(RecipeDef recipe = null, QualityCategory min_quality = QualityCategory.Awful,
            QualityCategory max_quality = QualityCategory.Legendary)
        {
            var genericThing = bonusYields?.GetBonusYield();
            Thing billThing = null;
            Thing thingYield = null;

            if (billBonusYields != null && recipe != null && billBonusYields.ContainsKey(recipe.defName))
                billThing = billBonusYields[recipe.defName]?.GetBonusYield();
            thingYield = billThing ?? genericThing;

            var thingQuality = QualityCategory.Normal;
            if (thingYield.TryGetQuality(out thingQuality))
            {
                //Try Update Quality
                thingQuality =
                    (QualityCategory) Mathf.Clamp((float) thingQuality, (float) min_quality, (float) max_quality);
                thingYield.TryGetComp<CompQuality>()?.SetQuality(thingQuality, ArtGenerationContext.Colony);
            }

            return thingYield;
        }
    }


    public class BonusYieldList
    {
        public float chance;
        public List<BonusYield> yields;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.Attributes["Chance"] != null) float.TryParse(xmlRoot.Attributes["Chance"].Value, out chance);
            yields = xmlRoot.ChildNodes.Cast<XmlNode>().Where(n => n.NodeType == XmlNodeType.Element)
                .Select(n => n as XmlElement)
                .Where(e => e != null)
                .Select(e => DirectXmlToObject.ObjectFromXml<BonusYield>(e, false))
                .ToList();
        }

        public Thing GetBonusYield()
        {
            if (Rand.Chance(chance))
            {
                var yield = yields?
                    .RandomElementByWeightWithFallback(y => y.Weight);
                if (yield != null)
                {
                    var t = ThingMaker.MakeThing(yield.def, yield.MaterialDef);
                    t.stackCount = yield.Count;
                    if (t.TryGetQuality(out _))
                        t.TryGetComp<CompQuality>()
                            ?.SetQuality((QualityCategory) yield.Quality, ArtGenerationContext.Colony);
                    if (t.def.CanHaveFaction) t.SetFaction(Faction.OfPlayer);

                    return t.TryMakeMinified();
                }
            }

            return null;
        }
    }

    public class BonusYield
    {
        public int count;
        public ThingDef def;
        public string materialdef;
        public int quality;
        public float weight;

        public int Count => Mathf.Min(count, def.stackLimit);

        public float Weight => weight;

        public int Quality => quality;

        public ThingDef MaterialDef
        {
            get
            {
                if (materialdef != null)
                    return ThingDef.Named(materialdef);
                return GenStuff.DefaultStuffFor(def);
            }
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "def", xmlRoot.Name);
            if (xmlRoot.Attributes["Weight"] != null) float.TryParse(xmlRoot.Attributes["Weight"].Value, out weight);
            if (xmlRoot.Attributes["Count"] != null) int.TryParse(xmlRoot.Attributes["Count"].Value, out count);
            if (xmlRoot.Attributes["Quality"] != null) int.TryParse(xmlRoot.Attributes["Quality"].Value, out quality);
            if (xmlRoot.Attributes["MaterialDef"] != null) materialdef = xmlRoot.Attributes["MaterialDef"].Value;
        }
    }
}