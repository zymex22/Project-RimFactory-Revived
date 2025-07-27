using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Archo
{
    public class WorldComponent_NotificatonManager(World world) : WorldComponent(world)
    {
        public List<string> notifiedMessages = [];

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref notifiedMessages, "notifiedMessages", LookMode.Value);
        }
    }
}
