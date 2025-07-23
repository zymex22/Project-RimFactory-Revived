using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory
{
    public class CompTransmitsPower : ThingComp
    {
        //TODO: Sometime in 2021, this can be removed!
        static int version = 2;
        static ThingDef undergroundCable = null;
        bool hasConduitInt = true;
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref hasConduitInt, "PRF_CTP_hasConduit", false);
            if (Scribe.mode == LoadSaveMode.Saving) version = 2;
            Scribe_Values.Look(ref version, "PRF_CTP_V", 1);
        }
        public ThingDef TransmitterDef
        {
            get
            {
                if (props is not CompProperties_TransmitsPower propertiesTransmitsPower)
                {
                    Log.Error("PRF CompTransmitsPower is unexpectedly null");
                }
                else if (parent is not AutoMachineTool.IBeltConveyorLinkable { IsUnderground: true })
                {
                    return propertiesTransmitsPower.transmitter ?? ThingDefOf.HiddenConduit; 
                }
                if (undergroundCable != null) return undergroundCable;
                if (CompProperties_TransmitsPower.possibleUndergroundTransmitters.NullOrEmpty())
                {
                    return undergroundCable ??= ThingDefOf.HiddenConduit;
                }

                foreach (var dn in CompProperties_TransmitsPower
                             .possibleUndergroundTransmitters)
                {
                    var d = DefDatabase<ThingDef>.GetNamedSilentFail(dn);
                    if (d == null) continue;
                    undergroundCable = d;
                    break;
                }

                return undergroundCable ??= ThingDefOf.HiddenConduit;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (version != 1 && (respawningAfterLoad || !hasConduitInt)) return;
            
            var isTransmitterHere = false;
            foreach (var t in parent.Map.thingGrid.ThingsListAt(parent.Position))
            {
                if ((t as Building)?.TransmitsPowerNow == true)
                {
                    isTransmitterHere = true;
                    break;
                }
            }

            if (isTransmitterHere || !Common.ProjectRimFactory_ModSettings.PRF_PlaceConveyorCable) return;
            var conduit = GenSpawn.Spawn(TransmitterDef, parent.Position, parent.Map);
            conduit.SetFaction(Faction.OfPlayer); // heh; don't forget
            hasConduitInt = false;
            var comps = parent.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is CompPower cp)
                {
                    cp.ConnectToTransmitter(conduit.TryGetComp<CompPower>(), respawningAfterLoad);
                    break;
                }
                if (comps[i] == this)
                {
                    Log.Warning("PRF Warning: " + parent.def.defName + " has " + this.GetType() + " before CompPower!\n" +
                                "  This will make connecting to power grid difficult");
                }
            }
        }
        // TODO: mod setting: pick it up on despawn?
    }
    public class CompProperties_TransmitsPower : CompProperties
    {
        public CompProperties_TransmitsPower()
        {
            this.compClass = typeof(CompTransmitsPower);
        }
        public ThingDef transmitter = null;  //ThingDefOf.HiddenConduit;
        static public List<string> possibleUndergroundTransmitters;
    }
}
