using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Common
{
    public class Cache<T>
    {
        Func<T> cacheGetter;
        T cache;
        int updateInterval = 10;
        int lastTick;
        public Cache(Func<T> func)
        {
            cacheGetter = func;
            cache = cacheGetter();
        }
        public Cache(Func<T> func, int ticksUpdateInterval) : this(func)
        {
            updateInterval = ticksUpdateInterval;
        }
        public T Get()
        {
            if (lastTick + updateInterval < Find.TickManager.TicksAbs)
            {
                return UpdateCache();
            }
            return cache;
        }
        public T UpdateCache()
        {
            lastTick = Find.TickManager.TicksAbs;
            return cache = cacheGetter();
        }
    }
}
