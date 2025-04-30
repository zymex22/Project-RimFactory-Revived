using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones
{
    public class Building_DroneCultivator : Building_WorkGiverDroneStation
    {
        private int totalDroneCount => spawnedDrones.Count + dronesLeft;

        private int dronesLeft;

        public override int DronesLeft => dronesLeft - spawnedDrones.Count;

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
