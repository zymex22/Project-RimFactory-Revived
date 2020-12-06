using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Common
{
    public class PRFMapComponent : MapComponent
    {
        private readonly List<ITicker> tickers = new List<ITicker>();

        public PRFMapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            tickers.ForEach(t => t.Tick());
        }

        public void AddTicker(ITicker ticker)
        {
            tickers.Add(ticker);
        }

        public void RemoveTicker(ITicker ticker)
        {
            tickers.Remove(ticker);
        }
    }

    public interface ITicker
    {
        void Tick();
    }
}