using Verse;
namespace ProjectRimFactory.CultivatorTools
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CultivatorDefModExtension : DefModExtension, Common.IXMLThingDescription
    {
        public int TickFrequencyDivisor = 200;
        public int squareAreaRadius;
        public int GrowRate = 2500;

        public string GetDescription(ThingDef def)
        {
            var range = 0;
            if (squareAreaRadius > 0)
            {
                range = squareAreaRadius;
            }
            else if (def.specialDisplayRadius > 0)
            {
                range = (int)def.specialDisplayRadius;
            }
            var text = "PRF_UTD_CultivatorDefModExtension_Range".Translate(range) + "\r\n";

            text += "PRF_UTD_CultivatorDefModExtension_Tickdev".Translate(TickFrequencyDivisor) + "\r\n";

            return text;
        }
    }
}
