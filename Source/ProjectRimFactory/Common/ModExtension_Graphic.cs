using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ModExtension_Graphic : DefModExtension
    {
        [XmlInheritanceAllowDuplicateNodes]
        List<GraphicDataListItem> graphicDataList = new List<GraphicDataListItem>();

        public IEnumerable<Graphic> Graphics => this.graphicDataList.Select(i => i.Graphic);

        public Graphic FirstGraphic => this.Graphics.FirstOrDefault();

        private Dictionary<string, int> getByNameCache = new Dictionary<string, int>();

        public Graphic GetByName(string name)
        {
            int index;
            if (name is null) return null;
            if (!getByNameCache.TryGetValue(name, out index))
            {
                var value = this.graphicDataList.Where(g => g.name == name).FirstOrDefault();
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
            this.name = xmlRoot.Name;
            this.graphicData = DirectXmlToObject.ObjectFromXml<GraphicData>(xmlRoot.FirstChild, false);
        }

        public string name;

        public GraphicData graphicData;

        public Graphic Graphic => this.graphicData?.Graphic ?? null;
    }
}
