using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    public abstract class Building_RadialCellIterator : Building_CellIterator
    {
        public int RadialCellCount { get; private set; }

        public override IntVec3 Current => GenRadial.RadialPattern[currentPosition + 1] + Position;

        protected override int cellCount => RadialCellCount;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            RadialCellCount = GenRadial.NumCellsInRadius(def.specialDisplayRadius);
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
        }
    }
}