using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ProjectRimFactory.Common
{
    interface ILimitWatcher
    {
        public bool ItemIsLimit(ThingDef thing,bool CntStacks, int limit);

    }
}
