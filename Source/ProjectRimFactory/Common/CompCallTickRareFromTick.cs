using Verse;

namespace ProjectRimFactory.Common
{
    // ReSharper disable once UnusedType.Global
    public class CompCallTickRareFromTick : ThingComp
    {
        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                parent?.TickRare();
            }
        }
    }
}
