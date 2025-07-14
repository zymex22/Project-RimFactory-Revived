using ProjectRimFactory.AutoMachineTool;
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
    
    [StaticConstructorOnStartup]
    public abstract class Building_DroneStation : Building, IPowerSupplyMachineHolder, IDroneSeetingsITab, IPRF_SettingsContentLink
    {
        //Sleep Time List (Loaded on Spawn)
        public string[] CachedSleepTimeList;

        protected CompRefuelable RefuelableComp;

        public List<SkillRecord> GetDroneSkillsRecord { get; set; } = [];

        //Return the Range depending on the Active Defenition
        public int DroneRange
        {
            get
            {
                if (compPowerWorkSetting != null)
                {
                    return (int)Math.Ceiling(compPowerWorkSetting.GetRange());
                }
                return def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius;
            }
        }
        
        private CompPowerWorkSetting compPowerWorkSetting;

        public IEnumerable<IntVec3> StationRangeCells
        {
            get
            {
                if (compPowerWorkSetting != null && compPowerWorkSetting.RangeSetting)
                {
                    return compPowerWorkSetting.GetRangeCells();
                }
                else
                {
                    return this.OccupiedRect().ExpandedBy(DroneRange).Cells;
                }
            }
        }
        private IEnumerable<IntVec3> stationRangeCellsOld;

        public List<IntVec3> CashedGetCoverageCells = null;

        //DroneAllowedArea Loaded on Spawn | this is the zone where the DronePawns are allowed to move in
        //This needs to be "Area" as one can cast "DroneArea" to "Area" but not the other way around
        //That feature is needed to assign vanilla Allowed Areas
        //Please note that for Area Null is a valid Value. it stands for unrestricted
        public Area DroneAllowedArea = null;

        public DroneArea GetDroneAllowedArea
        {
            get
            {
                if (DroneRange <= 0) return null;
                var droneArea = new DroneArea(Map.areaManager);
                //Need to set the Area to a size

                foreach (IntVec3 cell in StationRangeCells)
                {
                    if (cell.InBounds(Map)) droneArea[cell] = true;
                }

                //Not sure if I need that but just to be sure
                droneArea[Position] = true;
                Map.areaManager.AllAreas.Add(droneArea);

                return droneArea;
            }
        }

        //This function can be used to Update the Allowed area for all Drones (Active and future)
        //Just need to auto call tha on Change from CompPowerWorkSetting
        public void Update_droneAllowedArea_forDrones(Area dr)
        {
            //Refresh area if current is null
            DroneAllowedArea = dr ?? (Area)GetDroneAllowedArea;

            if (!stationRangeCellsOld.SequenceEqual(StationRangeCells))
            {
                ((DroneArea)DroneAllowedArea).SetMutable(true);
                DroneAllowedArea.Delete();

                DroneAllowedArea = (Area)GetDroneAllowedArea;
                stationRangeCellsOld = StationRangeCells;
            }

            for (int i = 0; i < spawnedDrones.Count; i++)
            {
                spawnedDrones[i].playerSettings.AreaRestrictionInPawnCurrentMap = DroneAllowedArea;
            }
        }

        public static readonly Texture2D Cancel = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
        protected bool Lockdown;
        private string droneAreaSelectorLable = "PRFDroneStationSelectArea".Translate("PRFDroneStationUnrestricted".Translate());
        protected DefModExtension_DroneStation extension;
        protected List<Pawn_Drone> spawnedDrones = new List<Pawn_Drone>();

        public abstract int DronesLeft { get; }

        public IPowerSupplyMachine RangePowerSupplyMachine => this.GetComp<CompPowerWorkSetting>();

        public Dictionary<WorkTypeDef, bool> WorkSettings = new Dictionary<WorkTypeDef, bool>();

        public Dictionary<WorkTypeDef, bool> GetWorkSettings
        {
            get => WorkSettings;
            set => WorkSettings = value;
        }

        public List<SkillRecord> DroneSeetings_skillDefs => GetDroneSkillsRecord;

        public string[] GetSleepTimeList => CachedSleepTimeList;

        public CompRefuelable compRefuelable => GetComp<CompRefuelable>();

        public void UpdateDronePriorities()
        {
            if (spawnedDrones.Count <= 0) return;
            foreach (var pawn in spawnedDrones)
            {
                foreach (var workTypeDef in WorkSettings.Keys)
                {
                    pawn.workSettings.SetPriority(workTypeDef, WorkSettings[workTypeDef] ? 3 : 0);
                }
            }
        }

        // Used for destroyed pawns
        public abstract void Notify_DroneLost();
        // Used to negate imaginary pawns despawned in WorkGiverDroneStations and JobDriver_ReturnToStation
        public abstract void Notify_DroneGained();

        public override void PostMake()
        {
            base.PostMake();
            extension = def.GetModExtension<DefModExtension_DroneStation>();
            RefuelableComp = GetComp<CompRefuelable>();
            CompPowerTrader = GetComp<CompPowerTrader>();
            compPowerWorkSetting = GetComp<CompPowerWorkSetting>();
        }

        private MapTickManager mapManager;
        protected MapTickManager MapManager => this.mapManager;

        IPRF_SettingsContent IPRF_SettingsContentLink.PRF_SettingsContentOb => new ITab_DroneStation_Def(this);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.mapManager = map.GetComponent<MapTickManager>();
            RefuelableComp = GetComp<CompRefuelable>();
            CompPowerTrader = GetComp<CompPowerTrader>();
            extension = def.GetModExtension<DefModExtension_DroneStation>();
            compPowerWorkSetting = GetComp<CompPowerWorkSetting>();
            stationRangeCellsOld = StationRangeCells;
            //Setup Allowed Area
            //Ensuring that Update_droneAllowedArea_forDrones() is run resolves #224 (May need to add a different check)
            if (DroneAllowedArea == null)
            {
                //Log.Message("droneAllowedArea was null");
                Update_droneAllowedArea_forDrones(DroneAllowedArea);
            }
            //Load the SleepTimes from XML
            CachedSleepTimeList = extension.Sleeptimes.Split(',');

            CashedGetCoverageCells = StationRangeCells.ToList();

            //Check for missing WorkTypeDef
            foreach (WorkTypeDef workTypeDef in extension.workTypes.Except(WorkSettings.Keys).ToList())
            {
                WorkSettings.Add(workTypeDef, true);
            }
            //Remove stuff that's no longer valid (can only happen after updates)
            foreach (WorkTypeDef workTypeDef in WorkSettings.Keys.Except(extension.workTypes).ToList())
            {
                WorkSettings.Remove(workTypeDef);
            }
            //need to take action to init droneSkillsRecord
            if (GetDroneSkillsRecord.Count == 0)
            {
                Pawn_Drone drone = MakeDrone();
                GenSpawn.Spawn(drone, Position, Map);
                drone.Destroy();

                GetComp<CompRefuelable>()?.Refuel(1);

            }
            //Init the Designator default Label
            update_droneAreaSelectorLabel(DroneAllowedArea);

            //Need this type of call to set the Power consumption on load
            //A normal call will not work
            var rangePowerSupplyMachine = this.RangePowerSupplyMachine;
            if (rangePowerSupplyMachine == null) return;
            this.MapManager.NextAction(rangePowerSupplyMachine.RefreshPowerStatus);
            this.MapManager.AfterAction(5, rangePowerSupplyMachine.RefreshPowerStatus);
        }

        private void update_droneAreaSelectorLabel(Area a)
        {
            if (a == null)
            {
                droneAreaSelectorLable = "PRFDroneStationSelectArea".Translate("PRFDroneStationUnrestricted".Translate());
            }
            else
            {
                droneAreaSelectorLable = "PRFDroneStationSelectArea".Translate(a.Label);
            }
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            if (phase != DrawPhase.Draw) return; //Crashes when drawing 2 things at the same time in some of the other phases
            if (extension.displayDormantDrones)
            {
                DrawDormantDrones();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            var drones = spawnedDrones.ToList();
            for (var i = 0; i < drones.Count; i++)
            {
                drones[i].Destroy();
            }
            if (DroneAllowedArea is DroneArea droneArea)
            {
                //Delete the old Zone
                droneArea.SetMutable(true);
                droneArea.Delete();
            }
            DroneAllowedArea = null;
        }

        public virtual void DrawDormantDrones()
        {
            foreach (var cell in GenAdj.CellsOccupiedBy(this).Take(DronesLeft))
            {
                PRFDefOf.PRFDrone.graphic.DrawFromDef(cell.ToVector3ShiftedWithAltitude(AltitudeLayer.LayingPawn),
                    default(Rot4), PRFDefOf.PRFDrone);
            }
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (Lockdown)
            {
                Map.overlayDrawer.DrawOverlay(this, OverlayTypes.ForbiddenBig);
            }
        }

        public abstract Job TryGiveJob(Pawn pawn);
        
        protected CompPowerTrader CompPowerTrader;

        protected int AdditionJobSearchTickDelay = 0;


        protected override void Tick()
        {
            //Base Tick
            base.Tick();
            //Return if not Spawnd
            if (!this.Spawned) return;


            //Should not draw much performence...
            //To enhance performence we could add "this.IsHashIntervalTick(60)"
            if (spawnedDrones.Count > 0 && CompPowerTrader?.PowerOn == false)
            {
                for (var i = spawnedDrones.Count - 1; i >= 0; i--)
                {
                    spawnedDrones[i].jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, this), JobCondition.InterruptForced);
                }
                //return as there is nothing to do if its off....
                return;
            }


            //Update the Allowed Area Range on Power Change
            if (this.IsHashIntervalTick(60))
            {
                //Update the Range
                Update_droneAllowedArea_forDrones(DroneAllowedArea);

                //TODO add cell calc
                CashedGetCoverageCells = StationRangeCells.ToList();
            }

            //Search for Job
            if (this.IsHashIntervalTick(60 + AdditionJobSearchTickDelay) && DronesLeft > 0 && !Lockdown && CompPowerTrader?.PowerOn != false)
            {
                //The issue appears to be 100% with TryGiveJob
                Pawn_Drone drone = MakeDrone();
                GenSpawn.Spawn(drone, Position, Map);

                Job job = TryGiveJob(drone);

                if (job != null)
                {
                    AdditionJobSearchTickDelay = 0; //Reset to 0 - found a job -> may find more
                    job.playerForced = true; // Why is that here? (included since the very beginning)
                    //MakeDrone takes about 1ms
                    
                    drone.jobs.StartJob(job);
                }
                else
                {
                    drone.Destroy();
                    Notify_DroneGained();
                    //Experimental Delay
                    //Add delay (limit to 300) i am well aware that this can be higher that 300 with the current code
                    if (AdditionJobSearchTickDelay < 300)
                    {
                        //Exponential delay
                        AdditionJobSearchTickDelay = (AdditionJobSearchTickDelay + 1) * 2;
                    }
                }
            }
        }
        public void Notify_DroneMayBeLost(Pawn_Drone drone)
        {
            if (!spawnedDrones.Contains(drone)) return;
            spawnedDrones.Remove(drone);
            Notify_DroneLost();
        }

        //Handel the Range UI
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            //Dont Draw if infinite
            if (def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius > 0)
            {
                GenDraw.DrawFieldEdges(CashedGetCoverageCells);
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


        private static List<Hediff> droneDiffs = null;

        public Pawn_Drone MakeDrone()
        {
            var drone = (Pawn_Drone)ThingMaker.MakeThing(PRFDefOf.PRFDroneKind.race);
            drone.kindDef = PRFDefOf.PRFDroneKind;
            drone.SetFactionDirect(Faction);
            PawnComponentsUtility.CreateInitialComponents(drone);
            drone.gender = Gender.None;
            drone.ageTracker.AgeBiologicalTicks = 0;
            drone.ageTracker.AgeChronologicalTicks = 0;

            if (droneDiffs == null)
            {
                PawnTechHediffsGenerator.GenerateTechHediffsFor(drone);
                droneDiffs = new List<Hediff>(drone.health.hediffSet.hediffs);
            }
            else
            {
                drone.health.hediffSet.hediffs = droneDiffs.ToList();
            }


            drone.Faction.Notify_PawnJoined(drone);
            drone.relations = new Pawn_RelationsTracker(drone);

            //Set Ido for Style Support
            drone.ideo = new Pawn_IdeoTracker(drone);
            drone.ideo.SetIdeo(Faction.ideos.PrimaryIdeo);

            drone.BaseStation = this;
            spawnedDrones.Add(drone);
            return drone;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref spawnedDrones, "spawnedDrones", LookMode.Reference);
            Scribe_Values.Look(ref Lockdown, "lockdown");
            Scribe_References.Look(ref DroneAllowedArea, "droneAllowedArea");
            //WorkSettings
            Scribe_Collections.Look(ref WorkSettings, "WorkSettings");
            WorkSettings ??= new Dictionary<WorkTypeDef, bool>();
            //init RefuelableComp after a Load
            RefuelableComp ??= this.GetComp<CompRefuelable>();
            stationRangeCellsOld ??= StationRangeCells;
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
                    Lockdown = !Lockdown;
                    if (Lockdown)
                    {
                        foreach (Pawn_Drone drone in spawnedDrones.ToList())
                        {
                            drone.jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, this), JobCondition.InterruptForced);
                        }
                    }
                },
                isActive = () => Lockdown,
                icon = Cancel
            };
            yield return new Command_Action()
            {
                defaultLabel = "PRFDroneStationLockdownAll".Translate(),
                defaultDesc = "PRFDroneStationLockdownAllDesc".Translate(),
                action = () =>
                {
                    var buildings = Map.listerThings.AllThings.OfType<Building_DroneStation>().ToList();
                    for (var i = 0; i < buildings.Count; i++)
                    {
                        buildings[i].Lockdown = true;
                        foreach (var drone in buildings[i].spawnedDrones.ToList())
                        {
                            drone.jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, buildings[i]), JobCondition.InterruptForced);
                        }
                    }

                },
                icon = ContentFinder<Texture2D>.Get("PRFUi/deactivate", true)
            };
            yield return new Command_Action()
            {
                defaultLabel = "PRFDroneStationLiftLockdownAll".Translate(),
                defaultDesc = "PRFDroneStationLiftLockdownAllDesc".Translate(),
                action = () =>
                {
                    List<Building_DroneStation> buildings = Map.listerThings.AllThings.OfType<Building_DroneStation>().ToList();
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        buildings[i].Lockdown = false;
                    }

                },
                icon = ContentFinder<Texture2D>.Get("PRFUi/activate", true)
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
                    SelectAction = (a) =>
                    {
                        Update_droneAllowedArea_forDrones(a);
                        update_droneAreaSelectorLabel(a);

                    }
                };


            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "DEV: Respawn drones",
                    defaultDesc = "Respawns all Drones",
                    action = () =>
                    {
                        for (int i = spawnedDrones.Count - 1; i >= 0; i--)
                        {
                            spawnedDrones[i].Destroy();
                            Notify_DroneGained();
                        }
                    },
                };
            }
        }
    }
}
