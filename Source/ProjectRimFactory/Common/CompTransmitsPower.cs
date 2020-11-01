using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRimFactory {
    public class CompTransmitsPower : ThingComp {
        //TODO: Sometime in 2021, this can be removed!
        static int version=2;
        bool hasConduitInt = true;
        public CompTransmitsPower() {
        }
        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Values.Look(ref hasConduitInt, "PRF_CTP_hasConduit", false);
            if (Scribe.mode == LoadSaveMode.Saving) version = 2;
            Scribe_Values.Look(ref version, "PRF_CTP_V", 1);
        }
        public ThingDef TransmitterDef => (props as CompProperties_TransmitsPower).transmitter ?? ThingDefOf.PowerConduit;
        public override void PostSpawnSetup(bool respawningAfterLoad) {
            base.PostSpawnSetup(respawningAfterLoad);
            if (version == 1 ||
                (!respawningAfterLoad && hasConduitInt)) {
                bool isTransmitterHere = false;
                foreach (var t in parent.Map.thingGrid.ThingsListAt(parent.Position)) {
                    if ((t as Building)?.TransmitsPowerNow == true) {
                        isTransmitterHere = true;
                        break;
                    }
                }
                if (!isTransmitterHere) {
                    var conduit = GenSpawn.Spawn(TransmitterDef, parent.Position, parent.Map);
                    conduit.SetFaction(Faction.OfPlayer); // heh; don't forget
                    hasConduitInt = false;
                    var comps = parent.AllComps;
                    for (int i = 0; i < comps.Count; i++) {
                        if (comps[i] is CompPower cp) {
                            cp.ConnectToTransmitter(conduit.TryGetComp<CompPower>(), respawningAfterLoad);
                            break;
                        }
                        if (comps[i] == this) {
                            Log.Warning("PRF Warning: " + parent.def.defName + " has " + this.GetType() + " before CompPower!\n" +
                                        "  This will make connecting to power grid difficult");
                        }
                    }
                }
            }
        }
        // TODO: mod setting: pick it up on despawn?
    }
    public class CompProperties_TransmitsPower : CompProperties {
        public CompProperties_TransmitsPower() {
            this.compClass = typeof(CompTransmitsPower);
        }
        public ThingDef transmitter = null;  //ThingDefOf.PowerConduit;
    }
}
