using ProjectRimFactory.AutoMachineTool;
using System.Collections.Generic;
using ProjectRimFactory.SAL3.Things;
using Verse;

namespace ProjectRimFactory.Common
{
    public class PRFMapComponent : MapComponent
    {
        private List<ITicker> tickers = [];
        // iHideRightMenus: see HarmonyPatches/PatchStorage.cs
        public HashSet<IntVec3> iHideRightMenus = [];

        public readonly List<Storage.Building_ColdStorage> ColdStorageBuildings = [];

        private readonly Dictionary<IntVec3, List<HarmonyPatches.IHideItem>> hideItemLocations = new();

        private readonly Dictionary<IntVec3, List<HarmonyPatches.IForbidPawnOutputItem>> forbidPawnOutputItemLocations = new();

        public Dictionary<IntVec3, Storage.Building_AdvancedStorageUnitIOPort> GetAdvancedIOLocations { get; } = new();

        public readonly Dictionary<Building_BeltConveyor, IBeltConveyorLinkable> NextBeltCache = new();
        
        private readonly List<IRecipeSubscriber> recipeSubscribers = [];

        public void RegisterRecipeSubscriber(IRecipeSubscriber recipeSubscriber)
        {
            recipeSubscribers.Add(recipeSubscriber);
        }
        public void DeregisterRecipeSubscriber(IRecipeSubscriber recipeSubscriber)
        {
            recipeSubscribers.Remove(recipeSubscriber);
        }
        public void NotifyRecipeSubscriberOfProvider(IntVec3 pos, Building_RecipeHolder recipeHolder)
        {
            foreach (var subscriber in recipeSubscribers)
            {
                subscriber.RecipeProviderSpawnedAt(pos, recipeHolder);
            }
        }


        public void RegisterColdStorageBuilding(Storage.Building_ColdStorage port)
        {
            if (!ColdStorageBuildings.Contains(port))
            {
                ColdStorageBuildings.Add(port);
            }
        }
        public void DeRegisterColdStorageBuilding(Storage.Building_ColdStorage port)
        {
            if (ColdStorageBuildings.Contains(port))
            {
                ColdStorageBuildings.Remove(port);
            }

        }



        public void RegisteradvancedIOLocations(IntVec3 pos, Storage.Building_AdvancedStorageUnitIOPort port)
        {
            GetAdvancedIOLocations.TryAdd(pos, port);
        }
        public void DeRegisteradvancedIOLocations(IntVec3 pos)
        {
            if (GetAdvancedIOLocations.ContainsKey(pos))
            {
                GetAdvancedIOLocations.Remove(pos);
            }

        }

        public void RegisterIHideItemPos(IntVec3 pos, HarmonyPatches.IHideItem hideItem)
        {
            if (hideItemLocations.ContainsKey(pos))
            {
                hideItemLocations[pos].Add(hideItem);
            }
            else
            {
                hideItemLocations.Add(pos, [hideItem]);
            }
        }
        public void DeRegisterIHideItemPos(IntVec3 pos, HarmonyPatches.IHideItem hideItem)
        {
            if (hideItemLocations[pos].Count <= 1)
            {
                hideItemLocations.Remove(pos);
            }
            else
            {
                hideItemLocations[pos].Remove(hideItem);
            }

        }
        public void RegisterIForbidPawnOutputItem(IntVec3 pos, HarmonyPatches.IForbidPawnOutputItem forbidPawnOutput)
        {
            if (forbidPawnOutputItemLocations.ContainsKey(pos))
            {
                forbidPawnOutputItemLocations[pos].Add(forbidPawnOutput);
            }
            else
            {
                forbidPawnOutputItemLocations.Add(pos, [forbidPawnOutput]);
            }
        }
        public void DeRegisterIForbidPawnOutputItem(IntVec3 pos, HarmonyPatches.IForbidPawnOutputItem forbidPawnOutput)
        {
            if (forbidPawnOutputItemLocations[pos].Count <= 1)
            {
                forbidPawnOutputItemLocations.Remove(pos);
            }
            else
            {
                forbidPawnOutputItemLocations[pos].Remove(forbidPawnOutput);
            }

        }

        private List<HarmonyPatches.IHideItem> CheckIHideItemPos(IntVec3 pos)
        {
            return hideItemLocations.GetValueOrDefault(pos);
        }

        public bool ShouldHideItemsAtPos(IntVec3 pos)
        {
            return CheckIHideItemPos(pos)?.Any(t => t.HideItems) ?? false;
        }

        private List<HarmonyPatches.IForbidPawnOutputItem> CheckIForbidPawnOutputItem(IntVec3 pos)
        {
            return forbidPawnOutputItemLocations.GetValueOrDefault(pos);
        }

        public bool ShouldForbidPawnOutputAtPos(IntVec3 pos)
        {
            return CheckIForbidPawnOutputItem(pos)?.Any(t => t.ForbidPawnOutput) ?? false;
        }


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
        public void AddIHideRightClickMenu(Building b)
        {
            foreach (var v in b.OccupiedRect())
            {
                iHideRightMenus.Add(v);
            }
        }
        public void RemoveIHideRightClickMenu(Building b)
        {
            foreach (var v in b.OccupiedRect())
            {
                iHideRightMenus.Remove(v);
                foreach (var t in map.thingGrid.ThingsListAt(v))
                {
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
