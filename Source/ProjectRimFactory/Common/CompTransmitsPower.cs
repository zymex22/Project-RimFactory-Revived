using System.Collections.Generic;
using ProjectRimFactory.AutoMachineTool;
using RimWorld;
using Verse;

namespace ProjectRimFactory
{
    public class CompTransmitsPower : ThingComp
    {
        //TODO: Sometime in 2021, this can be removed!
        private static int version = 2;
        private static ThingDef undergroundCable;
        private bool hasConduitInt = true;

        public ThingDef TransmitterDef
        {
            get
            {
                var p = props as CompProperties_TransmitsPower;
                if (parent is IBeltConveyorLinkable belt
                    && belt.IsUnderground)
                {
                    if (undergroundCable == null)
                    {
                        if (!CompProperties_TransmitsPower
                            .possibleUndergroundTransmitters.NullOrEmpty())
                            foreach (var dn in CompProperties_TransmitsPower
                                .possibleUndergroundTransmitters)
                            {
                                var d = DefDatabase<ThingDef>.GetNamedSilentFail(dn);
                                if (d != null)
                                {
                                    undergroundCable = d;
                                    break;
                                }
                            }

                        if (undergroundCable == null) undergroundCable = ThingDefOf.PowerConduit;
                    }

                    return undergroundCable;
                }

                return p.transmitter ?? ThingDefOf.PowerConduit;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref hasConduitInt, "PRF_CTP_hasConduit");
            if (Scribe.mode == LoadSaveMode.Saving) version = 2;
            Scribe_Values.Look(ref version, "PRF_CTP_V", 1);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (version == 1 ||
                !respawningAfterLoad && hasConduitInt)
            {
                var isTransmitterHere = false;
                foreach (var t in parent.Map.thingGrid.ThingsListAt(parent.Position))
                    if ((t as Building)?.TransmitsPowerNow == true)
                    {
                        isTransmitterHere = true;
                        break;
                    }

                if (!isTransmitterHere)
                {
                    var conduit = GenSpawn.Spawn(TransmitterDef, parent.Position, parent.Map);
                    conduit.SetFaction(Faction.OfPlayer); // heh; don't forget
                    hasConduitInt = false;
                    var comps = parent.AllComps;
                    for (var i = 0; i < comps.Count; i++)
                    {
                        if (comps[i] is CompPower cp)
                        {
                            cp.ConnectToTransmitter(conduit.TryGetComp<CompPower>(), respawningAfterLoad);
                            break;
                        }

                        if (comps[i] == this)
                            Log.Warning("PRF Warning: " + parent.def.defName + " has " + GetType() +
                                        " before CompPower!\n" +
                                        "  This will make connecting to power grid difficult");
                    }
                }
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
        }

        // TODO: mod setting: pick it up on despawn?
    }

    public class CompProperties_TransmitsPower : CompProperties
    {
        public static List<string> possibleUndergroundTransmitters;
        public ThingDef transmitter = null; //ThingDefOf.PowerConduit;

        public CompProperties_TransmitsPower()
        {
            compClass = typeof(CompTransmitsPower);
        }
    }
}