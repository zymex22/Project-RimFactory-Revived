using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

namespace ProjectRimFactory.Common
{
    public class PRFMapComponent : MapComponent
    {
        private List<ITicker> tickers = new List<ITicker>();
        // iHideRightMenus: see HarmonyPatches/PatchStorage.cs
        public HashSet<IntVec3> iHideRightMenus = new HashSet<IntVec3>();

        public PRFMapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            this.tickers.ForEach(t => t.Tick());
        }

        public void AddTicker(ITicker ticker)
        {
            this.tickers.Add(ticker);
        }

        public void RemoveTicker(ITicker ticker)
        {
            this.tickers.Remove(ticker);
        }
        public void AddIHideRightClickMenu(Building b)
        {
            foreach (var v in b.OccupiedRect()) {
                iHideRightMenus.Add(v);
            }
        }
        public void RemoveIHideRightClickMenu(Building b)
        {
            foreach (var v in b.OccupiedRect()) {
                iHideRightMenus.Remove(v);
                foreach (var t in map.thingGrid.ThingsListAt(v)) {
                    if (t != b && t is IHideRightClickMenu) iHideRightMenus.Add(v);
                }
            }
        }
    }

    public interface ITicker
    {
        void Tick();
    }
    // NOTE: You might register AND deregister via Map.GetComponent<PRFMapComponent>().iHideRightMenus.Add(this);
    public interface IHideRightClickMenu
    {
        bool HideRightClickMenus { get; }
    }
}
