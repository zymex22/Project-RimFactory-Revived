using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory
{
    // Note: Other places that touch SpecialSculputre:
    //   ProjectRimFactor_ModComponent has the list of all of them
    //   RS.cs initializes the resources (graphics)
    //   PRFGameComponent has the list of current special sculptures in a game
    //   HarmonyPatches/CompArt_GenerateImageDescription makes the description show up in game
    // General outline in code:
    //   1. RimWorld starts; ModComponenet initializes all available options
    //      graphics inits
    //   2. Game starts/loads
    //      master list cleansed of any extra data
    //      If loading game: GameComponent loads own list
    //                       these update some values from master list
    //                       at end of load, each turns it sculpture into special version
    //   3. Player somehow creates new special sculpture
    //      GameComponent handles adding new sculpture to list; art make special.
    //  Weird implementation notes: master list can be used to keep track of items
    //    in game b/c it was easier than copying it.  But saving and loading the game
    //    makes a copy, so that's separate.
    //    Feel free to make an actual copy, if you want to rewrite GameCompononent's logic.
    //    If you add new logic to SpecialSculptures, in GameComponent, always check against
    //    master list.
    //    To be fair, I wrote this v quickly while grieving, so it's understandable
    //    if there are questionable choices.
    public class SpecialSculpture : IExposable
    {
        public SpecialSculpture()
        {
        }
        public string id;
        public int maxNumberCopies = 1;
        public List<ThingDef> limitToDefs;
        public GraphicData graphicData;
        public string titleKey;
        public string descKey;
        public string authorKey; // Just putting author ok
        public Graphic graphic; // setup in game
        public List<Thing> currentInstances = null; // item in game

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // make sure we should bother saving:
                bool shouldSave = false;
                foreach (var t in currentInstances)
                {
                    if (!t.Destroyed)
                    {
                        shouldSave = true;
                        break;
                    }
                }
                if (!shouldSave) return;
            }
            Scribe_Values.Look(ref id, "id", null, true);
            Scribe_Values.Look(ref titleKey, "title");
            Scribe_Values.Look(ref descKey, "desc");
            Scribe_Values.Look(ref authorKey, "author");
            Scribe_Collections.Look(ref currentInstances, "item", LookMode.Reference);
            // At end of loading, go through update description and graphics of art objects
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                //Log.Message("PRF Loading Sculpture " + titleKey);
                var all = Common.ProjectRimFactory_ModComponent.availableSpecialSculptures;
                var s = all?.FirstOrDefault(x => x.id == id);
                if (s != null)
                {
                    // Update graphic from master XML
                    graphicData = s.graphicData;
                    graphic = s.graphic;
                    maxNumberCopies = s.maxNumberCopies;
                }
                foreach (Thing t in currentInstances)
                    MakeItemSpecial(t);
            }
        }
        // called on system load; prepares graphics (main thread only)
        public void Init()
        {
            if (graphicData != null)
                graphic = graphicData.Graphic;
        }
        // Handle magic of making art item into this special sculpture
        // Note: Description is handled by harmony patch checking against
        //       GameComponent list
        public void MakeItemSpecial(Thing art)
        {
            var artComp = art.TryGetComp<CompArt>();
            if (artComp == null) { Log.Error("PRF could not make special sculpture from " + art); return; }
            // Use HarmonyLib to set internal string values for title and author:
            AccessTools.Field(typeof(CompArt), "titleInt").SetValue(artComp, titleKey.Translate());
            AccessTools.Field(typeof(CompArt), "authorNameInt").SetValue(artComp, authorKey.Translate());
            if (graphicData != null)
                AccessTools.Field(typeof(Thing), "graphicInt").SetValue(artComp.parent, graphicData.Graphic);
        }
        // Pre-load game init:
        static public void PreStartGame()
        {
            var all = Common.ProjectRimFactory_ModComponent.availableSpecialSculptures;
            if (all == null) return;
            foreach (var s in all)
            {
                s.currentInstances = null;
            }
        }
        // Load from XML file Settings/SpecialSculpture.xml; called from ModComponent
        static public List<SpecialSculpture> LoadAvailableSpecialSculptures(ModContentPack content)
        {
            try
            {
                var xml = DirectXmlLoader.XmlAssetsInModFolder(content, "Settings")?.
                                  Where(x => x.name == "SpecialSculpture.xml")?.FirstOrDefault();
                if (xml == null || xml.xmlDoc == null)
                {
                    Log.Warning("PRF could not load special sculpture data");
                    return null;
                }
                List<SpecialSculpture> list = DirectXmlToObject
                     .ObjectFromXml<List<SpecialSculpture>>(xml.xmlDoc.DocumentElement, false);
#if DEBUG
                Log.Message("PRF: loaded " + ((list == null) ? "zero" : (list.Count.ToString())) +
                                " special Sculptures");
#endif
                return list;
            }
            catch (Exception e)
            {
                Log.Error("PRF was unable to extract Special Sculpture data from XML; \n" +
                    "  Exception: " + e);
                return null;
            }
        }
    }
#if DEBUG
    [HarmonyPatch(typeof(Verse.ThingComp), "CompGetGizmosExtra")]
    class Patch_CompGizmosExtraForArt {
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, ThingComp __instance) {
            foreach (var g in __result) yield return g;
            if (!(__instance is CompArt compArt)) yield break;
            if (!(compArt.parent is Building_Art)) yield break;
            yield return new Command_Action()
            {
                defaultLabel = "Make Special Sculpture (SLOW)",
                defaultDesc = "Make this Sculpture the first Special Sculpture available.  Reloads everything.\n\nTHIS IS VERY SLOW!\n(Project RimFactory)",
                action = delegate () {
                    // reload graphic assets: (not fast)
                    LoadedModManager.GetMod<ProjectRimFactory.Common.ProjectRimFactory_ModComponent>().Content.ReloadContent();
                    // reload language data: (....slow)
                    LanguageDatabase.Clear();
                    LanguageDatabase.InitAllMetadata(); // have to reload all? :p
                    //LoadedModManager.GetMod<ProjectRimFactory.Common.ProjectRimFactory_ModComponent>().Content.
                    ProjectRimFactory.Common.ProjectRimFactory_ModComponent.availableSpecialSculptures
                        = SpecialSculpture.LoadAvailableSpecialSculptures(LoadedModManager.GetMod<ProjectRimFactory.Common.ProjectRimFactory_ModComponent>().Content
                        );
                    foreach (var s in ProjectRimFactory.Common.ProjectRimFactory_ModComponent.availableSpecialSculptures)
                        s.Init();
                    var target = ProjectRimFactory.Common.ProjectRimFactory_ModComponent.availableSpecialSculptures[0];

                    var comp = Current.Game.GetComponent<ProjectRimFactory.PRFGameComponent>();
                    if (comp.specialScupltures != null)
                        //                    if (comp.specialScupltures == null) comp.specialScupltures = new List<SpecialSculpture>();

                        comp.specialScupltures.RemoveAll(s => s.id == target.id);
                    comp.TryAddSpecialSculpture(compArt.parent, target);
                    compArt.parent.DirtyMapMesh(compArt.parent.Map); // redraw graphic
                }
            };

        }
    }
#endif
}
