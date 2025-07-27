using System;
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
        public T Get()
        {
            if (lastTick + updateInterval < Find.TickManager.TicksAbs)
            {
                return UpdateCache();
            }
            return cache;
        }

        private T UpdateCache()
        {
            lastTick = Find.TickManager.TicksAbs;
            return cache = cacheGetter();
        }
    }
}
