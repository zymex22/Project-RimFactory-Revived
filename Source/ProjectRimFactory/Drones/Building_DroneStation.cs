﻿using ProjectRimFactory.AutoMachineTool;
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

        private bool mutable = false;

        public override bool Mutable => mutable;

        public void SetMutable(bool val)
        {
            mutable = val;
        }

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
                    //base.ProcessInput(ev);
                }
                AreaUtility.MakeAllowedAreaListFloatMenu(delegate (Area a)
                {
                    selectedArea = a;
                    // base.ProcessInput(ev);

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
    public abstract class Building_DroneStation : Building, IPowerSupplyMachineHolder, IDroneSeetingsITab, IPRF_SettingsContentLink
    {
        //Sleep Time List (Loaded on Spawn)
        public string[] cachedSleepTimeList;

        private const int defaultSkillLevel = 20;

        public int GetdefaultSkillLevel => defaultSkillLevel;

        protected CompRefuelable refuelableComp;

        private List<SkillRecord> droneSkillsRecord = new List<SkillRecord>();

        public List<SkillRecord> GetDroneSkillsRecord
        {
            get => droneSkillsRecord;
            set => droneSkillsRecord = value;
        }

        //Return the Range depending on the Active Defenition
        public int DroneRange
        {
            get
            {
                if (compPowerWorkSetting != null)
                {
                    return (int)Math.Ceiling(compPowerWorkSetting.GetRange());
                }
                else
                {
                    return def.GetModExtension<DefModExtension_DroneStation>().SquareJobRadius;
                }
            }

        }



        private CompPowerWorkSetting compPowerWorkSetting;

        public IEnumerable<IntVec3> StationRangecells
        {
            get
            {
                if (compPowerWorkSetting != null && compPowerWorkSetting.RangeSetting)
                {
                    return compPowerWorkSetting.GetRangeCells();
                }
                else
                {
                    return GenAdj.OccupiedRect(this).ExpandedBy(DroneRange).Cells;
                }
            }
        }
        private IEnumerable<IntVec3> StationRangecells_old;

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
                        if (cell.InBounds(this.Map)) droneArea[cell] = true;
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
        public void Update_droneAllowedArea_forDrones(Area dr)
        {
            //Refresh area if current is null
            droneAllowedArea = dr ?? (Area)GetDroneAllowedArea;

            if (!StationRangecells_old.SequenceEqual(StationRangecells))
            {
                ((DroneArea)droneAllowedArea).SetMutable(true);
                droneAllowedArea.Delete();

                droneAllowedArea = (Area)GetDroneAllowedArea;
                StationRangecells_old = StationRangecells;
            }

            for (int i = 0; i < spawnedDrones.Count; i++)
            {
                spawnedDrones[i].playerSettings.AreaRestrictionInPawnCurrentMap = droneAllowedArea;
            }
        }

        public static readonly Texture2D Cancel = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
        protected bool lockdown;
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

        public List<SkillRecord> DroneSeetings_skillDefs => droneSkillsRecord;

        public string[] GetSleepTimeList => cachedSleepTimeList;

        public CompRefuelable compRefuelable => GetComp<CompRefuelable>();

        public void UpdateDronePrioritys()
        {

            if (spawnedDrones.Count > 0)
            {
                foreach (Pawn pawn in spawnedDrones)
                {
                    foreach (WorkTypeDef def in WorkSettings.Keys)
                    {
                        if (WorkSettings[def])
                        {
                            pawn.workSettings.SetPriority(def, 3);
                        }
                        else
                        {
                            pawn.workSettings.SetPriority(def, 0);
                        }
                    }
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
            refuelableComp = GetComp<CompRefuelable>();
            compPowerTrader = GetComp<CompPowerTrader>();
            compPowerWorkSetting = GetComp<CompPowerWorkSetting>();
        }

        private MapTickManager mapManager;
        protected MapTickManager MapManager => this.mapManager;

        IPRF_SettingsContent IPRF_SettingsContentLink.PRF_SettingsContentOb => new ITab_DroneStation_Def(this);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.mapManager = map.GetComponent<MapTickManager>();
            refuelableComp = GetComp<CompRefuelable>();
            compPowerTrader = GetComp<CompPowerTrader>();
            extension = def.GetModExtension<DefModExtension_DroneStation>();
            compPowerWorkSetting = GetComp<CompPowerWorkSetting>();
            StationRangecells_old = StationRangecells;
            //Setup Allowd Area
            //Enssuring that Update_droneAllowedArea_forDrones() is run resolves #224 (May need to add a diffrent check)
            if (droneAllowedArea == null)
            {
                //Log.Message("droneAllowedArea was null");
                Update_droneAllowedArea_forDrones(droneAllowedArea);
            }
            //Load the SleepTimes from XML
            cachedSleepTimeList = extension.Sleeptimes.Split(',');

            cashed_GetCoverageCells = StationRangecells.ToList();

            //Check for missing WorkTypeDef
            foreach (WorkTypeDef def in extension.workTypes.Except(WorkSettings.Keys).ToList())
            {
                WorkSettings.Add(def, true);
            }
            //Remove stuff thats nolonger valid (can only happen after updates)
            foreach (WorkTypeDef def in WorkSettings.Keys.Except(extension.workTypes).ToList())
            {
                WorkSettings.Remove(def);
            }
            //need to take action to init droneSkillsRecord
            if (droneSkillsRecord.Count == 0)
            {
                Pawn_Drone drone = MakeDrone();
                GenSpawn.Spawn(drone, Position, Map);
                drone.Destroy();

                GetComp<CompRefuelable>()?.Refuel(1);

            }
            //Init the Designator default Label
            update_droneAreaSelectorLable(droneAllowedArea);

            //Need this type of call to set the Powerconsumption on load
            //A normal call will not work
            var rangePowerSupplyMachine = this.RangePowerSupplyMachine;
            if (rangePowerSupplyMachine != null)
            {
                this.MapManager.NextAction(rangePowerSupplyMachine.RefreshPowerStatus);
                this.MapManager.AfterAction(5, rangePowerSupplyMachine.RefreshPowerStatus);
            }
        }

        private void update_droneAreaSelectorLable(Area a)
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
            base.DeSpawn();
            List<Pawn_Drone> drones = spawnedDrones.ToList();
            for (int i = 0; i < drones.Count; i++)
            {
                drones[i].Destroy();
            }
            if (droneAllowedArea is DroneArea)
            {
                //Deleate the old Zone
                ((DroneArea)droneAllowedArea).SetMutable(true);
                droneAllowedArea.Delete();
            }

            droneAllowedArea = null;


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

        public abstract Job TryGiveJob(Pawn pawn);


        protected CompPowerTrader compPowerTrader;

        protected int additionJobSearchTickDelay = 0;


        protected override void Tick()
        {
            //Base Tick
            base.Tick();
            //Return if not Spawnd
            if (!this.Spawned) return;


            //Should not draw much performence...
            //To enhance performence we could add "this.IsHashIntervalTick(60)"
            if (spawnedDrones.Count > 0 && compPowerTrader?.PowerOn == false)
            {
                for (int i = spawnedDrones.Count - 1; i >= 0; i--)
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
                Update_droneAllowedArea_forDrones(droneAllowedArea);

                //TODO add cell calc
                cashed_GetCoverageCells = StationRangecells.ToList();
            }

            //Search for Job
            if (this.IsHashIntervalTick(60 + additionJobSearchTickDelay) && DronesLeft > 0 && !lockdown && compPowerTrader?.PowerOn != false)
            {
                //The issue appears to be 100% with TryGiveJob
                Pawn_Drone drone = MakeDrone();
                GenSpawn.Spawn(drone, Position, Map);

                Job job = TryGiveJob(drone);

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
            {
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


        private static List<Hediff> droneDiffs = null;

        public Pawn_Drone MakeDrone()
        {
            Pawn_Drone drone = (Pawn_Drone)ThingMaker.MakeThing(PRFDefOf.PRFDroneKind.race);
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
            {
                WorkSettings = new Dictionary<WorkTypeDef, bool>();
            }
            //init refuelableComp after a Load
            if (refuelableComp == null)
            {
                refuelableComp = this.GetComp<CompRefuelable>();
            }
            if (StationRangecells_old == null)
            {
                StationRangecells_old = StationRangecells;
            }


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
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        buildings[i].lockdown = true;
                        foreach (Pawn_Drone drone in buildings[i].spawnedDrones.ToList())
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
                        buildings[i].lockdown = false;
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
                    selectAction = (a) =>
                    {
                        Update_droneAllowedArea_forDrones(a);
                        update_droneAreaSelectorLable(a);

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
