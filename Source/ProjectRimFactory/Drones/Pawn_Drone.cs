using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3.Tools;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Drones
{
    
    public class Pawn_Drone : Pawn
    {
        public Building_DroneStation BaseStation;
        
        private ModExtension_Skills skillSettings;

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
            ReflectionUtility.MapIndexOrState.SetValue(this, (sbyte)-2);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            //Kill all invalid spawns
            if (BaseStation == null)
            {
                Kill(null);
                return;
            }

            base.SpawnSetup(map, respawningAfterLoad);
            skills = new Pawn_SkillTracker(this);
            skillSettings = BaseStation.def.GetModExtension<ModExtension_Skills>();
            BaseStation.GetDroneSkillsRecord = DroneSkills.UpdateSkills(skills, BaseStation.GetDroneSkillsRecord, skillSettings, true);

            story = new Pawn_StoryTracker(this)
            {
                bodyType = BodyTypeDefOf.Thin,
                Childhood = DroneBackstories.childhood,
                Adulthood = DroneBackstories.adulthood
            };
            drafter = new Pawn_DraftController(this);
            relations = new Pawn_RelationsTracker(this);
            Name = new NameSingle("PRFDroneName".Translate());

            //Set the AreaRestriction. null means Unrestricted
            // TODO Check if that is the correct replacement of if i need that effective thing
            playerSettings.AreaRestrictionInPawnCurrentMap = BaseStation.DroneAllowedArea;
        }

        protected override void Tick()
        {
            //This is an issue
            //from what i understand base.base is not a option
            //This means that i am limited in what i can remove

            //JobTrackerTick is the biggest / only issue. its insanly high for some reason
            base.Tick();
            if (!Spawned) return;
            if (Downed)
            {
                Kill(null);
            }
        }

        public override void TickLong()
        {
            base.TickLong();
            if (!Spawned) return;
            BaseStation.GetDroneSkillsRecord = DroneSkills.UpdateSkills(skills, BaseStation.GetDroneSkillsRecord, skillSettings, true);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            BaseStation?.Notify_DroneMayBeLost(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref BaseStation, "station");
        }
    }
}
