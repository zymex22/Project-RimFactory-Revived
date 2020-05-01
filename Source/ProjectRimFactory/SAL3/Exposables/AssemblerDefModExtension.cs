using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.SAL3.Exposables
{
    public class AssemblerDefModExtension : DefModExtension
    {
        public float workSpeedBaseFactor = 1f;
        public List<ThingDef> importRecipesFrom;
        public int skillLevel = 20;
        public bool drawStatus = false;

        public GraphicData workingGraphicData;

        public Graphic WorkingGrahic
        {
            get
            {
                if(workingGraphicData != null)
                {
                    return workingGraphicData.Graphic;
                }
                return null;
            }
        }
    }
}
