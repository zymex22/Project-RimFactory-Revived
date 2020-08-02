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

    public class Pawn_Drone : Pawn
    {
        public Building_DroneStation station;

        // don't do anythin exciting when killed - just disappear:
        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null) {
        // don't call base.Kill
        this.Destroy();
        }


        public IEnumerable<IntVec3> Rangecells
        {
            get
            {
                return GenAdj.OccupiedRect(this).ExpandedBy(this.station.def.GetModExtension<DroneDefModExtension>().SquareJobRadius).Cells;
            }
        }

        // or destroyed
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
        if (this.Spawned) this.DeSpawn();
        // don't call base.Destroy();
        }
        

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



            //If range is set (bigger then 0) then do handel the range
            if (this.station.def.GetModExtension<DroneDefModExtension>().SquareJobRadius > 0) { 
                //Handel Allowed area (not to be confuced with Area_Allowed)
                Area droneArea;
                droneArea = new DroneArea(this.Map.areaManager);
                //Need to set the Area to a size

                foreach (IntVec3 cell in Rangecells)
                {
                    droneArea[cell] = true;
                }
                //Not shure if i need that but just to be shure
                droneArea[Position] = true;

                playerSettings.AreaRestriction = droneArea;
            }
            


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
