using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones
{
    // ReSharper disable once UnusedType.Global
    public class Building_DroneCultivator : Building_WorkGiverDroneStation
    {
        private int TotalDroneCount => SpawnedDrones.Count + dronesLeft;

        private int dronesLeft;

        protected override int DronesLeft => dronesLeft - SpawnedDrones.Count;

        protected override void Notify_DroneLost()
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
            dronesLeft = DefModExtensionDroneStation.GetDronesOnSpawn();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (TotalDroneCount < DefModExtensionDroneStation.GetDronesOnSpawn())
            {
                dronesLeft = DefModExtensionDroneStation.GetDronesOnSpawn();
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

        private void MakeMatchingGrowZone()
        {
            var designator = new Designator_ZoneAdd_Growing();
            designator.DesignateMultiCell(from tempCell in CashedGetCoverageCells
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
