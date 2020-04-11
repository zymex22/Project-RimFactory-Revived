using ProjectRimFactory.Common;
using ProjectRimFactory.Drones;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectRimFactory.CultivatorTools
{
    public class Building_DroneCultivator : Building_DroneStation
    {
        public Rot4 outputRotation = Rot4.North;

        public IntVec3 OutputSlot => Position + outputRotation.FacingCell * (this.def.GetModExtension<CultivatorDefModExtension>().squareAreaRadius + 1);

        int dronesLeft;
        List<IntVec3> cachedCoverageCells;

        public override int DronesLeft { get => dronesLeft - spawnedDrones.Count; }
        public override void Notify_DroneLost()
        {
            dronesLeft--;
        }
        public override void Notify_DroneGained()
        {
            dronesLeft++;
        }

        public override Job TryGiveJob()
        {
            for (int i = 0; i < cachedCoverageCells.Count; i++)
            {
                if (!Map.reservationManager.IsReservedByAnyoneOf(cachedCoverageCells[i], Faction))
                {
                    Job job = JobOnCell(cachedCoverageCells[i]);
                    if (job != null)
                    {
                        return job;
                    }
                }
            }
            return null;
        }

        Job JobOnCell(IntVec3 cell)
        {
            IPlantToGrowSettable plantToGrowSettable = cell.GetPlantToGrowSettable(Map);
            if (plantToGrowSettable != null)
            {
                bool plantFound = false;
                foreach (Thing t in cell.GetThingList(Map))
                {
                    if (t is Plant p)
                    {
                        plantFound = true;
                        if (!Map.reservationManager.IsReservedByAnyoneOf(t, Faction))
                        {
                            Job job = JobAtPlant(p, plantToGrowSettable);
                            if (job != null)
                            {
                                return job;
                            }
                        }
                    }
                    else if(t.def == plantToGrowSettable.GetPlantDefToGrow().plant.harvestedThingDef)
                    {
                        if (!Map.reservationManager.IsReservedByAnyoneOf(t, this.Faction) && !Map.reservationManager.IsReservedByAnyoneOf(this.OutputSlot, this.Faction))
                        {
                            var job = JobMaker.MakeJob(JobDefOf.HaulToCell, t, this.OutputSlot);
                            job.count = 99999;
                            job.haulOpportunisticDuplicates = false;
                            job.haulMode = HaulMode.ToCellNonStorage;
                            job.ignoreDesignations = true;
                            return job;
                        }
                    }
                }
                ThingDef plantDef = plantToGrowSettable.GetPlantDefToGrow();
                if (!plantFound &&
                    plantDef != null && 
                    plantToGrowSettable.CanPlantRightNow() &&
                    plantDef.CanEverPlantAt(cell, Map) &&
                    PlantUtility.GrowthSeasonNow(cell, Map))
                {
                    Thing blocker = PlantUtility.AdjacentSowBlocker(plantDef, cell, Map);
                    // Get rid of blockers
                    if (blocker != null)
                    {
                        if (blocker is Plant && blocker.def != plantDef && !Map.reservationManager.IsReservedByAnyoneOf(blocker, Faction))
                            return new Job(JobDefOf.CutPlant, blocker);
                        // Wait for blocker to be cut/moved
                    }
                    else
                    {
                        // Sow
                        return new Job(JobDefOf.Sow, cell)
                        {
                            plantDefToSow = plantToGrowSettable.GetPlantDefToGrow()
                        };
                    }
                }
            }
            return null;
        }

        Job JobAtPlant(Plant p, IPlantToGrowSettable plantToGrowSettable)
        {
            if (p.def == plantToGrowSettable.GetPlantDefToGrow())
            {
                // Harvest if fully grown
                if (p.Growth + 0.001f >= 1.00f)
                {
                    return new Job(JobDefOf.Harvest, p);
                }
            }
            else
            {
                // Cut if foreign plant
                return new Job(JobDefOf.CutPlant, p);
            }
            return null;
        }

        public override void PostMake()
        {
            base.PostMake();
            dronesLeft = extension.maxNumDrones;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            cachedCoverageCells = GetCoverageCells();
        }

        private List<IntVec3> GetCoverageCells()
        {
            int squareAreaRadius = def.GetModExtension<CultivatorDefModExtension>().squareAreaRadius;
            List<IntVec3> list = new List<IntVec3>((squareAreaRadius * 2 + 1) * (squareAreaRadius * 2 + 1));
            for (int i = -squareAreaRadius; i <= squareAreaRadius; i++)
            {
                for (int j = -squareAreaRadius; j <= squareAreaRadius; j++)
                {
                    list.Add(new IntVec3(i, 0, j) + Position);
                }
            }
            return list;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo baseGizmo in base.GetGizmos())
            {
                yield return baseGizmo;
            }
            yield return new Command_Action
            {
                action = MakeMatchingGrowZone,
                hotKey = KeyBindingDefOf.Misc2,
                defaultDesc = "CommandSunLampMakeGrowingZoneDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Growing"),
                defaultLabel = "CommandSunLampMakeGrowingZoneLabel".Translate()
            };
            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Misc/Compass"),
                defaultLabel = "CultivatorTools_AdjustDirection_Output".Translate(),
                defaultDesc = "CultivatorTools_AdjustDirection_Desc".Translate(outputRotation.AsCompassDirection()),
                activateSound = SoundDefOf.Click,
                action = () => outputRotation.Rotate(RotationDirection.Clockwise)
            };
        }

        protected void MakeMatchingGrowZone()
        {
            Designator_ZoneAdd_Growing designator = new Designator_ZoneAdd_Growing();
            designator.DesignateMultiCell(from tempCell in cachedCoverageCells
                                          where designator.CanDesignateCell(tempCell).Accepted
                                          select tempCell);
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(cachedCoverageCells);

            GenDraw.DrawFieldEdges(new List<IntVec3> { OutputSlot }, Color.cyan);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref dronesLeft, "dronesLeft");
        }
    }
}
