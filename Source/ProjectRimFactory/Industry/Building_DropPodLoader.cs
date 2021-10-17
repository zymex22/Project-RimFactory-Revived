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

        //Vanilly only. SRTS & SOS2 use diffrent comps
        private CompLaunchable transportPod;
        //MethodInfo to be used instead of CompLaunchable for modded content
        private System.Reflection.MethodInfo methodInfo_TryLaunch;
        private ThingComp instance_TryLaunch;

        //Seems to be universal in use
        private CompTransporter transporterComp;


        private int destinationTile;
        private IntVec3 destinationCell;

        DropPodLoaderMode loaderMode = DropPodLoaderMode.Manual;

        DropPodType podType;



        public int DestinationTile { get => destinationTile; set  => destinationTile = value; }
        public IntVec3 DestinationCell { get => destinationCell; set => destinationCell = value; }
        public DropPodLoaderMode LoaderMode { get => loaderMode; set => loaderMode = value; }

        public enum DropPodLoaderMode
        {
            Filter,
            Manual,
            Logic
        }

        public enum DropPodType
        {
            Core,
            SRTS,
            SOS2
        }

        private bool autoLaunch = false;

     

        /// <summary>
        /// TODO: Update for SRTS & SOS2 Support
        /// </summary>
        public void ChoosingDestination()
        {
            transportPod.StartChoosingDestination();    
        }
 


        //TODO Add Checks
        public void LaunchPod()
        {
            MapParent mapParent = Find.WorldObjects.MapParentAt(destinationTile);
            if (transportPod != null)
            {
                transportPod.TryLaunch(destinationTile, new TransportPodsArrivalAction_LandInSpecificCell(mapParent, destinationCell, landInShuttle: false));
            }
            else
            {
                if (methodInfo_TryLaunch is null)
                {
                    Log.Error("PRF - Building_DropPodLoader Fatal error methodInfo_TryLaunch && transportPod is null");
                    return;
                }
                methodInfo_TryLaunch.Invoke(instance_TryLaunch, new object[] { destinationTile, new TransportPodsArrivalAction_LandInSpecificCell(mapParent, destinationCell, landInShuttle: false)});
                var a = podType == 0;


            }
            
        }

        private void LoadPodManual()
        {
            if (transporterComp.AnythingLeftToLoad)
            {
                //TODO Fix that horrible Code
                Thing targetThing = null;
                foreach (TransferableOneWay transferableOneWay in transporterComp.leftToLoad)
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
                    transporterComp.innerContainer.TryAdd(targetThing.SplitOff(1));
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

        
        private void findTransportPod()
        {
            
            foreach (ThingWithComps thing in this.Map.thingGrid.ThingsAt(TransportPodPos))
            {
                //Find Vanilla Transport Port
                CompLaunchable comp = thing.TryGetComp<CompLaunchable>();
                if (comp != null)
                {
                    transportPod = comp;
                    transporterComp = comp.Transporter;
                    podType = DropPodType.Core;
                    break;
                }

                //Find Comps for Modded Content
                CompTransporter compT = thing.TryGetComp<CompTransporter>();
                if (compT != null)
                {
                    transporterComp = compT;

                    //Now find the Corrosponding Comp used for Launching
                    instance_TryLaunch = thing.AllComps.Where(c => c.GetType().ToString() == "SRTS.CompLaunchableSRTS" || c.GetType().ToString() == "RimWorld.CompShuttleLaunchable").FirstOrDefault();
                    
                    
                    if (instance_TryLaunch is null)
                    {
                        Log.Error("PRF - Building_DropPodLoader Fatal Error instance_TryLaunch is null");
                        return;
                    }else if (instance_TryLaunch.GetType().ToString() == "SRTS.CompLaunchableSRTS")
                    {
                        podType = DropPodType.SRTS;
                        methodInfo_TryLaunch = ProjectRimFactory.SAL3.ReflectionUtility.SRTS_TryLaunch;
                    }
                    else
                    {
                        podType = DropPodType.SOS2;
                        methodInfo_TryLaunch = ProjectRimFactory.SAL3.ReflectionUtility.SOS2_TryLaunch;

                    }
                }


            }

            //Find CompTransporter for Modded Content 
            
        }

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(10) && transporterComp != null)
            {
                if (! transporterComp.AnythingLeftToLoad)
                {
                    //Ready to launch;
                    if (autoLaunch) LaunchPod();

                }
                else
                {
                    LoadPod();
                }
            }
            else if (this.IsHashIntervalTick(20) && transporterComp == null)
            {
                //Check for transportPod
                findTransportPod();
            }


        }
    }
}
