using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ModExtension_Graphic : DefModExtension
    {
        [XmlInheritanceAllowDuplicateNodes]
        private readonly List<GraphicDataListItem> graphicDataList = new List<GraphicDataListItem>();

        public IEnumerable<Graphic> Graphics => graphicDataList.Select(i => i.Graphic);

        public Graphic FirstGraphic => Graphics.FirstOrDefault();

        public Graphic GetByName(string name)
        {
            return graphicDataList.Where(g => g.name == name).Select(i => i.Graphic).FirstOrDefault();
        }
    }

    public class GraphicDataListItem
    {
        public GraphicData graphicData;

        public string name;

        public Graphic Graphic => graphicData?.Graphic ?? null;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            name = xmlRoot.Name;
            graphicData = DirectXmlToObject.ObjectFromXml<GraphicData>(xmlRoot.FirstChild, false);
        }
    }
}