using RimWorld;
using System.Linq;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Building_HarvestPuller : Building_ItemPuller
    {
        protected override Thing TargetThing()
        {

            Zone z = (this.Position + this.Rotation.Opposite.FacingCell)
                .GetZone(this.Map);
            //Only allow Zones that are for growing
            if (z is not IPlantToGrowSettable) z = null;

            Thing target;
            if (z == null) return null;
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
