using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Verse;

using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    class Graphic_Selectable : Graphic_Collection
    {

        public override Material MatSingle => subGraphics[0].MatSingle;

        public Graphic Get(string path)
        {
            if (path == null)
            {
                Option(subGraphics[0].data).ForEach(d => d.drawRotated = true);
                return subGraphics[0];
            }
            if (!pathDic.ContainsKey(path))
            {
                pathDic[path] = subGraphics.Where(x => x.path == path).First();
                Option(pathDic[path].data).ForEach(d => d.drawRotated = true);
            }
            return pathDic[path];
        }

        private Dictionary<string, Graphic> pathDic = new();

        public override bool ShouldDrawRotated => true;

        public override void Init(GraphicRequest req)
        {
            base.Init(req);

            subGraphics.ForEach(g => Option(g.data).ForEach(d => d.drawRotated = true));
        }
    }
}
