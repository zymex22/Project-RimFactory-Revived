using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectRimFactory.AutoMachineTool;
using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.Drones
{
    //This is basicly a clone of Area_Allowed it was created since the former one is limited to 10 in vanilla RimWorld
    public class DroneArea : Area
    {
        private Color colorInt = Color.red;
        private string labelInt;

        public DroneArea()
        {
        }

        public DroneArea(AreaManager areaManager, string label = null) : base(areaManager)
        {
            this.areaManager = areaManager;
            if (!label.NullOrEmpty())
            {
                labelInt = label;
            }
            else
            {
                var num = 1;
                while (true)
                {
                    labelInt = "AreaDefaultLabel".Translate(num);
                    if (areaManager.GetLabeled(labelInt) == null) break;
                    num++;
                }
            }

            colorInt = new Color(Rand.Value, Rand.Value, Rand.Value);
            colorInt = Color.Lerp(colorInt, Color.gray, 0.5f);
        }

        public override string Label { get; } = "DroneZone";

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

        public Action<Area> selectAction;

        public static Area SelectedArea { get; private set; }


        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return loc.InBounds(Map) && Designator_AreaAllowed.SelectedArea != null &&
                   !Designator_AreaAllowed.SelectedArea[loc];
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
                if (SelectedArea != null)
                {
                    //base.ProcessInput(ev);
                }

                AreaUtility.MakeAllowedAreaListFloatMenu(delegate(Area a)
                {
                    SelectedArea = a;
                    // base.ProcessInput(ev);

                    /*
                    selectedArea == null --> Unrestricted
                    selectedArea != null --> User Area
                     */
                    selectAction(SelectedArea);
                }, true, false, Map);
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
    public abstract class Building_DroneStation : Building, IPowerSupplyMachineHolder, IDroneSeetingsITab,
        IPRF_SettingsContentLink
    {
        private const int defaultSkillLevel = 20;

        public static readonly Texture2D Cancel = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        protected int additionJobSearchTickDelay;

        //Sleep Time List (Loaded on Spawn)
        public string[] cachedSleepTimeList;

        public List<IntVec3> cashed_GetCoverageCells;


        protected CompPowerTrader compPowerTrader;

        //droneAllowedArea Loaded on Spawn | this is ithe zone where the DronePawns are allowed to move in
        //This needs to be "Area" as one can cast "DroneArea" to "Area" but not the other way around
        //That feature is needed to assign vanilla Allowed Areas
        //Please note that for Area Null is a valid Value. it stands for unrestricted
        public Area droneAllowedArea;
        private string droneAreaSelectorLable = "Unrestricted\nSelect Area";

        protected DefModExtension_DroneStation extension;

        private float LastPowerOutput;
        protected bool lockdown;

        protected CompRefuelable refuelableComp;
        protected List<Pawn_Drone> spawnedDrones = new List<Pawn_Drone>();

        public Dictionary<WorkTypeDef, bool> WorkSettings = new Dictionary<WorkTypeDef, bool>();

        public int GetdefaultSkillLevel => defaultSkillLevel;

        public List<SkillRecord> GetDroneSkillsRecord { get; set; } = new List<SkillRecord>();

        //Return the Range depending on the Active Defenition
        public int DroneRange
        {
            get
            {
                if (compPowerWorkSetting != null)
                    return (int) Math.Ceiling(compPowerWorkSetting.GetRange());
                return def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius;
            }
        }

        private CompPowerWorkSetting compPowerWorkSetting => GetComp<CompPowerWorkSetting>();

        public IEnumerable<IntVec3> StationRangecells
        {
            get
            {
                if (compPowerWorkSetting != null && compPowerWorkSetting.RangeSetting)
                    return compPowerWorkSetting.GetRangeCells();
                return this.OccupiedRect().ExpandedBy(DroneRange).Cells;
            }
        }

        public DroneArea GetDroneAllowedArea
        {
            get
            {
                DroneArea droneArea = null;
                if (DroneRange > 0)
                {
                    droneArea = new DroneArea(Map.areaManager);
                    //Need to set the Area to a size


                    foreach (var cell in StationRangecells) droneArea[cell] = true;
                    //Not shure if i need that but just to be shure
                    droneArea[Position] = true;
                    Map.areaManager.AllAreas.Add(droneArea);
                }


                return droneArea;
            }
        }

        public abstract int DronesLeft { get; }
        protected MapTickManager MapManager { get; private set; }

        public Dictionary<WorkTypeDef, bool> GetWorkSettings
        {
            get => WorkSettings;
            set => WorkSettings = value;
        }

        public List<SkillRecord> DroneSeetings_skillDefs => GetDroneSkillsRecord;

        public string[] GetSleepTimeList => cachedSleepTimeList;

        public CompRefuelable compRefuelable => GetComp<CompRefuelable>();

        public IPowerSupplyMachine RangePowerSupplyMachine => GetComp<CompPowerWorkSetting>();

        IPRF_SettingsContent IPRF_SettingsContentLink.PRF_SettingsContentOb => new ITab_DroneStation_Def(this);

        //This function can be used to Update the Allowed area for all Drones (Active and future)
        //Just need to auto call tha on Change from CompPowerWorkSetting
        public void Update_droneAllowedArea_forDrones(Area dr = null)
        {
            //Refresh the area
            droneAllowedArea = dr ?? GetDroneAllowedArea;
            for (var i = 0; i < spawnedDrones.Count; i++)
                spawnedDrones[i].playerSettings.AreaRestriction = droneAllowedArea;
        }

        public void UpdateDronePrioritys()
        {
            if (spawnedDrones.Count > 0)
                foreach (Pawn pawn in spawnedDrones)
                foreach (var def in WorkSettings.Keys)
                    if (WorkSettings[def])
                        pawn.workSettings.SetPriority(def, 3);
                    else
                        pawn.workSettings.SetPriority(def, 0);
        }

        // Used for destroyed pawns
        public abstract void Notify_DroneLost();

        // Used to negate imaginary pawns despawned in WorkGiverDroneStations and JobDriver_ReturnToStation
        public abstract void Notify_DroneGained();

        public override void PostMake()
        {
            base.PostMake();
            extension = def.GetModExtension<DefModExtension_DroneStation>();
            refuelableComp = GetComp<CompRefuelable>();
            compPowerTrader = GetComp<CompPowerTrader>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            MapManager = map.GetComponent<MapTickManager>();
            refuelableComp = GetComp<CompRefuelable>();
            compPowerTrader = GetComp<CompPowerTrader>();
            extension = def.GetModExtension<DefModExtension_DroneStation>();
            //Setup Allowd Area
            //Enssuring that Update_droneAllowedArea_forDrones() is run resolves #224 (May need to add a diffrent check)
            if (droneAllowedArea == null) //Log.Message("droneAllowedArea was null");
                Update_droneAllowedArea_forDrones();
            //Load the SleepTimes from XML
            cachedSleepTimeList = extension.Sleeptimes.Split(',');


            LastPowerOutput = compPowerTrader.powerOutputInt;
            cashed_GetCoverageCells = StationRangecells.ToList();

            //Check for missing WorkTypeDef
            foreach (var def in extension.workTypes.Except(WorkSettings.Keys).ToList()) WorkSettings.Add(def, true);
            //Remove stuff thats nolonger valid (can only happen after updates)
            foreach (var def in WorkSettings.Keys.Except(extension.workTypes).ToList()) WorkSettings.Remove(def);
            //need to take action to init droneSkillsRecord
            if (GetDroneSkillsRecord.Count == 0)
            {
                var drone = MakeDrone();
                GenSpawn.Spawn(drone, Position, Map);
                drone.Destroy();

                GetComp<CompRefuelable>()?.Refuel(1);
            }

            //Init the Designator default Label
            update_droneAreaSelectorLable(droneAllowedArea);

            //Need this type of call to set the Powerconsumption on load
            //A normal call will not work
            var rangePowerSupplyMachine = RangePowerSupplyMachine;
            if (rangePowerSupplyMachine != null)
            {
                MapManager.NextAction(rangePowerSupplyMachine.RefreshPowerStatus);
                MapManager.AfterAction(5, rangePowerSupplyMachine.RefreshPowerStatus);
            }
        }

        private void update_droneAreaSelectorLable(Area a)
        {
            if (a == null)
                droneAreaSelectorLable = "PRFDroneStationSelectArea".Translate("Unrestricted".Translate());
            else
                droneAreaSelectorLable = "PRFDroneStationSelectArea".Translate(a.Label);
        }

        public override void Draw()
        {
            base.Draw();
            if (extension.displayDormantDrones) DrawDormantDrones();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn();
            var drones = spawnedDrones.ToList();
            for (var i = 0; i < drones.Count; i++) drones[i].Destroy();
            droneAllowedArea = null;
        }

        public virtual void DrawDormantDrones()
        {
            foreach (var cell in GenAdj.CellsOccupiedBy(this).Take(DronesLeft))
                PRFDefOf.PRFDrone.graphic.DrawFromDef(cell.ToVector3ShiftedWithAltitude(AltitudeLayer.LayingPawn),
                    default, PRFDefOf.PRFDrone);
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (lockdown) Map.overlayDrawer.DrawOverlay(this, OverlayTypes.ForbiddenBig);
        }

        public abstract Job TryGiveJob();


        public override void Tick()
        {
            //Base Tick
            base.Tick();
            //Return if not Spawnd
            if (!Spawned) return;


            //Should not draw much performence...
            //To enhance performence we could add "this.IsHashIntervalTick(60)"
            if (spawnedDrones.Count > 0 && compPowerTrader?.PowerOn == false)
            {
                for (var i = spawnedDrones.Count - 1; i >= 0; i--)
                    spawnedDrones[i].jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, this),
                        JobCondition.InterruptForced);
                //return as there is nothing to do if its off....
                return;
            }


            //Update the Allowed Area Range on Power Change
            //TODO Check if we should increase the IsHashIntervalTick to enhace performence (will reduce responsivness)
            if (this.IsHashIntervalTick(60) && compPowerTrader.powerOutputInt != LastPowerOutput)
            {
                //Update the Range
                Update_droneAllowedArea_forDrones();
                //Update the last know Val
                LastPowerOutput = compPowerTrader.powerOutputInt;

                //TODO add cell calc
                cashed_GetCoverageCells = StationRangecells.ToList();
            }

            //Search for Job
            if (this.IsHashIntervalTick(60 + additionJobSearchTickDelay) && DronesLeft > 0 && !lockdown)
            {
                //The issue appears to be 100% with TryGiveJob
                var job = TryGiveJob();

                if (job != null)
                {
                    additionJobSearchTickDelay = 0; //Reset to 0 - found a job -> may find more
                    job.playerForced = true;
                    job.expiryInterval = -1;
                    //MakeDrone takes about 1ms
                    var drone = MakeDrone();
                    GenSpawn.Spawn(drone, Position, Map);
                    drone.jobs.StartJob(job);
                }
                else
                {
                    //Experimental Delay
                    //Add delay (limit to 300) i am well aware that this can be higher that 300 with the current code
                    if (additionJobSearchTickDelay < 300)
                        //Exponential delay
                        additionJobSearchTickDelay = (additionJobSearchTickDelay + 1) * 2;
                }
            }
        }
        //It appers as if TickLong & TickRare are not getting called here

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
            if (def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius > 0)
                GenDraw.DrawFieldEdges(cashed_GetCoverageCells);
        }

        public override string GetInspectString()
        {
            var builder = new StringBuilder();
            var str = base.GetInspectString();
            if (!string.IsNullOrEmpty(str)) builder.AppendLine(str);
            builder.Append("PRFDroneStation_NumberOfDrones".Translate(DronesLeft));
            return builder.ToString();
        }

        public virtual Pawn_Drone MakeDrone()
        {
            var drone = (Pawn_Drone) PawnGenerator.GeneratePawn(PRFDefOf.PRFDroneKind, Faction);
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
            //WorkSettings
            Scribe_Collections.Look(ref WorkSettings, "WorkSettings");
            if (WorkSettings == null) //Need for Compatibility with older saves
                WorkSettings = new Dictionary<WorkTypeDef, bool>();
            //init refuelableComp after a Load
            if (refuelableComp == null) refuelableComp = GetComp<CompRefuelable>();
        }


        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;
            yield return new Command_Toggle
            {
                defaultLabel = "PRFDroneStationLockdown".Translate(),
                defaultDesc = "PRFDroneStationLockdownDesc".Translate(),
                toggleAction = () =>
                {
                    lockdown = !lockdown;
                    if (lockdown)
                        foreach (var drone in spawnedDrones.ToList())
                            drone.jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, this),
                                JobCondition.InterruptForced);
                },
                isActive = () => lockdown,
                icon = Cancel
            };
            yield return new Command_Action
            {
                defaultLabel = "PRFDroneStationLockdownAll".Translate(),
                defaultDesc = "PRFDroneStationLockdownAllDesc".Translate(),
                action = () =>
                {
                    var buildings = Map.listerThings.AllThings.OfType<Building_DroneStation>().ToList();
                    for (var i = 0; i < buildings.Count; i++)
                    {
                        buildings[i].lockdown = true;
                        foreach (var drone in buildings[i].spawnedDrones.ToList())
                            drone.jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, buildings[i]),
                                JobCondition.InterruptForced);
                    }
                },
                icon = ContentFinder<Texture2D>.Get("PRFUi/deactivate")
            };
            yield return new Command_Action
            {
                defaultLabel = "PRFDroneStationLiftLockdownAll".Translate(),
                defaultDesc = "PRFDroneStationLiftLockdownAllDesc".Translate(),
                action = () =>
                {
                    var buildings = Map.listerThings.AllThings.OfType<Building_DroneStation>().ToList();
                    for (var i = 0; i < buildings.Count; i++) buildings[i].lockdown = false;
                },
                icon = ContentFinder<Texture2D>.Get("PRFUi/activate")
            };
            if (DroneRange == 0)
                /*
                    "Verse.Designator"
                    Holds example of how i want this Gizmo Implemented
                    */
                yield return new DroneAreaSelector
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/AreaAllowedExpand"),
                    defaultLabel = droneAreaSelectorLable,
                    selectAction = a =>
                    {
                        Update_droneAllowedArea_forDrones(a);
                        update_droneAreaSelectorLable(a);
                    }
                };
            if (Prefs.DevMode)
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Respawn drones",
                    defaultDesc = "Respawns all Drones",
                    action = () =>
                    {
                        for (var i = spawnedDrones.Count - 1; i >= 0; i--)
                        {
                            spawnedDrones[i].Destroy();
                            Notify_DroneGained();
                        }
                    }
                };
        }
    }
}