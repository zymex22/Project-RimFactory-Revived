using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HarmonyLib;  // AccessTools for methods 
using RimWorld;
using UnityEngine;
using Verse;
/***************************************************************
 * ModExtension_ModifyProduct
 * A DefModExtension that allows you to add bonuses or otherwise modify bill products/trigger callbacks
 *   Note that while this was originally set up to provide bonus items for a factory mod, the possible
 *   uses are mostly limited by your imagination.
 * Sample XML:
 * ...
 * <modExtensions>
 *   <li Class="[namespace].ModExtension_ModifyProducts">
 *     <doAll>true</doAll>   - whether to process all "bonuses"
 *     <bonuses>
 *       <li> - 1st child "bonus"
 *         <chance>.5</chance> - half the time
 *         <altChanger>MyMod.KillAllHumans</altChanger> - alternate callback; this one
 *                                            may not actually be considered a "bonus"
 *                                            See ProductChangerDel in the C#
 *       </li>
 *       <li><def>Gold</def><count>3</count></li> - automatically always add 3 gold
 *       <li>
 *         <replaceOrigProduct>true</replaceOrigProduct>    - we didn't want that gold anyway
 *         <bonusYields Chance="0.18">                  - an alternate way to specify bonus rewards:
 *           <ChunkSlagSteel Weight="1" Count="1" />    - 18% chance of something replacing the orig
 *           <Gold Weight="0.33" Count="10" />            product, with a weighted random choice of
 *           <Jade Weight="0.33" Count="10" />            slag, gold, jade, silver, or a sculpture
 *           <Silver Weight="0.33" Count="30" />
 *           <SculptureLarge Weight="0.01" Count="1" Quality="6" MaterialDef="Jade" />
 *         </bonusYields>
 *         <billBonusYields> - only works with bonusYields, I'm afraid.  If you get a bonus
 *           <li>              yield (as above), this has a chance to replace it with a 
 *             <key>Make_SculptureSmall</key>    - recipe-specific bonus item
 *             <value Chance="1"><ChunkSlagSteel Weight="1" Count="1" /> - no one likes small scupltures?
 *           </li>
 *         </billBonusYields>
 *       </li>
 *       <li>
 *         <chance>0.25</chance>       - 25% chance of either 1 gold or 1 silver (50% chance of either)
 *         <bonuses>
 *           <li><weight>1</weight><def>Gold</def></li>
 *           <li><weight>1</weight><def>Silver</def></li>
 *         </bonuses>
 *       </li>
 *       <li>
 *         <chance>0.25</chance>       - 25% chance of either 1 gold or 1 silver (50% chance of either)
 *         <bonuses>                   - (note: doAll is false by default)
 *           <li><chance>0.5</chance><def>gold</def></li>
 *           <li><def>Silver</def></li>  (note: chance defaults to 1)
 *         </bonuses>
 *       </li>
 *     </bonuses>
 *   </li>
 *   ...
 * C# Note:
 * If you make products but do not do so using the vanilla recipe bill product creation (GenRecipe's MakeRecipeProducts),
 * you can still use this by calling the DefModExtension directly:
 * this.def.GetModExtension<ModExtension_ModifyProduct>()?.ProcessProducts(tmpList, this as IBillGiver, this);
 *
 * Note also: This can be expanded to add new fields fairly easily.  As it's a DefModExtension and only called 
 * when products are actually produced, impact should be minimal.
 *
 * Happy producing!
 *
 * Authors: Nobo -  original BonusYields
 *          Sn1p3rr3c0n - made it better
 *          LWM -  universal application
 *
 **********/
