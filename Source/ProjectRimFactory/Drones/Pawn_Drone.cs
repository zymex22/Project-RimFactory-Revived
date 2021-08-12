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
using System.Diagnostics;

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

#if DEBUG
        Stopwatch sw_Comps = new Stopwatch();
        Stopwatch sw_Rare = new Stopwatch();
        Stopwatch sw_jobs = new Stopwatch();
        Stopwatch sw_p1 = new Stopwatch();
        Stopwatch sw_p2 = new Stopwatch();
        Stopwatch sw_p3 = new Stopwatch();

#endif


        private void baseTick()
        {
            //if (DebugSettings.noAnimals && base.Spawned && RaceProps.Animal)
            //{
            //    Destroy();
            //    return;
            //}
            //((ThingWithComps)this).Tick();
#if DEBUG
            sw_Comps.Restart();
#endif
            if (AllComps != null)
            {
                int i = 0;
                for (int count = AllComps.Count; i < count; i++)
                {
                    AllComps[i].CompTick();
                }
            }
#if DEBUG
            sw_Comps.Stop();
            sw_Rare.Restart();
#endif
            if (Find.TickManager.TicksGame % 250 == 0)
			{
				TickRare();
			}
#if DEBUG
            sw_Rare.Stop();
            sw_p1.Restart();
#endif
            //bool suspended = base.Suspended;
            if (!false)
			{
                if (base.Spawned)
                {
                    pather.PatherTick();
                }
                if (base.Spawned)
				{
					stances.StanceTrackerTick();
					verbTracker.VerbsTick();
				}

                if (base.Spawned)
				{
					roping.RopingTick();
					natives.NativeVerbsTick();
				}

#if DEBUG
                sw_p1.Stop();
                sw_jobs.Restart();
#endif
                //This is requiered
                if (base.Spawned)
				{
					jobs.JobTrackerTick();
				}
#if DEBUG
                sw_jobs.Stop();
                sw_p2.Restart();
#endif




                if (base.Spawned)
				{
					Drawer.DrawTrackerTick();
					rotationTracker.RotationTrackerTick();
				}
                health.HealthTick();
				if (!Dead)
				{
					mindState.MindStateTick();
					carryTracker.CarryHandsTick();
				}
#if DEBUG
                sw_p2.Stop();
                sw_p3.Restart();
#endif

            }

            //if (!Dead)
            //{
            //	needs.NeedsTrackerTick();
            //}
            if (!false)
			{
    //            if (equipment != null)
    //            {
    //                equipment.EquipmentTrackerTick();
    //            }
    //            if (apparel != null)
    //            {
    //                apparel.ApparelTrackerTick();
    //            }
    //            if (interactions != null && base.Spawned)
				//{
				//	interactions.InteractionsTrackerTick();
				//}
				if (caller != null)
				{
					caller.CallTrackerTick();
				}
				//if (skills != null)
				//{
				//	skills.SkillsTick();
				//}
				//if (abilities != null)
				//{
				//	abilities.AbilitiesTick();
				//}
				if (inventory != null)
				{
					inventory.InventoryTrackerTick();
				}
                //if (drafter != null)
                //{
                //    drafter.DraftControllerTick();
                //}
                //if (relations != null)
                //{
                //    relations.RelationsTrackerTick();
                //}
                //if (ModsConfig.RoyaltyActive && psychicEntropy != null)
                //{
                //    psychicEntropy.PsychicEntropyTrackerTick();
                //}
                //if (RaceProps.Humanlike)
                //{
                //    guest.GuestTrackerTick();
                //}
                //if (ideo != null)
                //{
                //    ideo.IdeoTrackerTick();
                //}
                //if (royalty != null && ModsConfig.RoyaltyActive)
                //{
                //    royalty.RoyaltyTrackerTick();
                //}
                //if (style != null && ModsConfig.IdeologyActive)
                //{
                //    style.StyleTrackerTick();
                //}
                //if (styleObserver != null && ModsConfig.IdeologyActive)
                //{
                //    styleObserver.StyleObserverTick();
                //}
                //if (surroundings != null && ModsConfig.IdeologyActive)
                //{
                //    surroundings.SurroundingsTrackerTick();
                //}
                //ageTracker.AgeTick();
                //records.RecordsTick();
            }
            //guilt?.GuiltTrackerTick();

#if DEBUG
            sw_p3.Stop();
            
            if (sw_p3.ElapsedMilliseconds > 2 || sw_p2.ElapsedMilliseconds > 2 || sw_p1.ElapsedMilliseconds > 2 || sw_jobs.ElapsedMilliseconds > 2 || sw_Rare.ElapsedMilliseconds > 2 || sw_Comps.ElapsedMilliseconds > 2)
            {
                Log.Message("sw_p1: " + sw_p1.Elapsed.TotalMilliseconds + " ms  | " + "sw_p2: " + sw_p2.Elapsed.TotalMilliseconds + " ms  | " + "sw_p3: " + sw_p3.Elapsed.TotalMilliseconds + " ms  | " +
                    "sw_Comps: " + sw_Comps.Elapsed.TotalMilliseconds + " ms  | " +
                    "sw_jobs: " + sw_jobs.Elapsed.TotalMilliseconds + " ms  | " +
                    "sw_Rare: " + sw_Rare.Elapsed.TotalMilliseconds + " ms  | ");
            }

#endif

        }

        public override void Tick()
        {
            //This is an issue
            //from what i understand base.base is not a option
            //This means that i am limited in what i can remove

            //JobTrackerTick is the biggest / only issue. its insanly high for some reason
            //base.Tick();
            baseTick();



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
