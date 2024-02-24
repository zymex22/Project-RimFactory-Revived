using ProjectRimFactory.Common;
using ProjectRimFactory.Common.HarmonyPatches;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProjectRimFactory
{
    public class PRFGameComponent : GameComponent
    {
        public List<SpecialSculpture> specialScupltures;  // currently in game
        public List<IAssemblerQueue> AssemblerQueue = new List<IAssemblerQueue>();

        public static Pawn PRF_StaticPawn = null;
        public static Job PRF_StaticJob = null;

        public static void GenStaticPawn()
        {
            PRF_StaticPawn = PawnGenerator.GeneratePawn(PRFDefOf.PRFSlavePawn, Faction.OfPlayer);
            PRF_StaticPawn.Name = new NameTriple("...", "PRF_Static", "...");
        }

        public PRFGameComponent(Game game)
        {
            SpecialSculpture.PreStartGame();

            //Back Compatibility Setup

            List<BackCompatibilityConverter> data = (List<BackCompatibilityConverter>)SAL3.ReflectionUtility.BackCompatibility_conversionChain.GetValue(null);
            data.Add(new Common.BackCompatibility.PRF_BackCompatibilityConverter());
            SAL3.ReflectionUtility.BackCompatibility_conversionChain.SetValue(null, data);

            PRFDefOf.PRFDrone.race.mechEnabledWorkTypes.AddRange(DefDatabase<WorkTypeDef>.AllDefs);

        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref specialScupltures, "specialSculptures");

            Scribe_Deep.Look(ref PRF_StaticPawn, "PRF_StaticPawn");
            Scribe_Deep.Look(ref PRF_StaticJob, "PRF_StaticJob");




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
        public bool TryAddSpecialSculpture(Thing item, SpecialSculpture specialSculpture = null, bool verifyProvidedSculpture = true)
        {
            if (specialSculpture != null)
            {
                if (verifyProvidedSculpture && (specialSculpture.limitToDefs?.Contains(item.def) == false)) return false;
            }
            else
            {  // find an acceptable special sculpture
                foreach (var ss in ProjectRimFactory_ModComponent.availableSpecialSculptures
                                    .Where(s => (s.limitToDefs?.Contains(item.def) != false))
                                    .InRandomOrder())
                {
                    if (this.specialScupltures == null) { specialSculpture = ss; break; }
                    var inGameWithSameId = specialScupltures.FirstOrDefault(s => s.id == ss.id);
                    if (inGameWithSameId == null) { specialSculpture = ss; break; }
                    if (inGameWithSameId.currentInstances == null ||
                        inGameWithSameId.currentInstances.Count(a => a != item)
                           < inGameWithSameId.maxNumberCopies)
                    {
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
            if (alreadyRecordedSpecialSculpture == null)
            {
                this.specialScupltures.Add(specialSculpture);
                alreadyRecordedSpecialSculpture = specialSculpture;
            } // alreadyRSS has been added to list one way or another now
            if (alreadyRecordedSpecialSculpture.currentInstances == null)
                alreadyRecordedSpecialSculpture.currentInstances = new List<Thing>();
            if (!alreadyRecordedSpecialSculpture.currentInstances.Contains(item))
            {
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
            foreach (Thing p in products)
            {
                if (Current.Game.GetComponent<PRFGameComponent>().TryAddSpecialSculpture(p))
                {
                    return true;
                }
            }
            return false;
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
            UpdateThingDescriptions.Update();
            if (ProjectRimFactory_ModSettings.PRF_LiteMode) PRF_CustomizeDefs.ToggleLiteMode();
            TraderMinifyCheck();

        }



        public override void StartedNewGame()
        {
            base.StartedNewGame();
            UpdateThingDescriptions.Update();
            if (ProjectRimFactory_ModSettings.PRF_LiteMode) PRF_CustomizeDefs.ToggleLiteMode();
            TraderMinifyCheck();
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


        /// <summary>
        /// Removes ThingDef's that can't be minified from the PRF_Factory_Supplier
        /// </summary>
        private void TraderMinifyCheck()
        {
            var stockGenerators = PRFDefOf.PRF_Factory_Supplier.stockGenerators;

            for (int i = stockGenerators.Count - 1; i >= 0; i--)
            {
                if (stockGenerators[i] is StockGenerator_SingleDef stockDev)
                {
                    ThingDef thingDef = (ThingDef)SAL3.ReflectionUtility.StockGenerator_SingleDef_thingDef.GetValue(stockDev);
                    if (!thingDef.mineable) stockGenerators.RemoveAt(i);
                }
            }
        }



    }





}
