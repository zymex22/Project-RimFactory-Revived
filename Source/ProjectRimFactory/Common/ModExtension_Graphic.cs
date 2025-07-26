using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ModExtension_Graphic : DefModExtension
    {
        [XmlInheritanceAllowDuplicateNodes]
        List<GraphicDataListItem> graphicDataList = [];

        public IEnumerable<Graphic> Graphics => graphicDataList.Select(i => i.Graphic);

        public Graphic FirstGraphic => Graphics.FirstOrDefault();

        private Dictionary<string, int> getByNameCache = new();

        public Graphic GetByName(string name)
        {
            if (name is null) return null;
            if (!getByNameCache.TryGetValue(name, out var index))
            {
                var value = graphicDataList.FirstOrDefault(g => g.name == name);
                index = graphicDataList.IndexOf(value);
                getByNameCache.Add(name, index);
            }

            return graphicDataList[index].Graphic;
        }
    }

    public class GraphicDataListItem
    {
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            name = xmlRoot.Name;
            graphicData = DirectXmlToObject.ObjectFromXml<GraphicData>(xmlRoot.FirstChild, false);
        }

        public string name;

        public GraphicData graphicData;

        public Graphic Graphic => graphicData?.Graphic ?? null;
    }
}
