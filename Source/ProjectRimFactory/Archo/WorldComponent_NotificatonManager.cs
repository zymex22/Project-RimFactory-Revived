using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Archo
{
    public class WorldComponent_NotificatonManager : WorldComponent
    {
        public List<string> notifiedMessages = new List<string>();

        public WorldComponent_NotificatonManager(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref notifiedMessages, "notifiedMessages", LookMode.Value);
        }
    }
}
