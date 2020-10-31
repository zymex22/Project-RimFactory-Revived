using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Verse.AI;
using ProjectRimFactory.Common;
using UnityEngine;
using ProjectRimFactory.SAL3;

namespace ProjectRimFactory.Drones
{

    public class Pawn_Drone : Pawn
    {
        public Building_DroneStation station;

        // don't do anythin exciting when killed - just disappear:
        //   (this keeps weird side effects from happening, such as
        //    forever increasing the list of dead drones)
        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null) {
          // don't call base.Kill
          this.Destroy();
        }

        // or destroyed
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
        if (this.Spawned) this.DeSpawn();
            // don't call base.Destroy();
            // DO set mapIndexOrState to -2 to make "thing.Destroyed" true (needed for Work Tab Compatibility)
            ReflectionUtility.mapIndexOrState.SetValue(this, (sbyte)-2);
        }

        private ModExtension_Skills skillSettings;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            //Kill all invalid spawns
            if (this.station == null)
            {
                this.Kill(null);
                return;
            }

            base.SpawnSetup(map, respawningAfterLoad);
            skills = new Pawn_SkillTracker(this);
            skillSettings =  station.def.GetModExtension<ModExtension_Skills>();
            station.GetDroneSkillsRecord = DroneSkills.UpdateSkills(skills,station.GetDroneSkillsRecord,skillSettings,true);

            story = new Pawn_StoryTracker(this)
            {
                bodyType = BodyTypeDefOf.Thin,
                crownType = CrownType.Average,
                childhood = DroneBackstories.childhood,
                adulthood = DroneBackstories.adulthood
            };
            drafter = new Pawn_DraftController(this);
            relations = new Pawn_RelationsTracker(this);
            Name = new NameSingle("PRFDroneName".Translate());

            //Set the AreaRestriction. null means Unrestricted
            playerSettings.AreaRestriction = this.station.droneAllowedArea;
        }

        public override void Tick()
        {
            base.Tick();
            if (Downed)
            {
                Kill(null);
            }
        }

        //Disabeling this for now as i think we dont need the refresh on Rare Tick.
        //I think on spawn && Long Tick is Enoth
        //public override void TickRare()
        //{
        //    base.TickRare();
        //    DroneSkills.UpdateSkills(skills, station.GetDroneSkillsRecord, skillSettings);
        //}

        public override void TickLong()
        {
            base.TickLong();
            station.GetDroneSkillsRecord = DroneSkills.UpdateSkills(skills, station.GetDroneSkillsRecord, skillSettings, true);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            if (station != null)
            {
                station.Notify_DroneMayBeLost(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref station, "station");
        }
    }
}
