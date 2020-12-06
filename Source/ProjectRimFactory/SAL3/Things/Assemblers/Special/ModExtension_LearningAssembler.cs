using RimWorld;
using Verse;

namespace ProjectRimFactory.SAL3.Things.Assemblers.Special
{
    internal class ModExtension_LearningAssembler : DefModExtension
    {
        public QualityCategory MaxQuality = QualityCategory.Legendary;
        public float MaxSpeed = float.PositiveInfinity;

        public QualityCategory MinQuality = QualityCategory.Awful;
        public float MinSpeed = 0.5f;
    }
}