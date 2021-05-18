using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using ProjectRimFactory.Common;
using ProjectRimFactory.Common.HarmonyPatches;

namespace ProjectRimFactory {
    public class PRFGameComponent : GameComponent {
        public List<SpecialSculpture> specialScupltures;  // currently in game
        public List<IAssemblerQueue> AssemblerQueue = new List<IAssemblerQueue>();
        public Dictionary<Industry.LogicSignal,Map> LoigSignalRegestry = new Dictionary< Industry.LogicSignal, Map>();

        public static Pawn PRF_StaticPawn = null;
        public static Job PRF_StaticJob = null;

        private int uniquePRF_ID = 0;

        //Get a unique ID for Saving by Refrence
        public int GetNextPRFID()
        {
            return GetNextID(ref uniquePRF_ID);
        }

        //Copy of RimWorld.UniqueIDsManager.GetNextID(ref int nextID)
        private static int GetNextID(ref int nextID)
        {
            if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Log.Warning("Getting next unique ID during saving or loading. This may cause bugs.");
            }
            int result = nextID;
            nextID++;
            if (nextID == int.MaxValue)
            {
                Log.Warning("Next ID is at max value. Resetting to 0. This may cause bugs.");
                nextID = 0;
            }
            return result;
        }


        public static void GenStaticPawn()
        {
            PRF_StaticPawn = PawnGenerator.GeneratePawn(PRFDefOf.PRFSlavePawn, Faction.OfPlayer);
            PRF_StaticPawn.Name = new NameTriple("...", "PRF_Static", "...");
        }

        public PRFGameComponent(Game game) {
            SpecialSculpture.PreStartGame();
            
        }
        public override void ExposeData() {
            base.ExposeData();
            Scribe_Collections.Look(ref specialScupltures, "specialSculptures");

            Scribe_Deep.Look(ref PRF_StaticPawn, "PRF_StaticPawn");
            Scribe_Deep.Look(ref PRF_StaticJob, "PRF_StaticJob");
            Scribe_Values.Look(ref uniquePRF_ID, "uniquePRF_ID");


            if (Scribe.mode != LoadSaveMode.Saving)
            {
                PRF_StaticPawn = null;
                PRF_StaticJob = null;

            }
          

        }
        public void RegisterAssemblerQueue(IAssemblerQueue queue)
        {
            //enshure that there are no duplicates
            //im shure there is a better way
           // AssemblerQueue.RemoveAll(q => queue.ToString() == q.ToString());

            AssemblerQueue.Add(queue);
#if DEBUG
            Debug.Message(Debug.Flag.AssemblerQueue, "RegisterAssemblerQueue " + queue);
#endif
        }
        public void DeRegisterAssemblerQueue(IAssemblerQueue queue)
        {
            AssemblerQueue.RemoveAll(q => queue.ToString() == q.ToString());
        }



            /// <summary>
            /// Make a sculpture Special!
            /// Use: Current.Game.GetComponent&lt;PRFGameComponent&gt;().TryAddSpecialSculpture(...)
            /// </summary>
            /// <returns><c>true</c>, if the sculpture is now Special.</returns>
            /// <param name="item">Art item to make Special.</param>
            /// <param name="specialSculpture">Specific special sculpture; otherwise random</param>
            public bool TryAddSpecialSculpture(Thing item, SpecialSculpture specialSculpture=null, bool verifyProvidedSculpture = true) {
            if (specialSculpture != null) {
                if (verifyProvidedSculpture && (specialSculpture.limitToDefs?.Contains(item.def) == false)) return false;
            } else {  // find an acceptable special sculpture
            foreach (var ss in ProjectRimFactory_ModComponent.availableSpecialSculptures
                                .Where(s => (s.limitToDefs?.Contains(item.def) != false))
                                .InRandomOrder()) {
                    if (this.specialScupltures == null) { specialSculpture = ss; break; }
                    var inGameWithSameId = specialScupltures.FirstOrDefault(s => s.id == ss.id);
                    if (inGameWithSameId == null) { specialSculpture = ss;  break; }
                    if (inGameWithSameId.currentInstances == null ||
                        inGameWithSameId.currentInstances.Count(a => a!=item) 
                           < inGameWithSameId.maxNumberCopies) {
                        specialSculpture = ss;
                        break;
                    }
                    //TODO: Maybe check defs, too, to make sure we aren't replacing
                    //  a grand sculptur with the image for a large one?
                }
                if (specialSculpture == null) return false; // could not find empty sculpture slot
            }
            specialSculpture.MakeItemSpecial(item);
            // bookkeeping:
            if (this.specialScupltures == null) specialScupltures = new List<SpecialSculpture>();
            var alreadyRecordedSpecialSculpture = this.specialScupltures
                             .FirstOrDefault(s => s.id == specialSculpture.id);
            if (alreadyRecordedSpecialSculpture == null) {
                this.specialScupltures.Add(specialSculpture);
                alreadyRecordedSpecialSculpture = specialSculpture;
            } // alreadyRSS has been added to list one way or another now
            if (alreadyRecordedSpecialSculpture.currentInstances == null)
                alreadyRecordedSpecialSculpture.currentInstances = new List<Thing>();
            if (!alreadyRecordedSpecialSculpture.currentInstances.Contains(item)) {
                alreadyRecordedSpecialSculpture.currentInstances.Add(item);
            }
            // Note: autocompletion was essential for all that.
            return true;
        }
        // For use with ModExtension_ModifyProduct
        public static bool TryMakeProductSpecialScupture(List<Thing> products, ModExtension_ModifyProduct modifyYieldExt,
                                          IBillGiver billGiver, Thing productMaker,
                                          RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
                                          Thing dominantIngredient)
        {
            //TODO: allow extraData to specify which special sculpture?
            foreach (Thing p in products) {
                if (Current.Game.GetComponent<PRFGameComponent>().TryAddSpecialSculpture(p)) {
                    return true;
                }
            }
            return false;
        }


