using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;

using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    class Graphic_Selectable : Graphic_Collection
    {

        public override Material MatSingle
        {
            get
            {
                return this.subGraphics[0].MatSingle;
            }
        }

        public Graphic Get(string path)
        {
            if (path == null)
            {
                Option(this.subGraphics[0].data).ForEach(d => d.drawRotated = true);
                return this.subGraphics[0];
            }
            if (!pathDic.ContainsKey(path))
            {
                pathDic[path] = this.subGraphics.Where(x => x.path == path).First();
                Option(pathDic[path].data).ForEach(d => d.drawRotated = true);
            }
            return this.pathDic[path];
        }

        private Dictionary<string, Graphic> pathDic = new Dictionary<string, Graphic>();

        public override bool ShouldDrawRotated
        {
            get
            {
                return true;
            }
        }

        public override void Init(GraphicRequest req)
        {
            base.Init(req);

            this.subGraphics.ForEach(g => Option(g.data).ForEach(d => d.drawRotated = true));
        }
    }
}
