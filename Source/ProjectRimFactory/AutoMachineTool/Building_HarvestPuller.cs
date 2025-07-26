using RimWorld;
using System.Linq;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    // ReSharper disable once UnusedType.Global
    public class Building_HarvestPuller : Building_ItemPuller
    {
        protected override Thing TargetThing()
        {

            var zone = (Position + Rotation.Opposite.FacingCell).GetZone(Map);
            //Only allow Zones that are for growing
            if (zone is not IPlantToGrowSettable) return null;

            var target = zone.AllContainedThings
                .Where(t => t.def.category == ThingCategory.Item)
                .Where(t => !t.IsForbidden(Faction.OfPlayer) || TakeForbiddenItems)
                .Where(t => settings.AllowedToAccept(t)).FirstOrDefault(t => !IsLimit(t));

            if (target is null) return null;
            if (takeSingleItems) return target.SplitOff(1);
            // SplitOff ensures any item-removal effects happen:
            return target.SplitOff(target.stackCount);
        }

    }
}
