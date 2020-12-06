using System.Linq;
using RimWorld;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_HarvestPuller : Building_ItemPuller
    {
        protected override Option<Thing> TargetThing()
        {
            var z = (Position + Rotation.Opposite.FacingCell)
                .GetZone(Map) as Zone_Growing;
            if (z == null) return Nothing<Thing>();
            if (takeForbiddenItems)
                return z.AllContainedThings
                    .Where(t => t.def.category == ThingCategory.Item)
                    .Where(t => settings.AllowedToAccept(t))
                    .Where(t => !IsLimit(t))
                    .FirstOption();
            return z.AllContainedThings
                .Where(t => t.def.category == ThingCategory.Item)
                .Where(t => !t.IsForbidden(Faction.OfPlayer))
                .Where(t => settings.AllowedToAccept(t))
                .Where(t => !IsLimit(t))
                .FirstOption();
        }
    }
}