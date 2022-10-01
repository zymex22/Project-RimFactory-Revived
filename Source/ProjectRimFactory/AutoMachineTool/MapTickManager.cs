using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class MapTickManager : MapComponent
    {
        public MapTickManager(Map map) : base(map)
        {
            //            this.thingsList = new ThingLister(map);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            var removeSet = this.eachTickActions.ToList().Where(Exec).ToHashSet();
            removeSet.ForEach(r => this.eachTickActions.Remove(r));

#if DEBUG
            if ((Debug.activeFlags & Debug.Flag.Benchmark) > 0) {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                var beforeCount = GC.CollectionCount(0);

                StringBuilder b = new StringBuilder();
                var tickers = this.tickActionsDict.GetOption(Find.TickManager.TicksGame);
                if (tickers.HasValue) {
                    foreach (var a in tickers.Value.ToList()) {
                        System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();
                        sw2.Start();
                        Exec(a);
                        sw2.Stop();
                        var micros = (double)sw2.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency * 1000d * 1000d;
                        b.Append(a.Target.GetType().ToString() + "." + a.Method.Name + " / elapse : " + micros + "micros \n");
                    }
                }

                var gcCount = GC.CollectionCount(0) - beforeCount;

                sw.Stop();
                var millis = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency * 1000d;
                if (millis > 2d) {
                    if (gcCount >= 1) {
                        L("GC called");
                    } else {
                        L("millis : " + millis + " / methods : " + b.ToString());
                    }
                }
            } else { // debug flag off:
                this.tickActionsDict.GetOption(Find.TickManager.TicksGame).ForEach(s => s.ToList().ForEach(Exec));
            }
#else
            // Need ToList() b/c the list of tickActions can change
            this.tickActionsDict.GetOption(Find.TickManager.TicksGame).ForEach(s => s.ToList().ForEach(Exec));
#endif
            this.tickActionsDict.Remove(Find.TickManager.TicksGame);
        }

        private static void Exec(Action act)
        {
            try
            {
                act();
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        private static T Exec<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return default(T);
            }
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            // ここでいいのか・・・？
            if ((Find.MainTabsRoot.OpenTab?.TabWindow as MainTabWindow_Architect)
                ?.selectedDesPanel?.def.defName == "Industrial")
            {
                OverlayDrawHandler_UGConveyor.DrawOverlayThisFrame();
            }
            /*            Option(Find.MainTabsRoot.OpenTab)
                            .Select(r => r.TabWindow)
                            .SelectMany(w => Option(w as MainTabWindow_Architect))
                            .SelectMany(a => Option(a.selectedDesPanel))
                            .Where(p => p.def.defName == "Industrial")
                            .ForEach(p => OverlayDrawHandler_UGConveyor.DrawOverlayThisFrame());
            */
            if (Find.Selector.FirstSelectedObject is IBeltConveyorLinkable)
            {
                OverlayDrawHandler_UGConveyor.DrawOverlayThisFrame();
            }
        }


        private Dictionary<int, HashSet<Action>> tickActionsDict = new Dictionary<int, HashSet<Action>>();

        private HashSet<Func<bool>> eachTickActions = new HashSet<Func<bool>>();

        public void AfterAction(int ticks, Action act)
        {
            if (ticks < 1)
                ticks = 1;

            if (!this.tickActionsDict.TryGetValue(Find.TickManager.TicksGame + ticks, out HashSet<Action> set))
            {
                set = new HashSet<Action>();
                this.tickActionsDict[Find.TickManager.TicksGame + ticks] = set;
            }

            set.Add(act);
        }

        public void NextAction(Action act)
        {
            this.AfterAction(1, act);
        }

        public void EachTickAction(Func<bool> act)
        {
            this.eachTickActions.Add(act);
        }

        public void RemoveAfterAction(Action act)
        {
            this.tickActionsDict.ForEach(kv => kv.Value.Remove(act));
        }

        public void RemoveEachTickAction(Func<bool> act)
        {
            this.eachTickActions.Remove(act);
        }

        public bool IsExecutingThisTick(Action act)
        {
            return this.tickActionsDict.GetOption(Find.TickManager.TicksGame).Select(l => l.Contains(act)).GetOrDefault(false);
        }

        //      private ThingLister thingsList;

        //        public ThingLister ThingsList => thingsList;

#if DEBUG
        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            if(Widgets.ButtonText(new Rect(200, 10, 150, 20), "Test(debug)"))
            {
                this.map.wealthWatcher.ForceRecount();
                L("wealth items : " + this.map.wealthWatcher.WealthItems);
            }
        }
#endif
    }

    public class ThingLister
    {
        public ThingLister(Map map)
        {
            this.map = map;
        }

        private Map map;

        private Dictionary<Type, List<ThingDef>> typeDic = new Dictionary<Type, List<ThingDef>>();

        public IEnumerable<T> ForAssignableFrom<T>() where T : Thing
        {
            if (!typeDic.TryGetValue(typeof(T), out List<ThingDef> defs))
            {
                defs = DefDatabase<ThingDef>.AllDefs.Where(d => typeof(T).IsAssignableFrom(d.thingClass)).ToList();
                typeDic[typeof(T)] = defs;
#if DEBUG
                L("ThingLister type : " + typeof(T) + " / defs count : " + defs.Count);
#endif
            }
            return defs.SelectMany(d => this.map.listerThings.ThingsOfDef(d)).SelectMany(t => Option(t as T));
        }
    }
}
