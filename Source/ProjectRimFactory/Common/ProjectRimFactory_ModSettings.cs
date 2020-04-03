using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ProjectRimFactory_ModSettings : ModSettings
    {
        public override void ExposeData()
        {
        }

        public void DoWindowContents(Rect inRect)
        {
            var list = new Listing_Standard()
            {
                ColumnWidth = inRect.width
            };
            list.Begin(inRect);
            list.Label("Nothing here...");
            list.End();
        }
    }
}
