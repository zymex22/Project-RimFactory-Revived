using RimWorld;
using Verse;


/*
Code Taken from https://github.com/KiameV/rimworld-rimfridge/blob/09e15aeef1c702dd729f267e4b51016edbb6f63f/Source/CompProperties_SecondLayer.cs & https://github.com/KiameV/rimworld-rimfridge/blob/09e15aeef1c702dd729f267e4b51016edbb6f63f/Source/CompSecondLayer.cs
Code has been published with the MIT Licence
Code was provided by "vendan" to rimfrige
 */

namespace ProjectRimFactory.Common
{
    internal class CompSecondLayer : ThingComp
    {
        private Graphic graphicInt;

        public CompProperties_SecondLayer Props => (CompProperties_SecondLayer)props;

        public virtual Graphic Graphic
        {
            get
            {
                if (graphicInt == null)
                {
                    if (Props.graphicData == null)
                    {
                        Log.ErrorOnce(parent.def + " has no SecondLayer graphicData but we are trying to access it.", 764532);
                        return BaseContent.BadGraphic;
                    }
                    graphicInt = Props.graphicData.GraphicColoredFor(parent);
                }
                return graphicInt;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Graphic.Draw(GenThing.TrueCenter(parent.Position, parent.Rotation, parent.def.size, Props.Altitude), parent.Rotation, parent);
        }
    }
    internal class CompProperties_SecondLayer : CompProperties
    {
        public GraphicData graphicData = null;

        public AltitudeLayer altitudeLayer = AltitudeLayer.MoteOverhead;

        public float Altitude => Altitudes.AltitudeFor(altitudeLayer);

        public CompProperties_SecondLayer()
        {
            compClass = typeof(CompSecondLayer);
        }
    }
}