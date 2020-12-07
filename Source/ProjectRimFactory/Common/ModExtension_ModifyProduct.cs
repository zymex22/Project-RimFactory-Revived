using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HarmonyLib;  // AccessTools for methods 
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common {
    //TODO: this is needed for VE's mech mod and our GenProducts2
    [HarmonyPatch(typeof(Verse.GenRecipe), "MakeRecipeProducts")]
    static class Patch_GenRecipe_MakeRecipeProducts_BonusYield {
        static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
            Thing dominantIngredient, IBillGiver billGiver) {
            Log.Warning("Harmony P called");
            List<Thing> products = new List<Thing>(__result); // List->IEnumerable->List, etc. As one does.
            if (billGiver is Thing t && // `is` implies non-null
                t.def.GetModExtension<ModExtension_ModifyProduct>() is ModExtension_ModifyProduct productExt
                && productExt.autoModify) {
                productExt.ProcessProducts(products, billGiver, t, recipeDef, worker, ingredients, dominantIngredient);
            }
            foreach (var r in products) {
                yield return r;
            }
        }
    }

    public class ModExtension_ModifyProduct : DefModExtension
    {
        public bool autoModify = true; // Whether Vanilla's Product generation (bills) will automatically add bonuses
                                       // (turn off if calling update internally.)
        //needs to be string instead of RecipeDef as most RecipeDef(s) are created in C# and that occurs after the XML causing a cant refrence error
        public Dictionary<string , BonusYieldList> billBonusYields;
        // Internal system for yield changers: C# methods that can determine yields with a lot of flexibility
        //   We have to set this aside as a separate object type, becaus
        //   Well, because several reasons, having to do with C# limitations
        //   and RW XML->C# limits.  Making our own object 
        public ProductChangerList productChangers;
        // Generic bill independent reward
        //  A different object instead of a List<BonusYield> for similar reason - it's the easiest
        //  way to make the XML->C# straightforward.  Note that in this case, it's harder to expand
        //  in a generic way, so ...good luck whoever maintains after this; I'm not going to fix it
        //  myself? Whoever wrote it did make the XML elegant.
        public BonusYieldList bonusYields;
        public bool doAll = true;
        public bool replaceOrigProduct = false;
        public QualityCategory minQuality = QualityCategory.Normal;
        public QualityCategory maxQuality = QualityCategory.Legendary;

        public void ProcessProducts(List<Thing> products, IBillGiver billGiver = null, Thing productMaker = null, RecipeDef recipeDef=null, Pawn worker=null, 
            List<Thing> ingredients=null, Thing dominantIngredient=null) {
            Log.Message("GABP called for " + (billGiver==null?"nll":billGiver.ToString()));
            if (this.productChangers == null || productChangers.changers.NullOrEmpty()) {
                this.productChangers = new ProductChangerList();
                productChangers.changers.Add(TryGetDefaultBonusYield);
            }
            foreach (var giver in productChangers.changers) {
                if (giver(products, this, billGiver, productMaker, recipeDef, worker, ingredients, dominantIngredient)) {
                    if (!doAll) return;
                }
            }
        }

        public static bool TryGetDefaultBonusYield(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                          IBillGiver billGiver, Thing productMaker,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient
                                     ) {
            Thing t = modifyYieldExt.GetBonusYield(recipeDef);
            if (t != null) {
                if (modifyYieldExt.replaceOrigProduct) {
                    products.Clear();
                }
                products.Add(t);
                return true;
            }
            return false;
        }

        public static bool TryGetSimpleBonusYield(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                          IBillGiver billGiver, Thing productMaker,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient
                                     ) {
            Thing t = modifyYieldExt.GetSimpleBonusYield();
            if (t != null) {
                if (modifyYieldExt.replaceOrigProduct) {
                    products.Clear();
                }
                products.Add(t);
                return true;
            }
            return false;
        }
        public static bool TryGetRecipeBonusYield(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                          IBillGiver billGiver, Thing productMaker,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient
                                     ) {
            Thing t = modifyYieldExt.GetRecipeBonusYield(recipeDef);
            if (t!=null) {
                if (modifyYieldExt.replaceOrigProduct) {
                    products.Clear();
                }
                products.Add(t);
                return true;
            }
            return false;
        }

        // If something is Awful, but minimum acceptable quality is Normal, make it Normal
        private void EnsureProperQuality(Thing t, QualityCategory? min=null, QualityCategory? max=null) {
            var compQ = t?.TryGetComp<CompQuality>();
            if (compQ != null) {
                if (min == null) min = this.minQuality;
                if (max == null) max = this.maxQuality;
                // restrict to acceptable qualities:
                var quality = (QualityCategory)Mathf.Clamp((float)compQ.Quality, (float)min, (float)max);
                compQ.SetQuality(quality, ArtGenerationContext.Colony);
            }
        }
        /*
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
        */
        /// <summary>
        /// Try to return a bonus yield if the player got lucky.  Recipe bonus yields will replace
        ///   regular bonus yields and only appear if regular bonus yield procs.
        /// </summary>
        /// <returns>The bonus yield or null.</returns>
        public Thing GetBonusYield(RecipeDef recipe=null, QualityCategory? overrideMinQ = null, QualityCategory? overrideMaxQ = null) {
            Thing t = GetSimpleBonusYield(overrideMinQ, overrideMaxQ);
            if (t != null) return (GetRecipeBonusYield(recipe, overrideMinQ, overrideMaxQ) ?? t);
            return null;
        }
        public Thing GetRecipeBonusYield(RecipeDef recipe=null, QualityCategory? minQ = null, QualityCategory? maxQ = null) {
            if (recipe != null && this.billBonusYields!=null && 
                billBonusYields.TryGetValue(recipe.defName, out BonusYieldList bonusYieldList)) {
                var thing = bonusYieldList.GetBonusYield();
                if (thing == null) return null;
                EnsureProperQuality(thing, minQ, maxQ);
                return thing;
            }
            return null;
        }
        public Thing GetSimpleBonusYield(QualityCategory? minQ = null, QualityCategory? maxQ = null) {
            Thing t = this.bonusYields?.GetBonusYield();
            if (t != null) EnsureProperQuality(t, minQ, maxQ);
            return t;
        }

        #if DEBUG
        public bool TestChanger(List<Thing> products, ModExtension_ModifyProduct bonusYieldExt,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient, IBillGiver billGiver
                                     ) {
            Log.Message("Test Product Yield changer called for " + (billGiver == null ? "nll" : billGiver.ToString()));
            products.Add(ThingMaker.MakeThing(ThingDefOf.MealNutrientPaste));
            return true;
        }
        #endif
    }

    public class ProductChangerList {
        public static Dictionary<string, ProductChanger> specialNames = new Dictionary<string, ProductChanger>
        { // case INsensitive:
            { "default", ModExtension_ModifyProduct.TryGetDefaultBonusYield },
            { "simplebonus", ModExtension_ModifyProduct.TryGetSimpleBonusYield },
            { "recipebonus", ModExtension_ModifyProduct.TryGetRecipeBonusYield },
        };
        // Technical: we have to do this delegate declaration b/c C# doesn't have
        //   any other way to define a set of these methods - no Func<ref Thing, bool> etc.
        //   We translate string->YieldGiver with the LoadDataFromXmlCustom, which overrides
        //   vanilla's XML->C# engine completely.
        //   There is NO WAY RW will handle that XML->C# translation for us ;p
        public delegate bool ProductChanger(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                          IBillGiver billGiver = null, Thing productMaker = null,
                                          RecipeDef recipeDef = null, Pawn worker = null,
                                          List<Thing> ingredients = null, Thing dominantIngredient = null);
        public List<ProductChanger> changers = new List<ProductChanger>();
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
                        changers.Add((ProductChanger)Delegate.CreateDelegate(typeof(ProductChanger), method));
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
        public List<BonusYield> bonuses;

        public float chance;

        // Completely override default RW read from XML:
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.Attributes["Chance"] != null)
            {
                float.TryParse(xmlRoot.Attributes["Chance"].Value, out this.chance);
            }
            this.bonuses = xmlRoot.ChildNodes.Cast<XmlNode>().Where(n => n.NodeType == XmlNodeType.Element)
                    .Select(n => n as XmlElement)
                    .Where(e => e != null)
                    .Select(e => DirectXmlToObject.ObjectFromXml<BonusYield>(e, false))
                    .ToList();
        }

        public Thing GetBonusYield()
        {
            if (Rand.Chance(this.chance))
            {
                var yield = this.bonuses?
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
