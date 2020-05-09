using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Xml;

namespace ProjectRimFactory.Common
{
    public class ModExtension_Graphic : DefModExtension
    {
        [XmlInheritanceAllowDuplicateNodes]
        List<GraphicDataListItem> graphicDataList = new List<GraphicDataListItem>();

        public IEnumerable<Graphic> Graphics => this.graphicDataList.Select(i => i.Graphic);

        public Graphic FirstGraphic => this.Graphics.FirstOrDefault();

        public Graphic GetByName(string name)
        {
            return this.graphicDataList.Where(g => g.name == name).Select(i => i.Graphic).FirstOrDefault();
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
