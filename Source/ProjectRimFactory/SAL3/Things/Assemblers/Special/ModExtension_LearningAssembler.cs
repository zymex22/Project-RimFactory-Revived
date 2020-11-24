using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;


namespace ProjectRimFactory.SAL3.Things.Assemblers.Special
{
    class ModExtension_LearningAssembler : DefModExtension
    {
        public float MinSpeed = 0.5f;
        public float MaxSpeed = float.PositiveInfinity;

        public QualityCategory MinQuality = QualityCategory.Awful;

        public QualityCategory MaxQuality = QualityCategory.Legendary;







    }
}