namespace ProjectRimFactory.Common {
    //NOTE: anything that does not use vanilla's bill recipe products generation must directy call
    //   this.def.GetModExtension<ModExtension_ModifyProduct>()?.ProcessProducts(tmpList, this as IBillGiver, this);
    [HarmonyPatch(typeof(Verse.GenRecipe), "MakeRecipeProducts")]
    static class Patch_GenRecipe_MakeRecipeProducts_BonusYield {
        static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
            Thing dominantIngredient, IBillGiver billGiver) {
            //Log.Warning("Harmony P called");
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
    // Utility class that allows fairly arbitrary access to the bill production product list
    //   As long as a static method meets the requirements to e a ProductChangerDel, it can
    //   be called by the ModExtension_ProductModifier
    // Technical: we have to do this delegate declaration b/c C# doesn't have
    //   any other way to define a set of these methods - no Func<ref Thing, bool> etc.
    public delegate bool ProductChangerDel(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                         IBillGiver billGiver = null, Thing productMaker = null,
                                         RecipeDef recipeDef = null, Pawn worker = null,
                                         List<Thing> ingredients = null, Thing dominantIngredient = null);

    public class ProductChanger
    {
        public ProductChangerDel del;
        #if DEBUG
        public string name;
        #endif
        public static Dictionary<string, ProductChangerDel> specialNames = new Dictionary<string, ProductChangerDel>
        { // case INsensitive:
            { "default", ModExtension_ModifyProduct.TryGetDefaultBonusYield },
            { "simplebonus", ModExtension_ModifyProduct.TryGetSimpleBonusYield },
            { "recipebonus", ModExtension_ModifyProduct.TryGetRecipeBonusYield },
            #if DEBUG
            { "test", ModExtension_ModifyProduct.TestChanger },
            #endif
        };
        //   We translate string->YieldGiver with the LoadDataFromXmlCustom, which overrides
        //   vanilla's XML->C# engine completely.
        //   There is NO WAY RW will handle that XML->C# translation for us ;p
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            var methodName = xmlRoot.InnerText;
            try {
                #if DEBUG
                name = methodName;
                #endif
                var checkSpecial = methodName.ToLower();
                if (specialNames.TryGetValue(checkSpecial, out ProductChangerDel d)) {
                    del = d;
                } else {
                    var method = AccessTools.Method(methodName);
                    del=(ProductChangerDel)Delegate.CreateDelegate(typeof(ProductChangerDel), method);
                }
            }
            catch (Exception e) {
                Log.Error("ModExtension_ModifyProduct could not parse method \"" + methodName + "\"\nFull Exception: " + e);
            }
        }
    }

    // Note: This is a recursive class - you can define children with great flexibility
    //    ("bonuses" below, as the def mod extension was originally designed to allow
    //    bonus items during item production.)
    //    The children can have their own "bonuses" etc.
    //    Using altChanger, you can also produce other effects/things/whatever.
    //    See, e.g., the Special Sculpture system.  You can do other things, too.
    //    Maybe whenever something is butchered, there's  risk of blood demons 
    //    surrounding the pawn, etc.  Go wild.
    public class ModExtension_ModifyProduct : DefModExtension
    {
        //------ general options for this level ------//
        public bool autoModify = true; // Whether Vanilla's Product generation (bills) will automatically add bonuses
                                       // (turn off if calling update internally.)
        public List<ModExtension_ModifyProduct> bonuses; // iterative children
        public bool doAll = false;                  // only do 1st
        public bool replaceOrigProduct = false;
        //-- requirements for this to give bonus/change products --//
        public float? chance; // null for always
        public QualityCategory? requiredQuality;    // ==
        public QualityCategory? minRequiredQuality; // >=

        //---- bonus item: ----//
        public ThingDef def = null;
        public float weight = 0f;
        public int count = 1; // good default!
        public string materialdef = null;
        // Quality of bonus items (if they have a quality)
        public QualityCategory? quality;
        public QualityCategory? minQuality;
        public QualityCategory? maxQuality;
        //---- alternate product processor ----//
        public ProductChanger altChanger;
        //---- other way of defining things ----//
        // (included b/c the XML is so pretty...)
        // Needs to be string instead of RecipeDef as most RecipeDef(s) are created in C# and that occurs after the XML causing a cant refrence error
        public Dictionary<string, BonusYieldList> billBonusYields;
        // Generic bill independent reward
        //  A different object instead of a List<BonusYield> for similar reason - it's the easiest
        //  way to make the XML->C# straightforward.  Note that in this case, it's harder to expand
        //  in a generic way, so ...good luck whoever maintains after this; I'm not going to fix it
        //  myself? Whoever wrote it did make the XML elegant.
        public BonusYieldList bonusYields;

        //---- And if you want anything else? ----//
        public string extraData;
        public string extraData2;