        //Ensurs that UpdateThingDescriptions() is only run once.
        private static bool updatedThingDescriptions = false;

        //Updates the description of Things with ModExtension_ModifyProduct & ModExtension_Miner
        //It is executed here a it needs to run after all [StaticConstructorOnStartup] have been called 
        private void UpdateThingDescriptions()
        {
            updatedThingDescriptions = true;
            List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefs.Where(d => (d.thingClass == typeof(Industry.Building_DeepQuarry) || d.thingClass == typeof(Building_WorkTable) || d.thingClass == typeof(AutoMachineTool.Building_Miner)) && d.HasModExtension<ModExtension_ModifyProduct>()).ToList();
            foreach(ThingDef thing in thingDefs)
            {
                if (thing != null)
                {
                    string HelpText = "\r\n\r\n";
                    if (thing.recipes != null)
                    {
                        HelpText += "PRF_DescriptionUpdate_CanMine".Translate() ;
                        foreach (RecipeDef recipeDef in thing.recipes)
                        {
                            HelpText += String.Format("    - {0} x{1}\r\n", recipeDef.products?[0]?.Label, recipeDef.products?[0]?.count);
                        }
                        HelpText += "\r\n\r\n";
                    }

                    //Get Items that Building_DeepQuarry can Produce
                    if (thing.thingClass == typeof(Industry.Building_DeepQuarry))
                    {
                        List<ThingDef> rocks = Industry.Building_DeepQuarry.PossibleRockDefCandidates.Where(d => !thing.GetModExtension<ModExtension_Miner>()?.IsExcluded(d.building.mineableThing) ?? true).ToList();
                        HelpText += "PRF_DescriptionUpdate_CanMine".Translate();
                        foreach (ThingDef rock in rocks)
                        {
                            HelpText += String.Format("    - {0} x{1}\r\n", rock.LabelCap , rock.building.mineableYield);
                        }
                        HelpText += "\r\n\r\n";
                    }


                        HelpText += thing.GetModExtension<ModExtension_ModifyProduct>()?.GetBonusOverview_Text();
                    thing.description += HelpText;
                }
            }

        }


        public override void LoadedGame()
        {
            base.LoadedGame();
#if DEBUG
            //List all queue's for debug use
            foreach (IAssemblerQueue queue in AssemblerQueue)
            {
                Debug.Message(Debug.Flag.AssemblerQueue, "" + queue + "  - " + AssemblerQueue[AssemblerQueue.IndexOf(queue)].Map);
                Building building = queue as Building;
                Debug.Message(Debug.Flag.AssemblerQueue, "" + building.Map + " " + building.Position + "  --- ");
                foreach (Thing thing in AssemblerQueue[AssemblerQueue.IndexOf(queue)].Map.thingGrid.ThingsAt(building.Position))
                {
                    Debug.Message(Debug.Flag.AssemblerQueue, " " + thing + " is at " + building.Position);
                }
            }
#endif
            if (updatedThingDescriptions == false) UpdateThingDescriptions();

           

        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            if (updatedThingDescriptions == false) UpdateThingDescriptions();
        }

        /*
// A quick way to test all the scupltures available, if need be.       
public override void LoadedGame() {
base.LoadedGame();
foreach (var t in Current.Game.CurrentMap.spawnedThings.Where(x => x is Building_Art)) {
if (!Current.Game.GetComponent<PRFGameComponent>().TryAddSpecialSculpture(t)) return;
Log.Warning("---------added test special scuplture: " + t + " at " + t.Position);
}
}*/
    }
}
