using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{

    public class Building_AutoMachineTool : Building_BaseRange<Building_AutoMachineTool>, IRecipeProductWorker
    {

        public override bool ProductLimitationDisable => true;

        public Building_AutoMachineTool()
        {
            ForcePlace = false;
            targetEnumrationCount = 0;
        }

        private bool forbidItem = false;

        private SAL_TargetBench salTarget = null;

        ModExtension_Skills extension_Skills;

        public ModExtension_ModifyProduct ModifyProductExt => def.GetModExtension<ModExtension_ModifyProduct>();

        public int GetSkillLevel(SkillDef skillDef)
        {
            return extension_Skills?.GetExtendedSkillLevel(skillDef, typeof(Building_AutoMachineTool)) ?? SkillLevel ?? 0;
        }

        protected override int? SkillLevel => def.GetModExtension<ModExtension_Tier>()?.skillLevel;

        public override bool Glowable => false;

        public override void ExposeData()
        {
            Scribe_Deep.Look<SAL_TargetBench>(ref salTarget, "salTarget");
            base.ExposeData();
            Scribe_Values.Look<bool>(ref forbidItem, "forbidItem");

        }
        
        // TODO: refactor. calls GetTarget(Position, Rotation, Map, true) This has side-effects
        private void UpdateVisuals()
        {
            var hasTarget = GetTarget(Position, Rotation, Map, true);
            //Alter visuals based on the target
            if (!hasTarget) return;
            if (salTarget is not SAL_TargetWorktable)
            {
                CompOutputAdjustable.Visible = false;
                PowerWorkSetting.RangeSettingHide = true;
            }
            else
            {
                CompOutputAdjustable.Visible = true;
                PowerWorkSetting.RangeSettingHide = false;
            }
        }
        
        // TODO that's ugly. This needs a refactor
        public bool GetTarget(IntVec3 pos, Rot4 rot, Map map, bool spawned = false)
        {

            var buildings = (pos + rot.FacingCell).GetThingList(map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t.InteractionCell == pos).ToList();

            
            var newMyWorkTable = (Building_WorkTable)buildings.FirstOrDefault(t => t is Building_WorkTable);
            var new_drilltypeBuilding = (Building)buildings.FirstOrDefault(t => t is Building && t.TryGetComp<CompDeepDrill>() != null);
            var new_researchBench = (Building_ResearchBench)buildings.FirstOrDefault(t => t is Building_ResearchBench);
            
            if (spawned)
            {
                if ((salTarget is SAL_TargetWorktable && newMyWorkTable == null) || (salTarget is SAL_TargetResearch && new_researchBench == null) || (salTarget is SAL_TargetDeepDrill && new_drilltypeBuilding == null))
                {
                    //Log.Message($"new_my_workTable == null: {new_my_workTable == null}|| new_researchBench == null: {new_researchBench == null}|| new_drilltypeBuilding == null: {new_drilltypeBuilding == null} ");
                    salTarget.Free();
                }
            }
            if (newMyWorkTable != null)
            {
                salTarget = new SAL_TargetWorktable(this, Position, Map, Rotation, newMyWorkTable);
            }
            else if (new_drilltypeBuilding != null)
            {
                salTarget = new SAL_TargetDeepDrill(this, Position, Map, Rotation, new_drilltypeBuilding);
            }
            else if (new_researchBench != null)
            {
                salTarget = new SAL_TargetResearch(this, Position, Map, Rotation, new_researchBench);
            }
            else
            {
                salTarget = null;
            }

            if (spawned && salTarget != null) salTarget.Reserve();

            return salTarget != null;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            if (salTarget == null && State != WorkingState.Ready) State = WorkingState.Ready;
            base.SpawnSetup(map, respawningAfterLoad);

            if (salTarget is null)
            {
                UpdateVisuals();
            }
            extension_Skills = def.GetModExtension<ModExtension_Skills>();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            salTarget?.Free();
            base.DeSpawn(mode);
            salTarget = null;
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            WorkTableSetting();
        }

        protected override void Reset()
        {
            salTarget?.Reset(State);

            base.Reset();
        }

        protected override void CleanupWorkingEffect()
        {
            base.CleanupWorkingEffect();
            salTarget?.CleanupWorkingEffect(MapManager);
        }

        protected override void CreateWorkingEffect()
        {
            base.CreateWorkingEffect();
            salTarget?.CreateWorkingEffect(MapManager);
        }



        protected override TargetInfo ProgressBarTarget()
        {
            return salTarget?.TargetInfo() ?? TargetInfo.Invalid;
        }

        /// <summary>
        /// TODO Check that one again
        /// </summary>
        private void WorkTableSetting()
        {
            if (salTarget != null) return;
            UpdateVisuals();
            Reset();
        }

        protected override void Ready()
        {
            WorkTableSetting();
            base.Ready();
        }

        private IntVec3 FacingCell()
        {
            return Position + Rotation.FacingCell;
        }




        /// <summary>
        /// Try to start a new Bill to work on
        /// </summary>
        /// <param name="target"></param>
        /// <param name="workAmount"></param>
        /// <returns></returns>
        protected override bool TryStartWorking(out Building_AutoMachineTool target, out float workAmount)
        {
            target = this;
            workAmount = 0;
            if (salTarget == null) UpdateVisuals();

            //Return if not ready
            if (salTarget is null || !salTarget.Ready()) return false;
            return salTarget.TryStartWork(out workAmount);
        }

        protected override bool FinishWorking(Building_AutoMachineTool _, out List<Thing> outputProducts)
        {
            salTarget.WorkDone(out outputProducts);
            return true;
        }

        public override IntVec3 OutputCell()
        {
            return CompOutputAdjustable.CurrentCell;
        }
        
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }
        }

        public Room GetRoom(RegionType type)
        {
            return RegionAndRoomQuery.GetRoom(this, type);
        }


        private bool HasTargetWorktable()
        {
            return FacingCell().GetThingList(Map)
                .Where(t => t.def.category is ThingCategory.Building && t is Building_WorkTable)
                .Any(t => t.InteractionCell == Position);
        }

        protected override bool WorkInterruption(Building_AutoMachineTool _)
        {
            //Interrupt if worktable changed or is null
            if (salTarget is null || (salTarget is SAL_TargetWorktable && !HasTargetWorktable()))
            {
                return true;
            }
            //Interrupt if worktable is not ready for work
            return !salTarget.Ready();
        }

    }

}
