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

    public class DroneArea : Area
    {
        private string labelInt;
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

        public override int ListPriority => throw new NotImplementedException();

        public override string GetUniqueLoadID()
        {
            throw new NotImplementedException();
        }

    }

    public class DroneDefModExtension : DefModExtension
    {
        public int SquareJobRadius = 0; //0 Means infinite
    }


    [StaticConstructorOnStartup]
    public abstract class Building_DroneStation : Building
    {

        public IEnumerable<IntVec3> StationRangecells
        {
            get
            {
                return GenAdj.OccupiedRect(this).ExpandedBy(def.GetModExtension<DroneDefModExtension>().SquareJobRadius).Cells;
            }
        }

        public List<IntVec3> GetCoverageCells
        {
            get
            {
                return StationRangecells.ToList();
            }
        }

        public Area DroneAllowedArea
        {
            get
            {
                Area droneArea;
                droneArea = new DroneArea(this.Map.areaManager);
                //Need to set the Area to a size


                foreach (IntVec3 cell in StationRangecells)
                {
                    droneArea[cell] = true;
                }
                //Not shure if i need that but just to be shure
                droneArea[Position] = true;
                return droneArea;

            }
        }

        public static readonly Texture2D Cancel = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
        protected bool lockdown;
        protected DefModExtension_DroneStation extension;
        protected List<Pawn_Drone> spawnedDrones = new List<Pawn_Drone>();

        public abstract int DronesLeft { get; }
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
            if (def.GetModExtension<DroneDefModExtension>().SquareJobRadius > 0) { 
                GenDraw.DrawFieldEdges(GetCoverageCells);
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
        }
    }
}
