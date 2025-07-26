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

        //Return the Range depending on the Active Definition
        private int DroneRange
        {
            get
            {
                if (compPowerWorkSetting != null)
                {
                    return (int)Math.Ceiling(compPowerWorkSetting.GetRange());
                }
                return DefModExtensionDroneStation.SquareJobRadius;
            }
        }
        
        private CompPowerWorkSetting compPowerWorkSetting;

        private IEnumerable<IntVec3> StationRangeCells
        {
            get
            {
                if (compPowerWorkSetting is { RangeSetting: true })
                {
                    return compPowerWorkSetting.GetRangeCells();
                }
                return this.OccupiedRect().ExpandedBy(DroneRange).Cells;
            }
        }
        private IEnumerable<IntVec3> stationRangeCellsOld;

        protected List<IntVec3> CashedGetCoverageCells;

        //DroneAllowedArea Loaded on Spawn | this is the zone where the DronePawns are allowed to move in
        //This needs to be "Area" as one can cast "DroneArea" to "Area" but not the other way around
        //That feature is needed to assign vanilla Allowed Areas
        //Please note that for Area Null is a valid Value. it stands for unrestricted
        public Area DroneAllowedArea;

        private DroneArea GetDroneAllowedArea
        {
            get
            {
                if (DroneRange <= 0) return null;
                var droneArea = new DroneArea(Map.areaManager);
                //Need to set the Area to a size

                foreach (var cell in StationRangeCells)
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
        private void Update_droneAllowedArea_forDrones(Area dr)
        {
            //Refresh area if current is null
            DroneAllowedArea = dr ?? GetDroneAllowedArea;

            if (!stationRangeCellsOld.SequenceEqual(StationRangeCells))
            {
                ((DroneArea)DroneAllowedArea).SetMutable(true);
                DroneAllowedArea.Delete();

                DroneAllowedArea = GetDroneAllowedArea;
                stationRangeCellsOld = StationRangeCells;
            }

            for (var i = 0; i < SpawnedDrones.Count; i++)
            {
                SpawnedDrones[i].playerSettings.AreaRestrictionInPawnCurrentMap = DroneAllowedArea;
            }
        }

        private static readonly Texture2D Cancel = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
        private bool lockdown;
        private string droneAreaSelectorLable = "PRFDroneStationSelectArea".Translate("PRFDroneStationUnrestricted".Translate());
        protected DefModExtension_DroneStation DefModExtensionDroneStation;
        protected List<Pawn_Drone> SpawnedDrones = [];

        protected abstract int DronesLeft { get; }

        public IPowerSupplyMachine RangePowerSupplyMachine => compPowerWorkSetting;

        protected Dictionary<WorkTypeDef, bool> WorkSettings = new();

        public Dictionary<WorkTypeDef, bool> GetWorkSettings
        {
            get => WorkSettings;
            set => WorkSettings = value;
        }

        public List<SkillRecord> DroneSettingsSkillDefs => GetDroneSkillsRecord;

        public string[] GetSleepTimeList => CachedSleepTimeList;

        public CompRefuelable CompRefuelable => RefuelableComp;

        // Used for destroyed pawns
        protected abstract void Notify_DroneLost();
        // Used to negate imaginary pawns despawned in WorkGiverDroneStations and JobDriver_ReturnToStation
        public abstract void Notify_DroneGained();

        public override void PostMake()
        {
            base.PostMake();
            DefModExtensionDroneStation = def.GetModExtension<DefModExtension_DroneStation>();
            RefuelableComp = GetComp<CompRefuelable>();
            compPowerTrader = GetComp<CompPowerTrader>();
            compPowerWorkSetting = GetComp<CompPowerWorkSetting>();
        }

        private MapTickManager MapManager { get; set; }

        IPRF_SettingsContent IPRF_SettingsContentLink.PRF_SettingsContentOb => new ITab_DroneStation_Def(this);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            MapManager = map.GetComponent<MapTickManager>();
            RefuelableComp = GetComp<CompRefuelable>();
            compPowerTrader = GetComp<CompPowerTrader>();
            DefModExtensionDroneStation = def.GetModExtension<DefModExtension_DroneStation>();
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
            CachedSleepTimeList = DefModExtensionDroneStation.Sleeptimes.Split(',');

            CashedGetCoverageCells = StationRangeCells.ToList();

            //Check for missing WorkTypeDef
            foreach (var workTypeDef in DefModExtensionDroneStation.workTypes.Except(WorkSettings.Keys).ToList())
            {
                WorkSettings.Add(workTypeDef, true);
            }
            //Remove stuff that's no longer valid (can only happen after updates)
            foreach (var workTypeDef in WorkSettings.Keys.Except(DefModExtensionDroneStation.workTypes).ToList())
            {
                WorkSettings.Remove(workTypeDef);
            }
            //need to take action to init droneSkillsRecord
            if (GetDroneSkillsRecord.Count == 0)
            {
                var drone = MakeDrone();
                GenSpawn.Spawn(drone, Position, Map);
                drone.Destroy();

                RefuelableComp?.Refuel(1);

            }
            //Init the Designator default Label
            update_droneAreaSelectorLabel(DroneAllowedArea);

            //Need this type of call to set the Power consumption on load
            //A normal call will not work
            var rangePowerSupplyMachine = RangePowerSupplyMachine;
            if (rangePowerSupplyMachine == null) return;
            MapManager.NextAction(rangePowerSupplyMachine.RefreshPowerStatus);
            MapManager.AfterAction(5, rangePowerSupplyMachine.RefreshPowerStatus);
        }

        private void update_droneAreaSelectorLabel(Area area)
        {
            if (area is null)
            {
                droneAreaSelectorLable = "PRFDroneStationSelectArea".Translate("PRFDroneStationUnrestricted".Translate());
                return;
            }

            droneAreaSelectorLable = "PRFDroneStationSelectArea".Translate(area.Label);
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            if (phase != DrawPhase.Draw) return; //Crashes when drawing 2 things at the same time in some of the other phases
            if (DefModExtensionDroneStation.displayDormantDrones)
            {
                DrawDormantDrones();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            var drones = SpawnedDrones.ToList();
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

        protected virtual void DrawDormantDrones()
        {
            foreach (var cell in GenAdj.CellsOccupiedBy(this).Take(DronesLeft))
            {
                PRFDefOf.PRFDrone.graphic.DrawFromDef(cell.ToVector3ShiftedWithAltitude(AltitudeLayer.LayingPawn),
                    default, PRFDefOf.PRFDrone);
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

        public abstract Job TryGiveJob(Pawn pawn);

        private CompPowerTrader compPowerTrader;

        private int additionJobSearchTickDelay;


        protected override void Tick()
        {
            //Base Tick
            base.Tick();
            //Return if not Spawned
            if (!Spawned) return;


            //Should not draw much performance...
            //To enhance performance we could add "this.IsHashIntervalTick(60)"
            if (SpawnedDrones.Count > 0 && compPowerTrader?.PowerOn == false)
            {
                for (var i = SpawnedDrones.Count - 1; i >= 0; i--)
                {
                    SpawnedDrones[i].jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, this), JobCondition.InterruptForced);
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
            if (!this.IsHashIntervalTick(60 + additionJobSearchTickDelay) || DronesLeft <= 0 || lockdown || compPowerTrader?.PowerOn == false) return;
            //The issue appears to be 100% with TryGiveJob
            var drone = MakeDrone();
            GenSpawn.Spawn(drone, Position, Map);

            var job = TryGiveJob(drone);

            if (job != null)
            {
                additionJobSearchTickDelay = 0; //Reset to 0 - found a job -> may find more
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
                if (additionJobSearchTickDelay < 300)
                {
                    //Exponential delay
                    additionJobSearchTickDelay = (additionJobSearchTickDelay + 1) * 2;
                }
            }
        }
        public void Notify_DroneMayBeLost(Pawn_Drone drone)
        {
            if (!SpawnedDrones.Contains(drone)) return;
            SpawnedDrones.Remove(drone);
            Notify_DroneLost();
        }

        //Handel the Range UI
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            //Dont Draw if infinite
            if (DefModExtensionDroneStation.SquareJobRadius > 0)
            {
                GenDraw.DrawFieldEdges(CashedGetCoverageCells);
            }

        }

        public override string GetInspectString()
        {
            var builder = new StringBuilder();
            var str = base.GetInspectString();
            if (!string.IsNullOrEmpty(str))
            {
                builder.AppendLine(str);
            }
            builder.Append("PRFDroneStation_NumberOfDrones".Translate(DronesLeft));
            return builder.ToString();
        }


        private static List<Hediff> droneDiffs;

        private Pawn_Drone MakeDrone()
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
                droneDiffs = [..drone.health.hediffSet.hediffs];
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
            SpawnedDrones.Add(drone);
            return drone;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref SpawnedDrones, "spawnedDrones", LookMode.Reference);
            Scribe_Values.Look(ref lockdown, "lockdown");
            Scribe_References.Look(ref DroneAllowedArea, "droneAllowedArea");
            //WorkSettings
            Scribe_Collections.Look(ref WorkSettings, "WorkSettings");
            WorkSettings ??= new Dictionary<WorkTypeDef, bool>();
            //init RefuelableComp after a Load
            RefuelableComp ??= GetComp<CompRefuelable>();
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
                    lockdown = !lockdown;
                    if (!lockdown) return;
                    foreach (var drone in SpawnedDrones.ToList())
                    {
                        drone.jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, this), JobCondition.InterruptForced);
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
                    var buildings = Map.listerThings.AllThings.OfType<Building_DroneStation>().ToList();
                    for (var i = 0; i < buildings.Count; i++)
                    {
                        buildings[i].lockdown = true;
                        foreach (var drone in buildings[i].SpawnedDrones.ToList())
                        {
                            drone.jobs.StartJob(new Job(PRFDefOf.PRFDrone_ReturnToStation, buildings[i]), JobCondition.InterruptForced);
                        }
                    }

                },
                icon = ContentFinder<Texture2D>.Get("PRFUi/deactivate")
            };
            yield return new Command_Action()
            {
                defaultLabel = "PRFDroneStationLiftLockdownAll".Translate(),
                defaultDesc = "PRFDroneStationLiftLockdownAllDesc".Translate(),
                action = () =>
                {
                    var buildings = Map.listerThings.AllThings.OfType<Building_DroneStation>().ToList();
                    for (var i = 0; i < buildings.Count; i++)
                    {
                        buildings[i].lockdown = false;
                    }

                },
                icon = ContentFinder<Texture2D>.Get("PRFUi/activate")
            };
            if (DroneRange == 0)
            {
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
                        for (var i = SpawnedDrones.Count - 1; i >= 0; i--)
                        {
                            SpawnedDrones[i].Destroy();
                            Notify_DroneGained();
                        }
                    },
                };
            }
        }
    }
}
