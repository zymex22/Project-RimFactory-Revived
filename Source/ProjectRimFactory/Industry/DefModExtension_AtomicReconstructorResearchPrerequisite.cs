using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Industry
{
    public class DefModExtension_AtomicReconstructorResearchPrerequisite : DefModExtension
    {
        public bool ignoreMainPrerequisites;
        public List<ResearchProjectDef> prerequisites;
    }
}
