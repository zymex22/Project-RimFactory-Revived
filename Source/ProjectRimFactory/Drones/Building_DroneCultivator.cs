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

namespace ProjectRimFactory.Drones
{
    public class Building_DroneCultivator : Building_WorkGiverDroneStation
    {
        public Rot4 outputRotation = Rot4.North;

        private int totalDroneCount => spawnedDrones.Count + dronesLeft;

        int dronesLeft;

        public override int DronesLeft { get => dronesLeft - spawnedDrones.Count; }
        public override void Notify_DroneLost()
        {
            dronesLeft--;
        }
        public override void Notify_DroneGained()
        {
            dronesLeft++;
        }

        public override void PostMake()
        {
            base.PostMake();
            dronesLeft = extension.GetDronesOnSpawn();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (totalDroneCount < extension.GetDronesOnSpawn())
            {
                dronesLeft = extension.GetDronesOnSpawn();
            }
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
        }

        protected void MakeMatchingGrowZone()
        {
            Designator_ZoneAdd_Growing designator = new Designator_ZoneAdd_Growing();
            designator.DesignateMultiCell(from tempCell in cashed_GetCoverageCells
                                          where designator.CanDesignateCell(tempCell).Accepted
                                          select tempCell);
        }

        //Save Drone Count
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref dronesLeft, "dronesLeft");
        }
    }

    
}
