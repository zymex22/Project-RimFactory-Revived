using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class MapTickManager : MapComponent
    {
        private readonly HashSet<Func<bool>> eachTickActions = new HashSet<Func<bool>>();


        private readonly Dictionary<int, HashSet<Action>> tickActionsDict = new Dictionary<int, HashSet<Action>>();

        public MapTickManager(Map map) : base(map)
        {
//            this.thingsList = new ThingLister(map);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            var removeSet = eachTickActions.ToList().Where(Exec).ToHashSet();
            removeSet.ForEach(r => eachTickActions.Remove(r));

#if DEBUG
            if ((Debug.activeFlags & Debug.Flag.Benchmark) > 0)
            {
                var sw = new Stopwatch();
                sw.Start();

                var beforeCount = GC.CollectionCount(0);

                var b = new StringBuilder();
                var tickers = tickActionsDict.GetOption(Find.TickManager.TicksGame);
                if (tickers.HasValue)
                    foreach (var a in tickers.Value.ToList())
                    {
                        var sw2 = new Stopwatch();
                        sw2.Start();
                        Exec(a);
                        sw2.Stop();
                        var micros = sw2.ElapsedTicks / (double) Stopwatch.Frequency * 1000d * 1000d;
                        b.Append(a.Target.GetType() + "." + a.Method.Name + " / elapse : " + micros + "micros \n");
                    }

                var gcCount = GC.CollectionCount(0) - beforeCount;

                sw.Stop();
                var millis = sw.ElapsedTicks / (double) Stopwatch.Frequency * 1000d;
                if (millis > 2d)
                {
                    if (gcCount >= 1)
                        L("GC called");
                    else
                        L("millis : " + millis + " / methods : " + b);
                }
            }
            else
            {
                // debug flag off:
                tickActionsDict.GetOption(Find.TickManager.TicksGame).ForEach(s => s.ToList().ForEach(Exec));
            }
#else
            // Need ToList() b/c the list of tickActions can change
            this.tickActionsDict.GetOption(Find.TickManager.TicksGame).ForEach(s => s.ToList().ForEach(Exec));
#endif
            tickActionsDict.Remove(Find.TickManager.TicksGame);
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
                return default;
            }
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            // ここでいいのか・・・？
            if ((Find.MainTabsRoot.OpenTab?.TabWindow as MainTabWindow_Architect)
                ?.selectedDesPanel?.def.defName == "Industrial")
                OverlayDrawHandler_UGConveyor.DrawOverlayThisFrame();
            /*            Option(Find.MainTabsRoot.OpenTab)
                .Select(r => r.TabWindow)
                .SelectMany(w => Option(w as MainTabWindow_Architect))
                .SelectMany(a => Option(a.selectedDesPanel))
                .Where(p => p.def.defName == "Industrial")
                .ForEach(p => OverlayDrawHandler_UGConveyor.DrawOverlayThisFrame());
*/
            if (Find.Selector.FirstSelectedObject is IBeltConveyorLinkable)
                OverlayDrawHandler_UGConveyor.DrawOverlayThisFrame();
        }

        public void AfterAction(int ticks, Action act)
        {
            if (ticks < 1)
                ticks = 1;

            if (!tickActionsDict.TryGetValue(Find.TickManager.TicksGame + ticks, out var set))
            {
                set = new HashSet<Action>();
                tickActionsDict[Find.TickManager.TicksGame + ticks] = set;
            }

            set.Add(act);
        }

        public void NextAction(Action act)
        {
            AfterAction(1, act);
        }

        public void EachTickAction(Func<bool> act)
        {
            eachTickActions.Add(act);
        }

        public void RemoveAfterAction(Action act)
        {
            tickActionsDict.ForEach(kv => kv.Value.Remove(act));
        }

        public void RemoveEachTickAction(Func<bool> act)
        {
            eachTickActions.Remove(act);
        }

        public bool IsExecutingThisTick(Action act)
        {
            return tickActionsDict.GetOption(Find.TickManager.TicksGame).Select(l => l.Contains(act))
                .GetOrDefault(false);
        }

        //      private ThingLister thingsList;

//        public ThingLister ThingsList => thingsList;

#if DEBUG
        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            if (Widgets.ButtonText(new Rect(200, 10, 150, 20), "Test(debug)"))
            {
                map.wealthWatcher.ForceRecount();
                L("wealth items : " + map.wealthWatcher.WealthItems);
            }
        }
#endif
    }

    public class ThingLister
    {
        private readonly Map map;

        private readonly Dictionary<Type, List<ThingDef>> typeDic = new Dictionary<Type, List<ThingDef>>();

        public ThingLister(Map map)
        {
            this.map = map;
        }

        public IEnumerable<T> ForAssignableFrom<T>() where T : Thing
        {
            if (!typeDic.TryGetValue(typeof(T), out var defs))
            {
                defs = DefDatabase<ThingDef>.AllDefs.Where(d => typeof(T).IsAssignableFrom(d.thingClass)).ToList();
                typeDic[typeof(T)] = defs;
#if DEBUG
                L("ThingLister type : " + typeof(T) + " / defs count : " + defs.Count);
#endif
            }

            return defs.SelectMany(d => map.listerThings.ThingsOfDef(d)).SelectMany(t => Option(t as T));
        }
    }
}