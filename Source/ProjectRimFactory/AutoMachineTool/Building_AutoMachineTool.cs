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
            this.forcePlace = false;
            this.targetEnumrationCount = 0;
        }

        private bool forbidItem = false;

        private SAL_TargetBench salTarget = null;

        ModExtension_Skills extension_Skills;

        public ModExtension_ModifyProduct ModifyProductExt => this.def.GetModExtension<ModExtension_ModifyProduct>();

        public int GetSkillLevel(SkillDef def)
        {
            return extension_Skills?.GetExtendedSkillLevel(def, typeof(Building_AutoMachineTool)) ?? this.SkillLevel ?? 0;
        }

        protected override int? SkillLevel { get { return this.def.GetModExtension<ModExtension_Tier>()?.skillLevel; } }

        public override bool Glowable => false;

        public override void ExposeData()
        {
            Scribe_Deep.Look<SAL_TargetBench>(ref this.salTarget, "salTarget");
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.forbidItem, "forbidItem");

        }

        public bool GetTarget()
        {
            bool verdict = GetTarget(this.Position, this.Rotation, this.Map, true);
            //Alter visuals based on the target
            if (verdict && !(salTarget is SAL_TargetWorktable))
            {
                this.compOutputAdjustable.Visible = false;
                this.powerWorkSetting.RangeSettingHide = true;
            }
            else if (verdict)
            {
                this.compOutputAdjustable.Visible = true;
                this.powerWorkSetting.RangeSettingHide = false;
            }

            return verdict;
        }
        public bool GetTarget(IntVec3 pos, Rot4 rot, Map map, bool spawned = false)
        {

            var buildings = (pos + rot.FacingCell).GetThingList(map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t.InteractionCell == pos);

            Building_WorkTable new_my_workTable = (Building_WorkTable)buildings
                .Where(t => t is Building_WorkTable)
                .FirstOrDefault();
            Building new_drilltypeBuilding = (Building)buildings
                .Where(t => t is Building && t.TryGetComp<CompDeepDrill>() != null)
                .FirstOrDefault();
            Building_ResearchBench new_researchBench = (Building_ResearchBench)buildings
                .Where(t => t is Building_ResearchBench)
                .FirstOrDefault();
            if (spawned)
            {
                if ((salTarget is SAL_TargetWorktable && new_my_workTable == null) || (salTarget is SAL_TargetResearch && new_researchBench == null) || (salTarget is SAL_TargetDeepDrill && new_drilltypeBuilding == null))
                {
                    //Log.Message($"new_my_workTable == null: {new_my_workTable == null}|| new_researchBench == null: {new_researchBench == null}|| new_drilltypeBuilding == null: {new_drilltypeBuilding == null} ");
                    salTarget.Free();
                }
            }
            if (new_my_workTable != null)
            {
                salTarget = new SAL_TargetWorktable(this, this.Position, this.Map, this.Rotation, new_my_workTable);
            }
            else if (new_drilltypeBuilding != null)
            {
                salTarget = new SAL_TargetDeepDrill(this, this.Position, this.Map, this.Rotation, new_drilltypeBuilding);
            }
            else if (new_researchBench != null)
            {
                salTarget = new SAL_TargetResearch(this, this.Position, this.Map, this.Rotation, new_researchBench);
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
            if (salTarget == null && this.State != WorkingState.Ready) this.State = WorkingState.Ready;
            base.SpawnSetup(map, respawningAfterLoad);

            if (salTarget == null)
            {
                GetTarget();
            }
            extension_Skills = def.GetModExtension<ModExtension_Skills>();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            salTarget?.Free();
            base.DeSpawn();
            salTarget = null;
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            this.WorkTableSetting();
        }

        protected override void Reset()
        {
            salTarget?.Reset(this.State);

            base.Reset();
        }

        protected override void CleanupWorkingEffect()
        {
            base.CleanupWorkingEffect();
            salTarget?.CleanupWorkingEffect(this.MapManager);
        }

        protected override void CreateWorkingEffect()
        {
            base.CreateWorkingEffect();
            salTarget?.CreateWorkingEffect(this.MapManager);
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
            if (salTarget == null)
            {
                GetTarget();
                this.Reset();
            }
        }

        protected override void Ready()
        {
            this.WorkTableSetting();
            base.Ready();
        }

        private IntVec3 FacingCell()
        {
            return this.Position + this.Rotation.FacingCell;
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
            if (salTarget == null) GetTarget();

            //Return if not ready
            if (!salTarget.Ready()) return false;
            var res = salTarget.TryStartWork(out workAmount);
            return res;
        }

        protected override bool FinishWorking(Building_AutoMachineTool working, out List<Thing> products)
        {
            salTarget.WorkDone(out products);
            return true;
        }

        public List<IntVec3> OutputZone()
        {
            return this.OutputCell().SlotGroupCells(Map);
        }

        public override IntVec3 OutputCell()
        {
            return compOutputAdjustable.CurrentCell;
        }

        public override IEnumerable<InspectTabBase> GetInspectTabs()
        {
            return base.GetInspectTabs();
        }



        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            return msg;
        }

        public Room GetRoom(RegionType type)
        {
            return RegionAndRoomQuery.GetRoom(this, type);
        }


        private Building_WorkTable GetmyTragetWorktable()
        {
            return (Building_WorkTable)this.FacingCell().GetThingList(Map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Where(t => t is Building_WorkTable)
                .Where(t => t.InteractionCell == this.Position).FirstOrDefault();
        }

        protected override bool WorkInterruption(Building_AutoMachineTool working)
        {
            //Interupt if worktable chenged or is null
            if (salTarget == null || (salTarget is SAL_TargetWorktable && GetmyTragetWorktable() == null /*|| GetmyTragetWorktable() != my_workTable*/))
            {
                return true;
            }
            //Interrupt if worktable is not ready for work
            //if (my_workTable != null) return !my_workTable.CurrentlyUsableForBills();

            var notready = !salTarget.Ready();
            return notready;
        }

    }

}
