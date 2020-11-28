using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using ProjectRimFactory.Common;
using ProjectRimFactory.Common.HarmonyPatches;
namespace ProjectRimFactory {
    public class PRFGameComponent : GameComponent {
        public List<SpecialSculpture> specialScupltures;

        public static List<IAssemblerQueue> AssemblerQueue = new List<IAssemblerQueue>();

        public static void RegisterAssemblerQueue(IAssemblerQueue queue)
        {
            AssemblerQueue.Add(queue);
        }

        public PRFGameComponent(Game game) {
            SpecialSculpture.PreStartGame();
        }
        public override void ExposeData() {
            base.ExposeData();
            Scribe_Collections.Look(ref specialScupltures, "specialSculptures");
        }
        /// <summary>
        /// Make a sculpture Special!
        /// Use: Current.Game.GetComponent&lt;PRFGrameComponent&gt;().TryAddSpecialSculpture(...)
        /// </summary>
        /// <returns><c>true</c>, if the sculpture is now Special.</returns>
        /// <param name="item">Art item to make Special.</param>
        /// <param name="specialSculpture">Specific special sculpture; otherwise random</param>
        public bool TryAddSpecialSculpture(Thing item, SpecialSculpture specialSculpture=null) {
            if (specialSculpture == null) { // find an acceptable special sculpture
                foreach (var ss in ProjectRimFactory_ModComponent.availableSpecialSculptures
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
