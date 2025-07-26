using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Tools
{
    // ReSharper disable once UnusedType.Global
    public class ColorGenerator_RangeHSV : ColorGenerator
    {
        public FloatRange rangeH;
        public FloatRange rangeS;
        public FloatRange rangeV;
        public override Color NewRandomizedColor()
        {
            return Color.HSVToRGB(rangeH.RandomInRange, rangeS.RandomInRange, rangeV.RandomInRange);
        }
    }
}
