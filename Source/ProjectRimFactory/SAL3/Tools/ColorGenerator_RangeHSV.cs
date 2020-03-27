using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using UnityRandom = UnityEngine.Random;

namespace ProjectRimFactory.SAL3.Tools
{
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
