using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectRimFactory.Common
{
    class CommonColors
    {

        static public Color GetCellPatternColor(CellPattern pat)
        {
            switch (pat)
            {
                case CellPattern.BlurprintMin:
                    return blueprintMin;
                case CellPattern.BlurprintMax:
                    return blueprintMax;
                case CellPattern.Instance:
                    return instance;
                case CellPattern.OtherInstance:
                    return otherInstance;
                case CellPattern.OutputCell:
                    return outputCell;
                case CellPattern.OutputZone:
                    return outputZone;
                case CellPattern.InputCell:
                    return inputCell;
                case CellPattern.InputZone:
                    return inputZone;
            }
            return Color.white;
        }

        static public Color blueprintMin = Color.white;
        static public Color blueprintMax = Color.gray.A(0.6f);
        static public Color instance = Color.white;
        static public Color otherInstance = Color.white.A(0.35f);
        static public Color inputCell = Color.white;
        static public Color inputZone = Color.white.A(0.5f);
        static public Color outputCell = Color.yellow;
        static public Color outputZone = Color.yellow.A(0.5f);
        static public Color WorkbenchCell = Color.green;
        static public Color WorkbenchAlpha = Color.green.A(0.5f);


        public enum CellPattern
        {
            BlurprintMin,
            BlurprintMax,
            Instance,
            OtherInstance,
            OutputCell,
            OutputZone,
            InputCell,
            InputZone,
        }


    }
}
