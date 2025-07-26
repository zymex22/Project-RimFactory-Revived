using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    public abstract class Building_RadialCellIterator : Building_CellIterator
    {
        public int RadialCellCount { get; private set; }

        protected override IntVec3 Current => GenRadial.RadialPattern[CurrentPosition + 1] + Position;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            RadialCellCount = GenRadial.NumCellsInRadius(def.specialDisplayRadius);
        }

        protected override int CellCount => RadialCellCount;

    }
}
