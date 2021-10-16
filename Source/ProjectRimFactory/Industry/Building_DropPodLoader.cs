using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;
using ProjectRimFactory.Common;
using UnityEngine;


namespace ProjectRimFactory.Industry
{
 



    public class Building_DropPodLoader : Building
    {

        public virtual IEnumerable<IntVec3> IngredientStackCells => this.GetComp<CompPowerWorkSetting>()?.GetRangeCells().Where(c => c.InBounds(this.Map)) ?? GenAdj.CellsAdjacent8Way(this).Where(c => c.InBounds(this.Map));
        protected IEnumerable<Thing> AllThingsInArea
        {
            get
            {
                foreach (var c in IngredientStackCells)
                {
                    foreach (var t in Map.thingGrid.ThingsListAt(c))
                    {
                        if (t is Building && t is IThingHolder holder)
                        {
                            if (holder.GetDirectlyHeldThings() is ThingOwner<Thing> owner)
                            {
                                foreach (var moreT in owner.InnerListForReading) yield return moreT;
                            }
                        }
                        else if (t.def.category == ThingCategory.Item)
                        {
                            yield return t;
                        }
                    }
                }
                yield break;
            }
        }




        public IntVec3 FuelingPortPos => this.Position + this.Rotation.RighthandCell;
        public IntVec3 TransportPodPos => this.Position + this.Rotation.FacingCell;

        private CompLaunchable transportPod;

        private int destinationTile;
        private IntVec3 destinationCell;

        DropPodLoaderMode loaderMode = DropPodLoaderMode.Manual;


        public int DestinationTile { get => destinationTile; set  => destinationTile = value; }
        public IntVec3 DestinationCell { get => destinationCell; set => destinationCell = value; }
        public DropPodLoaderMode LoaderMode { get => loaderMode; set => loaderMode = value; }

        public enum DropPodLoaderMode
        {
            Filter,
            Manual,
            Logic
        }

        private bool autoLaunch = false;

     

        public void ChoosingDestination()
        {
            transportPod.StartChoosingDestination();    
        }
 


        //TODO Add Checks
        public void LaunchPod()
        {
            MapParent mapParent = Find.WorldObjects.MapParentAt(destinationTile);
            transportPod.TryLaunch(destinationTile, new TransportPodsArrivalAction_LandInSpecificCell(mapParent, destinationCell, landInShuttle: false));
        }

        private void LoadPodManual()
        {
            if (transportPod.AnythingLeftToLoad)
            {
                //TODO Fix that horrible Code
                Thing targetThing = null;
                foreach (TransferableOneWay transferableOneWay in transportPod.Transporter.leftToLoad)
                {
                    foreach(Thing thing in transferableOneWay.things)
                    {
                        if (AllThingsInArea.Contains(thing))
                        {
                            targetThing = thing;
                            goto foundThing;
                        }
                    }
                }
            foundThing:

                if (targetThing != null)
                {
                    transportPod.Transporter.innerContainer.TryAdd(targetThing.SplitOff(1));
                }
            }
        }


        public void LoadPod()
        {
            switch (loaderMode)
            {
                case DropPodLoaderMode.Filter:
                    break;
                case DropPodLoaderMode.Manual:
                    LoadPodManual();
                    break;
                case DropPodLoaderMode.Logic:
                    break;
                default:
                    break;
            }


        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            yield return new Command_Action
            {
                defaultLabel = "Launch",
                icon = RS.LaunchCommandTex,
                action = () => LaunchPod()
            };
        }

        public override string GetInspectString()
        {
            return base.GetInspectString();
        }

        public override void PostMake()
        {
            base.PostMake();
        }

        

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(10) && transportPod != null)
            {
                if (! transportPod.Transporter.AnythingLeftToLoad)
                {
                    //Ready to launch;
                    if (autoLaunch) LaunchPod();

                }
                else
                {
                    LoadPod();
                }
            }
            else if (this.IsHashIntervalTick(20) && transportPod == null)
            {
                //Check for transportPod
                foreach (ThingWithComps thing in this.Map.thingGrid.ThingsAt(TransportPodPos))
                {
                    CompLaunchable comp = thing.TryGetComp<CompLaunchable>();
                    if (comp != null)
                    {
                        transportPod = comp;
                        break;
                    }
                }
                

            }


        }
    }
}
