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

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            skills = new Pawn_SkillTracker(this);
            foreach (SkillRecord record in skills.skills)
            {
                record.levelInt = 20;
                record.passion = Passion.None;
            }
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
        }

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(250))
            {
                foreach (SkillRecord sr in skills.skills)
                {
                    sr.levelInt = 20;
                    if (sr.xpSinceLastLevel > 1f)
                    {
                        sr.xpSinceMidnight = 100f;
                        sr.xpSinceLastLevel = 100f;
                    }
                }
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
        }
    }
}
