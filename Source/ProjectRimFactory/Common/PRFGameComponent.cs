using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using ProjectRimFactory.Common;
namespace ProjectRimFactory {
    public class PRFGameComponent : GameComponent {
        public List<SpecialSculpture> specialScupltures;
        public PRFGameComponent(Game game) {
            Log.Message("Loaded PRF Game Component");
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
                    if (this.specialScupltures == null ||
                        // not currently in special Sculptures list
                        (specialScupltures.FirstOrDefault(s => s.id == ss.id) == null)) {
                        //TODO: Maybe check defs, too, to make sure we aren't replacing
                        //  a grand sculptur with the image for a large one?
                        specialSculpture = ss;
                        break;
                    }
                }
                if (specialSculpture == null) return false; // could not find empty sculpture
            }
            specialSculpture.currentInstance = item;
            specialSculpture.MakeItemSpecial(item);
            if (this.specialScupltures == null) specialScupltures = new List<SpecialSculpture>();
            this.specialScupltures.Add(specialSculpture);
            return true;
        }
        /*
        public override void LoadedGame() {
            base.LoadedGame();
            foreach (var t in Current.Game.CurrentMap.spawnedThings.Where(x => x is Building_Art)) {
                if (!Current.Game.GetComponent<PRFGameComponent>().TryAddSpecialSculpture(t)) return;
                Log.Warning("---------added test special scuplture: " + t + " at " + t.Position);
            }
        }*/
    }
}
