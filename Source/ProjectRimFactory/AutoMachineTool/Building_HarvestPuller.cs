using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static ProjectRimFactory.AutoMachineTool.Ops;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_HarvestPuller : Building_ItemPuller
    {
        protected override Thing TargetThing()
        {
            Zone_Growing z= (this.Position + this.Rotation.Opposite.FacingCell)
                .GetZone(this.Map) as Zone_Growing;
            Thing target;
            if ( z == null ) return null;
            target = z.AllContainedThings
                    .Where(t => t.def.category == ThingCategory.Item)
                    .Where(t => !t.IsForbidden(Faction.OfPlayer) || this.takeForbiddenItems)
                    .Where(t => this.settings.AllowedToAccept(t))
                    .Where(t => !this.IsLimit(t)).FirstOrDefault<Thing>();

            if (target == null) return target;
            if (this.takeSingleItems) return (target.SplitOff(1));
            // SplitOff ensures any item-removal effects happen:
            return (target.SplitOff(target.stackCount));

        }

    }
}
