using System;
using Verse;

namespace ProjectRimFactory.Common
{
    public class Cache<T>
    {
        private T cache;
        private readonly Func<T> cacheGetter;
        private int lastTick;
        private readonly int updateInterval = 10;

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
            if (lastTick + updateInterval < Find.TickManager.TicksAbs) return UpdateCache();
            return cache;
        }

        public T UpdateCache()
        {
            lastTick = Find.TickManager.TicksAbs;
            return cache = cacheGetter();
        }
    }
}