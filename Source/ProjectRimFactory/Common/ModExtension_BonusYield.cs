using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HarmonyLib;  // AccessTools for methods 
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common {
    [HarmonyPatch(typeof(Verse.GenRecipe), "MakeRecipeProducts")]
    static class Patch_GenRecipe_MakeRecipeProducts_BonusYield {
        static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
            Thing dominantIngredient, IBillGiver billGiver) {
            List<Thing> products = new List<Thing>(__result); // List->IEnumerable->List, etc. As one does.
            if (billGiver is Thing t && // `is` implies non-null
                t.def.GetModExtension<ModExtension_BonusYield>() is ModExtension_BonusYield yieldExt) {
                yieldExt.GiveAnyBonusProducts(products, recipeDef, worker, ingredients, dominantIngredient, billGiver);
            }
            foreach (var r in products) {
                yield return r;
            }
        }
    }

    public class ModExtension_BonusYield : DefModExtension
    {
        //needs to be string instead of RecipeDef as most RecipeDef(s) are created in C# and that occurs after the XML causing a cant refrence error
        public Dictionary<string , BonusYieldList> billBonusYields;
        // Internal system for yield changers: C# methods that can determine yields with a lot of flexibility
        //   We have to set this aside as a separate object type, becaus
        //   Well, because several reasons, having to do with C# limitations
        //   and RW XML->C# limits.  Making our own object 
        public YieldChangerList yieldChangers;
        // Generic bill independent reward
        //  A different object instead of a List<BonusYield> for similar reason - it's the easiest
        //  way to make the XML->C# straightforward.  Note that in this case, it's harder to expand
        //  in a generic way, so ...good luck whoever maintains after this; I'm not going to fix it
        //  myself? Whoever wrote it did make the XML elegant.
        public BonusYieldList bonusYields;
        public bool doAllBonuses = true;
        public QualityCategory minQuality = QualityCategory.Normal;
        public QualityCategory maxQuality = QualityCategory.Legendary;

        public void GiveAnyBonusProducts(List<Thing> products, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
            Thing dominantIngredient, IBillGiver billGiver) {
            if (this.yieldChangers == null || yieldChangers.changers.NullOrEmpty()) {
                this.yieldChangers = new YieldChangerList();
                yieldChangers.changers.Add(TryGetDefaultBonusYield);
            }
            foreach (var giver in yieldChangers.changers) {
                if (giver(products, this, recipeDef, worker, ingredients, dominantIngredient, billGiver)) {
                    if (!doAllBonuses) return;
                }
            }
        }

        public static bool TryGetDefaultBonusYield(List<Thing> products, ModExtension_BonusYield bonusYieldExt,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient, IBillGiver billGiver
                                     ) {
            return TryGetRecipeBonusYield(products, bonusYieldExt, recipeDef, worker, ingredients,
                dominantIngredient, billGiver) ||
                TryGetSimpleBonusYield(products, bonusYieldExt, recipeDef, worker, ingredients,
                dominantIngredient, billGiver);
        }

        public static bool TryGetSimpleBonusYield(List<Thing> products, ModExtension_BonusYield bonusYieldExt,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient, IBillGiver billGiver
                                     ) {
            // Normal bonus products:
            // Check to see if we get a random bonus item from general list:
            Thing genericThing = bonusYieldExt.bonusYields?.GetBonusYield();
            if (genericThing != null) {
                products.Add(genericThing);
                return true;
            }
            return false;
        }
        public static bool TryGetRecipeBonusYield(List<Thing> products, ModExtension_BonusYield bonusYieldExt,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient, IBillGiver billGiver
                                     ) {
            if (bonusYieldExt.billBonusYields != null && bonusYieldExt.billBonusYields.ContainsKey(recipeDef.defName)) {
                var billThing = bonusYieldExt.billBonusYields[recipeDef.defName]?.GetBonusYield();
                if (billThing != null) {
                    products.Add(billThing);
                    return true;
                }
            }
            return false;
        }

        // If something is Awful, but minimum acceptable quality is Normal, make it Normal
        private void EnsureProperQuality(Thing t) {
            var compQ = t?.TryGetComp<CompQuality>();
            if (compQ != null) {
                // restrict to acceptable qualities:
                var quality = (QualityCategory)Mathf.Clamp((float)compQ.Quality, (float)minQuality, (float)maxQuality);
                compQ.SetQuality(quality, ArtGenerationContext.Colony);
            }
        }

        public Thing GetBonusYield(RecipeDef recipe = null, QualityCategory min_quality = QualityCategory.Awful, 
                                   QualityCategory max_quality = QualityCategory.Legendary)
        {
            Thing genericThing = this.bonusYields?.GetBonusYield();
            Thing billThing = null;
            Thing thingYield = null;

            if (billBonusYields != null && recipe != null && billBonusYields.ContainsKey(recipe.defName))
            {
                
                billThing = this.billBonusYields[recipe.defName]?.GetBonusYield();
            }
            thingYield = billThing ?? genericThing;

            QualityCategory thingQuality = QualityCategory.Normal;
            if (thingYield.TryGetQuality(out thingQuality))
            {
                //Try Update Quality
                thingQuality = (QualityCategory)Mathf.Clamp((float)thingQuality, (float)min_quality, (float)max_quality);
                thingYield.TryGetComp<CompQuality>()?.SetQuality(thingQuality, ArtGenerationContext.Colony);

            }

            return thingYield;
        }


    }

    public class YieldChangerList {
        public static Dictionary<string, YieldChanger> specialNames = new Dictionary<string, YieldChanger>
        {
            { "default", ModExtension_BonusYield.TryGetDefaultBonusYield },
            { "simple", ModExtension_BonusYield.TryGetSimpleBonusYield },
            { "recipe", ModExtension_BonusYield.TryGetRecipeBonusYield },
        };
        // Technical: we have to do this delegate declaration b/c C# doesn't have
        //   any other way to define a set of these methods - no Func<ref Thing, bool> etc.
        //   We translate string->YieldGiver with the LoadDataFromXmlCustom, which overrides
        //   vanilla's XML->C# engine completely.
        //   There is NO WAY RW will handle that XML->C# translation for us ;p
        public delegate bool YieldChanger(List<Thing> products, ModExtension_BonusYield bonusYield,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient, IBillGiver billGiver);
        public List<YieldChanger> changers = new List<YieldChanger>();
        // Override RW XML->C#
        public void LoadDataFromXmlCustom(XmlNode xmlRoot) {
            // Copying Nobo's code below - there may be a better way to do this?
            foreach (var s in xmlRoot.ChildNodes.Cast<XmlNode>()
                    .Where(n => n.NodeType == XmlNodeType.Element)
                    .OfType<XmlElement>()
                    .Where(x => x.Name.ToLower() == "li")
                    .Select(e=>e.InnerText)) {
                try {
                    var checkSpecial = s.ToLower();
                    if (specialNames.ContainsKey(checkSpecial)) {
                        changers.Add(specialNames[checkSpecial]);
                    } else {
                        var method = AccessTools.Method(s);
                        changers.Add((YieldChanger)Delegate.CreateDelegate(typeof(YieldChanger), method));
                    }
                } catch (Exception e) {
                    Log.Error("ModExtension_BonusYield could not parse method \"" + s + "\"\nFull Exception: "+e);
                    continue;
                }
            }
        }
    }



    public class BonusYieldList
    {
        public List<BonusYield> yields;

        public float chance;

        // Completely override default RW read from XML:
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

        // override vanilla load from XML:
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
