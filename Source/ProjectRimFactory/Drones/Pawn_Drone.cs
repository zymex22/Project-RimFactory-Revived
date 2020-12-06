using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Drones
{
    public class Pawn_Drone : Pawn
    {
        private ModExtension_Skills skillSettings;
        public Building_DroneStation station;

        // don't do anythin exciting when killed - just disappear:
        //   (this keeps weird side effects from happening, such as
        //    forever increasing the list of dead drones)
        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            // don't call base.Kill
            Destroy();
        }

        // or destroyed
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Spawned) DeSpawn();
            // don't call base.Destroy();
            // DO set mapIndexOrState to -2 to make "thing.Destroyed" true (needed for Work Tab Compatibility)
            ReflectionUtility.mapIndexOrState.SetValue(this, (sbyte) -2);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            //Kill all invalid spawns
            if (station == null)
            {
                Kill(null);
                return;
            }

            base.SpawnSetup(map, respawningAfterLoad);
            skills = new Pawn_SkillTracker(this);
            skillSettings = station.def.GetModExtension<ModExtension_Skills>();
            station.GetDroneSkillsRecord =
                DroneSkills.UpdateSkills(skills, station.GetDroneSkillsRecord, skillSettings, true);

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
            playerSettings.AreaRestriction = station.droneAllowedArea;
        }

        public override void Tick()
        {
            //This is an issue
            //from what i understand base.base is not a option
            //This means that i am limited in what i can remove

            //JobTrackerTick is the biggest / only issue. its insanly high for some reason
            base.Tick();
            if (Downed) Kill(null);
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
            station.GetDroneSkillsRecord =
                DroneSkills.UpdateSkills(skills, station.GetDroneSkillsRecord, skillSettings, true);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            if (station != null) station.Notify_DroneMayBeLost(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref station, "station");
        }
    }
}