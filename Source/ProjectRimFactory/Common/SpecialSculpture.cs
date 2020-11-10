using System;
using System.Collections.Generic;
using Verse;
using System.Linq;
using RimWorld;
using HarmonyLib;
namespace ProjectRimFactory {
    // Note: Other places that touch SpecialSculputre:
    //   ProjectRimFactor_ModComponent has the list of all of them
    //   RS.cs initializes the resources (grahpics)
    //   PRFGameComponent has the list of current special sculptures in a game
    //   HarmonyPatches/CompArt_GenerateImageDescription makes the description show up in game
    public class SpecialSculpture : IExposable {
        public SpecialSculpture() {
        }
        public string id;
        public GraphicData graphicData;
        public string titleKey;
        public string descKey;
        public string authorKey; // Just putting author ok
        public Thing currentInstance = null; // item in game
        public Graphic graphic; // setup in game
        public void ExposeData() {
            Scribe_Values.Look(ref id, "id", null, true);
            //Scribe_Values.Look(ref graphicData, "graphicData");
            Scribe_Values.Look(ref titleKey, "title");
            Scribe_Values.Look(ref descKey, "desc");
            Scribe_Values.Look(ref authorKey, "author");
            Scribe_References.Look(ref currentInstance, "item");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                //Log.Message("PRF Loading Sculpture " + titleKey);
                //var all = Current.Game.GetComponent<ProjectRimFactory.PRFGameComponent>().specialScupltures;
                var all = Common.ProjectRimFactory_ModComponent.availableSpecialSculptures;
                if (all != null) {
//                    Log.Message("looking through game component...
                    var s = all.FirstOrDefault(x => x.id == this.id);
                    if (s!=null) {
                        this.graphicData = s.graphicData;
                        this.graphic = s.graphic;
                    }
                }
                MakeItemSpecial(currentInstance);
            }
        }
        public void Init() {
            if (graphicData != null)
                this.graphic = graphicData.Graphic;
        }
        public void MakeItemSpecial(Thing art) {
            var artComp = art.TryGetComp<CompArt>();
            if (artComp == null) { Log.Error("PRF could not make special sculpture from " + art); return; }
            // Use HarmonyLib to set internal string values for title and author:
            AccessTools.Field(typeof(CompArt), "titleInt").SetValue(artComp, titleKey.Translate());
            AccessTools.Field(typeof(CompArt), "authorNameInt").SetValue(artComp, authorKey.Translate());
            if (this.graphicData != null)
                AccessTools.Field(typeof(Thing), "graphicInt").SetValue(artComp.parent, graphicData.Graphic);
        }
        // Load from XML file Settings/SpecialSculpture.xml; called from ModComponent
        static public List<SpecialSculpture> LoadAvailableSpecialSculptures(ModContentPack content) {
            try {
                var xml = DirectXmlLoader.XmlAssetsInModFolder(content, "Settings")?.
                                  Where(x => x.name == "SpecialSculpture.xml")?.FirstOrDefault();
                if (xml == null || xml.xmlDoc == null) {
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
            } catch (Exception e) {
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
