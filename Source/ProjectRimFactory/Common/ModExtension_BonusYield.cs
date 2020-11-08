using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse.Noise;

namespace ProjectRimFactory.Common
{
    public class ModExtension_BonusYield : DefModExtension
    {
        public BonusYieldList bonusYields;

        public Thing GetBonusYield(QualityCategory min_quality = QualityCategory.Awful , QualityCategory max_quality = QualityCategory.Legendary)
        {
            Thing thing = this.bonusYields?.GetBonusYield();
            QualityCategory thingQuality = QualityCategory.Normal;
            if (thing.TryGetQuality(out thingQuality))
            {
                //Try Update Quality
                thingQuality = (QualityCategory)Mathf.Clamp((float)thingQuality, (float)min_quality, (float)max_quality);
                thing.TryGetComp<CompQuality>()?.SetQuality(thingQuality, ArtGenerationContext.Colony);

            }

            return thing;
        }


    }

    public class BonusYieldList
    {
        public List<BonusYield> yields;

        public float chance;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.Attributes["Chance"] != null)
            {
                float.TryParse(xmlRoot.Attributes["Chance"].Value, out this.chance);
            }
            this.yields = xmlRoot.ChildNodes.Cast<XmlNode>().Where(n => n.NodeType == XmlNodeType.Element)
                    .Select(n => n as XmlElement)
                    .Where(e => e != null)
                    .Select(e => DirectXmlToObject.ObjectFromXml<BonusYield>(e, false))
                    .ToList();
        }

        public Thing GetBonusYield()
        {
            if (Rand.Chance(this.chance))
            {
                var yield = this.yields?
                    .RandomElementByWeightWithFallback(y => y.Weight, null);
                if (yield != null)
                {
                    var t = ThingMaker.MakeThing(yield.def, yield.MaterialDef);
                    t.stackCount = yield.Count;
                    if (t.TryGetQuality(out _))
                    {
                        t.TryGetComp<CompQuality>()?.SetQuality((QualityCategory)yield.Quality, ArtGenerationContext.Colony);
                    }
                    if (t.def.CanHaveFaction)
                    {
                        t.SetFaction(Faction.OfPlayer);
                    }

                    return t.TryMakeMinified();
                }
            }
            return null;
        }
    }

    public class BonusYield
    {
        public ThingDef def;
        public float weight;
        public int count;
        public int quality;
        public string materialdef = null;

        public int Count => Mathf.Min(this.count, this.def.stackLimit);

        public float Weight => this.weight;

        public int Quality => this.quality;

        public ThingDef MaterialDef 
        {
            get 
            {
                if (materialdef != null)
                {
                    return ThingDef.Named(materialdef);
                }
                else
                {
                    return GenStuff.DefaultStuffFor(def);
                }
            }
        }

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
            if (xmlRoot.Attributes["Quality"] != null)
            {
                int.TryParse(xmlRoot.Attributes["Quality"].Value, out this.quality);
            }
            if (xmlRoot.Attributes["MaterialDef"] != null)
            {
                this.materialdef = xmlRoot.Attributes["MaterialDef"].Value;
            }
        }
    }



}