        // The main way to use the mod extension.  This is called via Harmony when a vanilla bill makes product items
        //   If products are made some other way and you want to use this def mod extension, you must call this directly:
        // this.def.GetModExtension<ModExtension_ModifyProduct>()?.ProcessProducts(tmpList, this as IBillGiver, this, etc);
        public bool ProcessProducts(List<Thing> products, IBillGiver billGiver = null, 
               Thing productMaker = null, RecipeDef recipeDef = null, Pawn worker = null,
               List<Thing> ingredients = null, Thing dominantIngredient = null)
        {
            Debug.Warning(Debug.Flag.ExtModifyProduct, "ModExtension_ModifyYield: ProcessProducts called for \"" + (billGiver == null ? "nil" : billGiver.ToString()));
            if (!MeetsRequirements(products)) return false;
            Debug.Message(Debug.Flag.ExtModifyProduct, "Processing...");
            bool gotSomeResult = false;
            bool tmp;
            if (altChanger != null) {
                #if DEBUG
                Debug.Message(Debug.Flag.ExtModifyProduct, "  Calling altChanger " + altChanger.name);
                #endif
                tmp = altChanger.del(products, this, billGiver, productMaker, recipeDef, worker, ingredients, dominantIngredient);
                if (!doAll) return tmp;
                if (tmp) gotSomeResult = true;
            }
            if (this.def != null || this.bonusYields != null || this.billBonusYields != null) {
                Thing t = this.BonusThing();
                Debug.Message(Debug.Flag.ExtModifyProduct, (t == null ? "  Tried to get bonus item. Failed." : ("  Tried to get bonus item. Got: " + t.ToString())));
                if (t!=null) {
                    // There was some debate about whether it's a "bonus" if it
                    //  removes the original product, but the consensus is that
                    //  if the XML specifies to replaceOrigProduct, replace it!
                    if (replaceOrigProduct) products.Clear();
                    products.Add(t);
                    gotSomeResult = true;
                }
                if (!doAll) return t != null;
            }
            if (!bonuses.NullOrEmpty()) {
                Debug.Message(Debug.Flag.ExtModifyProduct, "Trying Children:");
                foreach (var b in bonuses) {
                    bool res = b.ProcessProducts(products, billGiver, productMaker, recipeDef, worker, ingredients, dominantIngredient);
                    if (res) {
                        if (!doAll) return true;
                        gotSomeResult = true;
                    }
                }
                Debug.Message(Debug.Flag.ExtModifyProduct, "Done with Children.");
            }
            return gotSomeResult;
        }

        // A direct attempt to get a single bonus item; calling ProcessProducts is preferred
        public Thing TryGetBonus(RecipeDef recipe=null, QualityCategory? minQuality = null, QualityCategory? maxQuality = null)
        {
            if (this.MeetsRequirements()) {
                if (altChanger != null) {
                    var tmpList = new List<Thing>();
                    altChanger.del(tmpList, this, null, null, recipe);
                    if (tmpList.Count>0) {
                        EnsureProperQuality(tmpList[0], minQuality, maxQuality);
                        return tmpList[0];
                    }
                }
                if (def != null) return BonusThing(recipe, minQuality, maxQuality);
                if (!bonuses.NullOrEmpty()) {
                    if (bonuses[0].weight > 0) { // do by weight, not by order
                        var m = bonuses.RandomElementByWeightWithFallback(y => y.weight, null);
                        return m?.TryGetBonus(recipe, minQuality, maxQuality);
                    }
                    foreach (ModExtension_ModifyProduct m in bonuses) {
                        Thing t = m.TryGetBonus(recipe, minQuality, maxQuality);
                        if (t != null) return t;
                    }
                }
            }
            return null;
        }

        public bool MeetsRequirements(List<Thing> products = null) => MeetsQualityRequirements(products) &&
            (chance == null || Rand.Chance((float)this.chance));
        // Only checks first item in products that has the Quality comp
        //   Must have an item w/ quality to pass this test
        public bool MeetsQualityRequirements(List<Thing> products)
        {
            bool meets = true;
            if (this.requiredQuality != null || this.minRequiredQuality != null) {
                meets = false;
                if (!products.NullOrEmpty()) {
                    foreach (Thing t in products) {
                        if (t.TryGetQuality(out QualityCategory qc)) {
                            if ((requiredQuality == null || qc == requiredQuality) &&
                                (minRequiredQuality == null || ((int)qc >= (int)requiredQuality))) { // ? best way? does that even work?
                                meets = true;
                            }
                            break;
                        }
                    }
                }
            }
            return meets;
        }
        // Defalt Behavior:
        //   Look for Def
        //   Look for BonusYield
        //   If we have one of those, see about replacing it with recipe-specific yield
        public Thing BonusThing(RecipeDef recipe = null, QualityCategory? minQuality = null, QualityCategory? maxQuality = null)
        {
            Thing t = SimpleBonusThing(minQuality, maxQuality);
            if (t != null) {
                Thing u = RecipeBonusThing(recipe, minQuality, maxQuality);
                if (u != null) t = u;
            }
            return t;
        }
        // Mainly for use with BonusYieldLists (<bonusYields> in the XML)
        //   Also used iternally for default bonus creation with fallback
        //   to bonusYields bous creation.
        public Thing SimpleBonusThing(QualityCategory? minQuality = null, QualityCategory? maxQuality = null)
        {
            Thing t = null;
            if (this.def != null) {
                t = ThingMaker.MakeThing(this.def, this.MaterialDef);
                t.stackCount = this.Count;
                if (t.def.CanHaveFaction) {
                    t.SetFaction(Faction.OfPlayer);
                }
                Debug.Message(Debug.Flag.ExtModifyProduct, "    Bonus thing made from def: " + t);
            }
            if (t == null && bonusYields != null) {
                t = bonusYields.GetBonusYield();
                Debug.Message(Debug.Flag.ExtModifyProduct, "    BonusYields returned " + (t == null ? "nothing." : ("bonus item " + t)));
            }
            if (t != null) {
                EnsureProperQuality(t, minQuality, maxQuality);
                return t.TryMakeMinified();
            }
            return null;
        }
        // Purely for recipe-specific bonus yields (billBonusYields in XML)
        public Thing RecipeBonusThing(RecipeDef recipe = null, QualityCategory? minQuality = null, QualityCategory? maxQuality = null)
        {
            if (recipe != null && billBonusYields != null &&
                billBonusYields.TryGetValue(recipe.defName, out BonusYieldList b)) {
                Thing t = b.GetBonusYield();
                if (t != null) {
                    Debug.Message(Debug.Flag.ExtModifyProduct, "    Created BillBonusYield for " + recipe.defName + ": " + t);
                    EnsureProperQuality(t);
                    return t.TryMakeMinified();
                }
            }
            return null;
        }
        public int Count => Mathf.Min(this.count, this.def.stackLimit);

