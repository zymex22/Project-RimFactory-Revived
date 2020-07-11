﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ProjectRimFactory.Common
{
    public static class GridsUtilityExt
    {
        public static T GetFirst<T>(this IntVec3 c, Map map) where T : class
        {
            if (map == null) { return null; }
            foreach (var th in map.thingGrid.ThingsListAt(c))
            {
                if (th is T)
                {
                    return th as T;
                }
            }
            return null;
        }
    }
}
