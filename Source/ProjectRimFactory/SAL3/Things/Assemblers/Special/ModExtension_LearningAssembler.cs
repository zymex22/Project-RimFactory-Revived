using RimWorld;
using Verse;


namespace ProjectRimFactory.SAL3.Things.Assemblers.Special
{
    class ModExtension_LearningAssembler : DefModExtension, ProjectRimFactory.Common.IXMLThingDescription
    {
        public float MinSpeed = 0.5f;
        public float MaxSpeed = float.PositiveInfinity;

        public QualityCategory MinQuality = QualityCategory.Awful;

        public QualityCategory MaxQuality = QualityCategory.Legendary;

        public string GetDescription(ThingDef def)
        {
            string text = "";

            if (MinSpeed == MaxSpeed)
            {
                text += "PRF_UTD_ModExtension_LearningAssembler_Speed".Translate(MinSpeed * 100);
            }
            else
            {
                var maxSpeedString = "PRF_UTD_ModExtension_LearningAssembler_PositiveInfinity".Translate();
                if (MaxSpeed < float.PositiveInfinity) maxSpeedString = (MaxSpeed * 100).ToString();

                text += "PRF_UTD_ModExtension_LearningAssembler_Speed".Translate($"{MinSpeed * 100}% - {maxSpeedString}");
            }
            text += "\r\n";
            if (MinQuality == MaxQuality)
            {
                text += "PRF_UTD_ModExtension_LearningAssembler_Quality".Translate(MinQuality.ToString());
            }
            else
            {
                text += "PRF_UTD_ModExtension_LearningAssembler_Quality".Translate($"{MinQuality} - {MaxQuality}");
            }
            text += "\r\n";


            return text;
        }
    }
}
