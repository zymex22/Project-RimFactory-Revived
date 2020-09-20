using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones
{

    //This is basicly a clone of Area_Allowed it was created since the former one is limited to 10 in vanilla RimWorld
    public class DroneArea : Area
    {
        private string labelInt;

        public DroneArea()
        {
        }

        public DroneArea(AreaManager areaManager, string label = null) : base(areaManager)
        {
            base.areaManager = areaManager;
            if (!label.NullOrEmpty())
            {
                labelInt = label;
            }
            else
            {
                int num = 1;
                while (true)
                {
                    labelInt = "AreaDefaultLabel".Translate(num);
                    if (areaManager.GetLabeled(labelInt) == null)
                    {
                        break;
                    }
                    num++;
                }
            }
            colorInt = new Color(Rand.Value, Rand.Value, Rand.Value);
            colorInt = Color.Lerp(colorInt, Color.gray, 0.5f);
        }


        private Color colorInt = Color.red;

        private string LabelText = "DroneZone";

        public override string Label => LabelText;

        public override Color Color => colorInt;

        public override int ListPriority => 3000;

        public override string GetUniqueLoadID()
        {
            return "Area_" + ID + "_DroneArea";
        }

        public override void ExposeData()
        {
            //IL_0025: Unknown result type (might be due to invalid IL or missing references)
            //IL_002b: Unknown result type (might be due to invalid IL or missing references)
            base.ExposeData();
            Scribe_Values.Look(ref labelInt, "label");
            Scribe_Values.Look(ref colorInt, "color");
        }

    }


    //This Class is used for the Area Selection for Drones where the range is unlimeted (0)
    public class DroneAreaSelector : Designator
    {
        //Content is mostly a copy of Designator_AreaAllowedExpand

        private static Area selectedArea;

        public Action<Area> selectAction;

        public static Area SelectedArea => selectedArea;

        
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return loc.InBounds(base.Map) && Designator_AreaAllowed.SelectedArea != null && !Designator_AreaAllowed.SelectedArea[loc];
            //throw new NotImplementedException();
        }
        public override void SelectedUpdate()
        {
        //    Log.Message("SelectedUpdate");
        }

        public override void ProcessInput(Event ev)
        {
            if (CheckCanInteract())
            {
                if (selectedArea != null)
                {
                    base.ProcessInput(ev);
                }
                AreaUtility.MakeAllowedAreaListFloatMenu(delegate (Area a)
                {
                    selectedArea = a;
                    base.ProcessInput(ev);

                    /*
                    selectedArea == null --> Unrestricted
                    selectedArea != null --> User Area
                     */
                    selectAction(selectedArea);

                }, addNullAreaOption: true, addManageOption: false, base.Map);
            }
        }
        //public static void ClearSelectedArea()
        //{
        //    selectedArea = null;
        //}
        //protected override void FinalizeDesignationSucceeded()
        //{
        //    base.FinalizeDesignationSucceeded();
        //    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.AllowedAreas, KnowledgeAmount.SpecificInteraction);
        //}
    }



    [StaticConstructorOnStartup]
    public abstract class Building_DroneStation : Building , IPowerSupplyMachineHolder
    {
        //Sleep Time List (Loaded on Spawn)
        public string[] cachedSleepTimeList;

        //Return the Range depending on the Active Defenition
        public int DroneRange
        {
            get
            {
                if (this.GetComp<CompPowerWorkSetting>() != null) {
                    return (int)Math.Ceiling(this.GetComp<CompPowerWorkSetting>().GetRange());
                }
                else {
                    return def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius;
                }
            }

        }

        public IEnumerable<IntVec3> StationRangecells
        {
            get
            {
                return GenAdj.OccupiedRect(this).ExpandedBy(DroneRange).Cells;
            }
        }

        public List<IntVec3> cashed_GetCoverageCells = null;

        //droneAllowedArea Loaded on Spawn | this is ithe zone where the DronePawns are allowed to move in
        //This needs to be "Area" as one can cast "DroneArea" to "Area" but not the other way around
        //That feature is needed to assign vanilla Allowed Areas
        //Please note that for Area Null is a valid Value. it stands for unrestricted
        public Area droneAllowedArea = null;

        public DroneArea GetDroneAllowedArea
        {
            get
            {
                DroneArea droneArea = null;
                if (DroneRange > 0)
                {
                    droneArea = new DroneArea(this.Map.areaManager);
                    //Need to set the Area to a size


                    foreach (IntVec3 cell in StationRangecells)
                    {
                        droneArea[cell] = true;
                    }
                    //Not shure if i need that but just to be shure
                    droneArea[Position] = true;
                    this.Map.areaManager.AllAreas.Add(droneArea);
                }
                

                return droneArea;

            }
        }

        //This function can be used to Update the Allowed area for all Drones (Active and future)
        //Just need to auto call tha on Change from CompPowerWorkSetting
        public void Update_droneAllowedArea_forDrones(Area dr = null)
        {
            //Refresh the area
            droneAllowedArea = dr ?? (Area)GetDroneAllowedArea;
            foreach (Pawn_Drone sdrone in spawnedDrones)
            {
                sdrone.playerSettings.AreaRestriction = droneAllowedArea;    
            }
        }

        public static readonly Texture2D Cancel = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
        protected bool lockdown;
        private string droneAreaSelectorLable = "Unrestricted\nSelect Area";
        protected DefModExtension_DroneStation extension;
        protected List<Pawn_Drone> spawnedDrones = new List<Pawn_Drone>();

        public abstract int DronesLeft { get; }

        public IPowerSupplyMachine RangePowerSupplyMachine => this.GetComp<CompPowerWorkSetting>();

        private float LastPowerOutput = 0;

        // Used for destroyed pawns
        public abstract void Notify_DroneLost();
        // Used to negate imaginary pawns despawned in WorkGiverDroneStations and JobDriver_ReturnToStation
        public abstract void Notify_DroneGained();

        public override void PostMake()
        {
            base.PostMake();
            extension = def.GetModExtension<DefModExtension_DroneStation>();
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            extension = def.GetModExtension<DefModExtension_DroneStation>();
            //Setup Allowd Area
            if (droneAllowedArea == null) {
                //Log.Message("droneAllowedArea was null");
                Update_droneAllowedArea_forDrones();
            }
            //Load the SleepTimes from XML
            cachedSleepTimeList = extension.Sleeptimes.Split(',');
            LastPowerOutput = GetComp<CompPowerTrader>().powerOutputInt;
            cashed_GetCoverageCells = StationRangecells.ToList();
        }
        public override void Draw()
        {
            base.Draw();
            if (extension.displayDormantDrones)
            {
                DrawDormantDrones();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn();
            List<Pawn_Drone> drones = spawnedDrones.ToList();
            for (int i = 0; i < drones.Count; i++)
            {
                drones[i].Destroy();
            }
        }

        public virtual void DrawDormantDrones()
        {
            foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(this).Take(DronesLeft))
            {
                PRFDefOf.PRFDrone.graphic.DrawFromDef(cell.ToVector3ShiftedWithAltitude(AltitudeLayer.LayingPawn), default(Rot4), PRFDefOf.PRFDrone);
            }
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (lockdown)
            {
                Map.overlayDrawer.DrawOverlay(this, OverlayTypes.ForbiddenBig);
            }
        }

        public abstract Job TryGiveJob();

        public override void Tick()
        {
            base.Tick();
            if (DronesLeft > 0 && !lockdown && this.IsHashIntervalTick(60) && GetComp<CompPowerTrader>()?.PowerOn != false)
            {
                Job job = TryGiveJob();
                if (job != null)
                {
                    job.playerForced = true;
                    job.expiryInterval = -1;
                    Pawn_Drone drone = MakeDrone();
                    GenSpawn.Spawn(drone, Position, Map);
                    drone.jobs.StartJob(job);
                }
            }
            //TODO Check if we should increase the IsHashIntervalTick to enhace performence (will reduce responsivness)
            if (this.IsHashIntervalTick(60) && GetComp<CompPowerTrader>().powerOutputInt != LastPowerOutput)
            {
                //Update the Range
                Update_droneAllowedArea_forDrones();
                //Update the last know Val
                LastPowerOutput = GetComp<CompPowerTrader>().powerOutputInt;

                //TODO add cell calc
                cashed_GetCoverageCells = StationRangecells.ToList();
            }
        }

        public void Notify_DroneMayBeLost(Pawn_Drone drone)
        {
            if (spawnedDrones.Contains(drone))
            {
                spawnedDrones.Remove(drone);
                Notify_DroneLost();
            }
        }

        //Handel the Range UI
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            //Dont Draw if infinite
            if (def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius > 0) { 
                GenDraw.DrawFieldEdges(cashed_GetCoverageCells);
            }
            
        }

        public override string GetInspectString()
        {
            StringBuilder builder = new StringBuilder();
            string str = base.GetInspectString();
            if (!string.IsNullOrEmpty(str))
            {
                builder.AppendLine(str);
            }
            builder.Append("PRFDroneStation_NumberOfDrones".Translate(DronesLeft));
            return builder.ToString();
        }

        public virtual Pawn_Drone MakeDrone()
        {
            Pawn_Drone drone = (Pawn_Drone)PawnGenerator.GeneratePawn(PRFDefOf.PRFDroneKind, Faction);
            drone.station = this;
            spawnedDrones.Add(drone);
            return drone;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref spawnedDrones, "spawnedDrones", LookMode.Reference);
            Scribe_Values.Look(ref lockdown, "lockdown");
            Scribe_References.Look(ref droneAllowedArea, "droneAllowedArea");
        }


        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
                yield return g;
            yield return new Command_Toggle()
            {
                defaultLabel = "PRFDroneStationLockdown".Translate(),
                defaultDesc = "PRFDroneStationLockdownDesc".Translate(),
                toggleAction = () =>
                {
                    lockdown = !lockdown;
                    if (lockdown)
                    {
                        foreach (Pawn_Drone drone in spawnedDrones.ToList())
                        {
                            drone.jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, this), JobCondition.InterruptForced);
                        }
                    }
                },
                isActive = () => lockdown,
                icon = Cancel
            };
            yield return new Command_Action()
            {
                defaultLabel = "PRFDroneStationLockdownAll".Translate(),
                defaultDesc = "PRFDroneStationLockdownAllDesc".Translate(),
                action = () =>
                {
                    List<Building_DroneStation> buildings = Map.listerThings.AllThings.OfType<Building_DroneStation>().ToList();
                    Log.Message("Count" + buildings.Count);
                    for (int i = 0; i< buildings.Count;i++)
                    {
                        buildings[i].lockdown = true;
                        foreach (Pawn_Drone drone in buildings[i].spawnedDrones.ToList())
                        {
                            drone.jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, buildings[i]), JobCondition.InterruptForced);
                        }
                    }

                },
                icon = Cancel
            };
            if (DroneRange == 0)
            {
                /*
                "Verse.Designator"
                Holds example of how i want this Gizmo Implemented
                */
                yield return new DroneAreaSelector()
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/AreaAllowedExpand"),
                    defaultLabel = droneAreaSelectorLable,
                    selectAction = (a) =>
                    {
                        Update_droneAllowedArea_forDrones(a);
                        if (a == null)
                        {
                            droneAreaSelectorLable = "Unrestricted\nSelect Area";
                        }
                        else
                        {
                            droneAreaSelectorLable =  a.Label + "\nSelect Area";
                        }

                    }
                };


            }
            
        }
    }
}
