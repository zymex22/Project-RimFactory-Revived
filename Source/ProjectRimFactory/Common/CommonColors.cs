using UnityEngine;

namespace ProjectRimFactory.Common
{
    internal static class CommonColors
    {

        public static Color GetCellPatternColor(CellPattern pat)
        {
            return pat switch
            {
                CellPattern.BlueprintMin => BlueprintMin,
                CellPattern.BlueprintMax => BlueprintMax,
                CellPattern.Instance => Instance,
                CellPattern.OtherInstance => OtherInstance,
                CellPattern.OutputCell => OutputCell,
                CellPattern.OutputZone => OutputZone,
                CellPattern.InputCell => InputCell,
                CellPattern.InputZone => InputZone,
                _ => Color.white
            };
        }

        public static Color BlueprintMin = Color.white;
        public static Color BlueprintMax = Color.gray.A(0.6f);
        public static Color Instance = Color.white;
        public static Color OtherInstance = Color.white.A(0.35f);
        public static Color InputCell = Color.white;
        private static readonly Color InputZone = Color.white.A(0.5f);
        public static Color OutputCell = Color.yellow;
        public static Color OutputZone = Color.yellow.A(0.5f);
        public static Color WorkbenchAlpha = Color.blue.A(0.5f);
        public static Color SeedsInputZone = new(0.3f, 0.15f, 0f);//#4d2600


        public enum CellPattern
        {
            BlueprintMin,
            BlueprintMax,
            Instance,
            OtherInstance,
            OutputCell,
            OutputZone,
            InputCell,
            InputZone,
        }
    }
}
