using Verse;

namespace ProjectRimFactory.Common
{
    public class CompCallTickRareFromTick : ThingComp
    {
        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                this.parent?.TickRare();
            }
        }
    }
}
