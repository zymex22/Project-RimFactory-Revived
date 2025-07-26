using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    public class SquareCellIterator
    {
        int rangeInt;
        public int Range => rangeInt;
        public readonly IntVec3[] CellPattern;
        public SquareCellIterator(int range)
        {
            rangeInt = range;
            CellPattern = new IntVec3[(range * 2 + 1) * (range * 2 + 1)];
            var currentIter = 0;
            for (var i = -range; i <= range; i++)
            {
                if ((i & 1) == 0)
                {
                    for (var j = -range; j <= range; j++, currentIter++)
                    {
                        CellPattern[currentIter] = new IntVec3(i, 0, j);
                    }
                }
                else
                {
                    for (int j = range; j >= -range; j--, currentIter++)
                    {
                        CellPattern[currentIter] = new IntVec3(i, 0, j);
                    }
                }
            }
        }
    }
}
