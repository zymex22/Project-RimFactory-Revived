using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class PlaceWorker_AutoMachineTool : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            var r = base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore);
            if (r.Accepted)
            {
                if (!(new Building_AutoMachineTool()).GetTarget(loc,rot,map))
                {
                    return new AcceptanceReport("PRF.AutoMachineTool.PlaceNotAllowed".Translate());
                }
                return r;
            }
            else
            {
                return r;
            }
        }
    }
}