        public ThingDef MaterialDef {
            get {
                if (materialdef != null) {
                    return ThingDef.Named(materialdef);
                } else {
                    return GenStuff.DefaultStuffFor(def);
                }
            }
        }
        // If something is Awful, but minimum acceptable quality is Normal, make it Normal
        private void EnsureProperQuality(Thing t, QualityCategory? min = null, QualityCategory? max = null)
        {
            var compQ = t?.TryGetComp<CompQuality>();
            if (compQ != null) {
                if (min == null) min = this.minQuality;
                if (max == null) max = this.maxQuality;
                var q = this.quality ?? compQ.Quality;
                // restrict to acceptable qualities:
                if (min != null) {
                    q = (QualityCategory)Math.Max((int)min, (int)q);
                }
                if (max != null) {
                    q = (QualityCategory)Math.Min((int)max, (int)q);
                }
                compQ.SetQuality(q, ArtGenerationContext.Colony);
            }
        }

        /********************* Static methods for default generic bonuses *************************/
public static bool TryGetDefaultBonusYield(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                          IBillGiver billGiver, Thing productMaker,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient
                                     )
        {
            Thing t = modifyYieldExt.TryGetBonus(recipeDef);
            if (t != null) {
                if (modifyYieldExt.replaceOrigProduct) {
                    products.Clear();
                }
                products.Add(t);
                return true;
            }
            return false;
        }

        // You probably want to use this only when using BonusYieldList (bonusYields in XML)
        public static bool TryGetSimpleBonusYield(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                          IBillGiver billGiver, Thing productMaker,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient
                                     )
        {
            Thing t = modifyYieldExt.SimpleBonusThing();
            if (t != null) {
                if (modifyYieldExt.replaceOrigProduct) {
                    products.Clear();
                }
                products.Add(t);
                return true;
            }
            return false;
        }
        // You definitely want to use this only when usin billBonusYieldList
        public static bool TryGetRecipeBonusYield(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                          IBillGiver billGiver, Thing productMaker,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient
                                     )
        {
            Thing t = modifyYieldExt.RecipeBonusThing(recipeDef);
            if (t != null) {
                if (modifyYieldExt.replaceOrigProduct) {
                    products.Clear();
                }
                products.Add(t);
                return true;
            }
            return false;
        }
#if DEBUG
        // Simply displays a message in the log.  Useful for testing?
        public static bool TestChanger(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                  IBillGiver billGiver, Thing productMaker,
                                  RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                  Thing dominantIngredient)
        {
            products.Add(ThingMaker.MakeThing(ThingDefOf.MealNutrientPaste));
            return true;
        }
#endif
    }

    // Another approach that makes pretty XML, so I kept it around (altho it's much more limited)
    public class BonusYieldList
    {
        public List<BonusYield> bonuses;
        public float chance;

        // Completely override default RW read from XML:
        //   We do this to allow pretty XML
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
        //   Read in things of the form
        //      <Gold Weight="0.02" Count="10" />
        //      <SculptureLarge Weight="0.5" Count="1" Quality="6" MaterialDef="Gold" />
        // etc.
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
