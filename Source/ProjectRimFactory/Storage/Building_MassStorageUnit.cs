﻿using ProjectRimFactory.Common;
using ProjectRimFactory.Common.HarmonyPatches;
using ProjectRimFactory.Storage.Editables;
using ProjectRimFactory.Storage.UI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage
{
    [StaticConstructorOnStartup]
    public abstract class Building_MassStorageUnit : Building_Storage, IHideItem, IHideRightClickMenu,
        IForbidPawnOutputItem, IForbidPawnInputItem, IRenameBuilding, ILinkableStorageParent, ILimitWatcher
    {
        private static readonly Texture2D RenameTex = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

        private readonly List<Thing> items = new List<Thing>();
        private List<Building_StorageUnitIOBase> ports = new List<Building_StorageUnitIOBase>();

        public string UniqueName { get => uniqueName; set => uniqueName = value; }
        private string uniqueName;
        public Building Building => this;
        public IntVec3 GetPosition => this.Position;
        public StorageSettings GetSettings => settings;

        public bool CanUseIOPort => def.GetModExtension<DefModExtension_CanUseStorageIOPorts>() != null;

        public LocalTargetInfo GetTargetInfo => this;

        //Initialized at spawn
        public DefModExtension_Crate ModExtension_Crate = null;

        public abstract bool CanStoreMoreItems { get; }
        // The maximum number of item stacks at this.Position:
        //   One item on each cell and the rest multi-stacked on Position?
        public virtual int MaxNumberItemsInternal => (ModExtension_Crate?.limit ?? int.MaxValue)
                                              - def.Size.Area + 1;
        public List<Thing> StoredItems => items;
        public int StoredItemsCount => items.Count;
        public override string LabelNoCount => uniqueName ?? base.LabelNoCount;
        public override string LabelCap => uniqueName ?? base.LabelCap;
        public virtual bool CanReceiveIO => true;
        public virtual bool Powered => true;

        public bool ForbidPawnAccess => ModExtension_Crate?.forbidPawnAccess ?? false;

        public virtual bool ForbidPawnInput => ForbidPawnAccess;

        private StorageOutputUtil outputUtil = null;

        public override void Notify_ReceivedThing(Thing newItem)
        {
            base.Notify_ReceivedThing(newItem);
            if (newItem.Position != Position) HandleNewItem(newItem);
            RefreshStorage();
        }

        public override void Notify_LostThing(Thing newItem)
        {
            base.Notify_LostThing(newItem);
            items.Remove(newItem);
            ItemCountsRemoved(newItem.def, newItem.stackCount);
            RefreshStorage();
        }

        public virtual bool ForbidPawnOutput => ForbidPawnAccess;

        public virtual bool HideItems => ModExtension_Crate?.hideItems ?? false;

        public virtual bool HideRightClickMenus =>
            ModExtension_Crate?.hideRightClickMenus ?? false;

        public bool AdvancedIOAllowed => true;

        public void DeregisterPort(Building_StorageUnitIOBase port)
        {
            ports.Remove(port);
        }

        public void RegisterPort(Building_StorageUnitIOBase port)
        {
            ports.Add(port);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;
            yield return new Command_Action
            {
                icon = RenameTex,
                action = () => Find.WindowStack.Add(new Dialog_RenameMassStorageUnit(this)),
                hotKey = KeyBindingDefOf.Misc1,
                defaultLabel = "PRFRenameMassStorageUnitLabel".Translate(),
                defaultDesc = "PRFRenameMassStorageUnitDesc".Translate()
            };
            yield return new Command_Action
            {
                icon = TexUI.RotRightTex,
                action = () =>
                {
                    RefreshStorage();
                    Messages.Message("PRFReorganize_Message".Translate(), MessageTypeDefOf.NeutralEvent);
                },
                defaultLabel = "PRFReorganize".Translate(),
                defaultDesc = "PRFReorganize_Desc".Translate()
            };
        }

        public virtual string GetUIThingLabel()
        {
            return "PRFMassStorageUIThingLabel".Translate(StoredItemsCount);
        }

        public virtual string GetITabString(int itemsSelected)
        {
            return "PRFItemsTabLabel".Translate(StoredItemsCount, itemsSelected);
        }

        public virtual void RegisterNewItem(Thing newItem)
        {
            var things = Position.GetThingList(Map);
            for (var i = 0; i < things.Count; i++)
            {
                var item = things[i];
                if (item == newItem) continue;
                if (item.def.category == ThingCategory.Item && item.CanStackWith(newItem))
                    item.TryAbsorbStack(newItem, true);
                if (newItem.Destroyed) break;
            }

            //Add a new stack of a thing
            if (!newItem.Destroyed)
            {
                if (!items.Contains(newItem))
                {
                    items.Add(newItem);
                    ItemCountsAdded(newItem.def, newItem.stackCount);
                }

                //What appens if its full?
                if (CanStoreMoreItems) newItem.Position = Position;
                if (!newItem.Spawned) newItem.SpawnSetup(Map, false);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref ports, "ports", LookMode.Reference);
            Scribe_Values.Look(ref uniqueName, "uniqueName");
            ModExtension_Crate ??= def.GetModExtension<DefModExtension_Crate>();
        }

        public override string GetInspectString()
        {
            var original = base.GetInspectString();
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(original)) stringBuilder.AppendLine(original);
            stringBuilder.Append("PRF_TotalStacksNum".Translate(items.Count));
            return stringBuilder.ToString();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var thingsToSplurge = new List<Thing>(Position.GetThingList(Map));
            for (var i = 0; i < thingsToSplurge.Count; i++)
                if (thingsToSplurge[i].def.category == ThingCategory.Item)
                {
                    thingsToSplurge[i].DeSpawn();
                    GenPlace.TryPlaceThing(thingsToSplurge[i], Position, Map, ThingPlaceMode.Near);
                }
            Map.GetComponent<PRFMapComponent>().RemoveIHideRightClickMenu(this);
            foreach (var cell in this.OccupiedRect().Cells)
            {
                Map.GetComponent<PRFMapComponent>().DeRegisterIHideItemPos(cell, this);
                Map.GetComponent<PRFMapComponent>().DeRegisterIForbidPawnOutputItem(cell, this);
            }
            base.DeSpawn(mode);
            ConditionalPatchHelper.Deregister(this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            ConditionalPatchHelper.Register(this);
            base.SpawnSetup(map, respawningAfterLoad);
            Map.GetComponent<PRFMapComponent>().AddIHideRightClickMenu(this);
            outputUtil = new StorageOutputUtil(this);
            foreach (var cell in this.OccupiedRect().Cells)
            {
                map.GetComponent<PRFMapComponent>().RegisterIHideItemPos(cell, this);
                map.GetComponent<PRFMapComponent>().RegisterIForbidPawnOutputItem(cell, this);
            }
            ModExtension_Crate ??= def.GetModExtension<DefModExtension_Crate>();
            RefreshStorage();
            foreach (var port in ports)
            {
                if (port?.Spawned ?? false)
                {
                    if (port.Map != map)
                    {
                        port.BoundStorageUnit = null;
                    }
                }
            }

        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (Current.CameraDriver.CurrentZoom <= CameraZoomRange.Close)
                GenMapUI.DrawThingLabel(this, LabelCap + "\n\r" + GetUIThingLabel());
        }

        public bool OutputItem(Thing item)
        {
            return outputUtil.OutputItem(item);
        }


        //TODO Why do we need to clear Items here?
        public virtual void RefreshStorage()
        {
            items.Clear();
            ItemCounts.Clear();

            if (!Spawned) return; // don't want to try getting lists of things when not on a map (see 155)
            foreach (var cell in AllSlotCells())
            {
                var things = new List<Thing>(cell.GetThingList(Map));
                for (var i = 0; i < things.Count; i++)
                {
                    var item = things[i];
                    if (item.def.category == ThingCategory.Item)
                    {
                        if (cell != Position)
                        {
                            HandleNewItem(item);
                        }
                        else
                        {
                            if (!items.Contains(item))
                            {
                                items.Add(item);
                                ItemCountsAdded(item.def, item.stackCount);
                                deregisterDrawItem(item);
                            }
                        }
                    }
                }
            }

            StackIncompleate();


            // Even though notifying I/O ports that the contents inside the storage unit have changed seems like a good idea, it can cause recursion issues.
            //for (int i = 0; i < ports.Count; i++)
            //{
            //    if (ports[i] == null)
            //    {
            //        ports.RemoveAt(i);
            //        i--;
            //    }
            //    else
            //    {
            //        ports[i].Notify_NeedRefresh();
            //    }
            //}
        }


        private void StackIncompleate()
        {
            // Only run the Stack attempts if the Storage is full
            // as that is the only point where #748 occurs
            int itemsCnt = items.Count;
            if (MaxNumberItemsInternal >= itemsCnt)
            {
                Dictionary<ThingDef, List<Thing>> incompleteStacks = new Dictionary<ThingDef, List<Thing>>();
                
                for (int i = 0; i < itemsCnt; i++)
                {
                    Thing item = items[i];
                    ThingDef def = item.def;
                    if (item.stackCount < def.stackLimit)
                    {
                        // Incompleate Stack
                        incompleteStacks.TryAdd(def, new List<Thing>());
                        incompleteStacks[def].Add(item);

                    }
                }

                //Now Stack them Up
                var vals = incompleteStacks.Values.ToList();
                int incompleteStacksCnt = incompleteStacks.Count;
                for (int i = 0; i < incompleteStacksCnt; i++)
                {
                    var incompleates = vals[i];
                    int limit = 20; // Prevent an infinit loop in case we can't stack some
                    while (incompleates.Count >= 2 && limit > 0)
                    {
                        limit--;
                        var absorber = incompleates[0];
                        var absorbee = incompleates[1];
                        absorber.TryAbsorbStack(absorbee, true);
                        
                        if (absorbee.stackCount == 0) incompleates.Remove(absorbee);
                        if (absorber.stackCount == absorber.def.stackLimit) incompleates.Remove(absorber);

                    }
                }
            }
        }


        //-----------    For compatibility with Pick Up And Haul:    -----------
        //                  (not used internally in any way)
        // true if can store, capacity is how many can store (more than one stack possible)
        public bool CapacityAt(Thing thing, IntVec3 cell, Map map, out int capacity)
        {
            //Some Sanity Checks
            capacity = 0;
            if (thing == null || map == null || map != this.Map || cell == null || !this.Spawned)
            {
                Log.Error("PRF DSU CapacityAt Sanity Check Error");
                return false;
            }
            thing = thing.GetInnerIfMinified();

            //Check if thing can be stored based upon the storgae settings
            if (!this.Accepts(thing))
            {
                return false;
            }

            //TODO Check if we want to forbid access if power is off
            //if (!GetComp<CompPowerTrader>().PowerOn) return false;

            //Get List of items stored in the DSU
            var storedItems = Position.GetThingList(Map).Where(t => t.def.category == ThingCategory.Item);

            //Find the Stack size for the thing
            int maxstacksize = thing.def.stackLimit;
            //Get capacity of partial Stacks
            //  So 45 Steel and 75 Steel and 11 Steel give 30+64 more capacity for steel
            foreach (Thing partialStack in storedItems.Where(t => t.def == thing.def && t.stackCount < maxstacksize))
            {
                capacity += maxstacksize - partialStack.stackCount;
            }

            //capacity of empy slots
            capacity += (MaxNumberItemsInternal - storedItems.Count()) * maxstacksize;

            //Access point:
            if (cell != Position)
            {
                var maybeThing = Map.thingGrid.ThingAt(cell, ThingCategory.Item);
                if (maybeThing != null)
                {
                    if (maybeThing.def == thing.def) capacity += (thing.def.stackLimit - maybeThing.stackCount);
                }
                else
                {
                    capacity += thing.def.stackLimit;
                }
            }
            return capacity > 0;
        }
        // ...The above? I think?  But without needing to know how many
        public bool StackableAt(Thing thing, IntVec3 cell, Map map)
        {
            return CapacityAt(thing, cell, map, out _);
        }

        public void HandleNewItem(Thing item)
        {
            RegisterNewItem(item);
            deregisterDrawItem(item);
        }

        private void deregisterDrawItem(Thing item)
        {
            Map.dynamicDrawManager.DeRegisterDrawable(item);
        }

        public void HandleMoveItem(Thing item)
        {
            //throw new System.NotImplementedException();
        }

        public bool CanReciveThing(Thing item)
        {
            return settings.AllowedToAccept(item) && CanReceiveIO && CanStoreMoreItems;
        }

        public bool HoldsPos(IntVec3 pos)
        {
            return AllSlotCells()?.Contains(pos) ?? false;
        }

        void ItemCountsRemoved(ThingDef def , int cnt)
        {
            if(ItemCounts.TryGetValue(def, out var count))
            {
                if (cnt > count)
                {
                    Log.Error($"ItemCountsRemoved attempted to remove {cnt}/{count} Items of {def}");
                    ItemCounts[def] = 0;
                }
                ItemCounts[def] -= cnt;

            }
            else
            {
                Log.Error($"ItemCountsRemoved attempted to remove nonexistent def {def}");
            }

        }
        void ItemCountsAdded(ThingDef def , int cnt)
        {
            if (!ItemCounts.TryAdd(def, cnt))
            {
                ItemCounts[def] += cnt;
            }
        }

        readonly Dictionary<ThingDef, int> ItemCounts = new();

        public bool ItemIsLimit(ThingDef thing,bool CntStacks, int limit)
        {
            if (limit < 0)
            {
                return true;
            }

            int cnt = 0;
            ItemCounts.TryGetValue(thing, out cnt);
            if (CntStacks)
            {
                cnt = Mathf.CeilToInt(((float)cnt) / thing.stackLimit);
            }
            
            return cnt >= limit;
        }
    }
}