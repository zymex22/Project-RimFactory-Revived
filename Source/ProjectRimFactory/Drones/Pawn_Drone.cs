using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Verse.AI;
using ProjectRimFactory.Common;
using UnityEngine;

namespace ProjectRimFactory.Drones
{

    public class Pawn_Drone : Pawn
    {
        public Building_DroneStation station;

        private const int defaultSkillLevel = 20;


        // don't do anythin exciting when killed - just disappear:
        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null) {
        // don't call base.Kill
        this.Destroy();
        }

        // or destroyed
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
        if (this.Spawned) this.DeSpawn();
        // don't call base.Destroy();
        }
        

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            skillSettings = this.station.def.GetModExtension<ModExtension_Skills>();
            skills = new Pawn_SkillTracker(this);

            UpdateSkills(skills);
            //foreach (SkillRecord record in skills.skills)
            //{
            //    record.levelInt = 20;
            //    record.passion = Passion.None;
            //}
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
        private ModExtension_Skills skillSettings;

        public void UpdateSkills(Pawn_SkillTracker skill)
        {
            foreach (SkillRecord record in skill.skills)
            {
                if (skillSettings != null)
                {
                    record.levelInt = skillSettings.skills.FirstOrDefault(x => x.def == record.def)?.levelInt ?? skillSettings.BaseSkill;
                }
                else {
                    record.levelInt = defaultSkillLevel; //No Settings Found use the Default
                }
                record.passion = Passion.None;
                if (record.xpSinceLastLevel > 1f)
                {
                    record.xpSinceMidnight = 100f;
                    record.xpSinceLastLevel = 100f;
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(250))
            {
                UpdateSkills(skills);
                //foreach (SkillRecord sr in skills.skills)
                //{
                //    sr.levelInt = 20;
                //    if (sr.xpSinceLastLevel > 1f)
                //    {
                //        sr.xpSinceMidnight = 100f;
                //        sr.xpSinceLastLevel = 100f;
                //    }
                //}
            }
            if (Downed)
            {
                Kill(null);
            }
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
          //  Scribe_References.Look(ref station.DroneAllowedArea, "Area");
        }
    }
}
