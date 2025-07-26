using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ModExtension_Puller : DefModExtension
    {
        public bool outputSides = false;


        public IntVec3 GetOutputCell(IntVec3 pos, Rot4 rot, bool isRight = false)
        {
            if (outputSides)
            {
                var dir = RotationDirection.Clockwise;
                if (!isRight)
                {
                    dir = RotationDirection.Counterclockwise;
                }
                return pos + rot.RotateAsNew(dir).FacingCell;
            }
            return pos + rot.FacingCell;
        }
    }
}
