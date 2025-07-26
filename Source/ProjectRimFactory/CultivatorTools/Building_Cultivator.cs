using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.SAL3.Tools;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    // ReSharper disable once UnusedType.Global
    public class Building_Cultivator : Building_SquareCellIterator
    {
        private CompOutputAdjustable compOutputAdjustable;

        public override void PostMake()
        {
            base.PostMake();
            compOutputAdjustable = GetComp<CompOutputAdjustable>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compOutputAdjustable = GetComp<CompOutputAdjustable>();
        }

        protected override int TickRate => CultivatorDefModExtension?.TickFrequencyDivisor ?? 200;

        protected override bool CellValidator(IntVec3 cell) => 
            base.CellValidator(cell) && Utilities.GetIPlantToGrowSettable(cell, Map) != null;

        protected override bool DoIterationWork(IntVec3 cell)
        {
            var plantToGrowSettable = Utilities.GetIPlantToGrowSettable(cell, Map);
            var plantDef = plantToGrowSettable.GetPlantDefToGrow();
            foreach (var thing in cell.GetThingList(Map))
            {
                if (thing is not Plant p) continue;
                if (thing.def == plantDef)
                {
                    
                    if (p.Growth + 0.001f >= 1.00f)
                    {
                        //Harvests fully grown plants
                        CreatePlantProducts(p);
                        return false;
                    }
                    return true;
                }
                //Destroys foreign plants
                CreatePlantProducts(p);
                if (!p.Destroyed) p.Destroy();
                return false;
            }
            //If no plant of specified type, plants one
            if (plantDef != null && plantToGrowSettable.CanPlantRightNow())
            {
                TryPlantNew(cell, plantDef);
            }
            return true;
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if(SeedsPleaseSupportActive)
            {
                //CommonColors.SeedsInputZone
                GenDraw.DrawFieldEdges(SeedsPleaseSupport.InputArea(this).Cells.ToList(),CommonColors.SeedsInputZone);
            }
            
        }

        private static bool SeedsPleaseSupportActive => ProjectRimFactory_ModComponent.ModSupport_SeedsPlease ||
                                                        ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite;

        private static bool SeedLiteDualCropSupportActive =>
            ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite ||
            ProjectRimFactory_ModComponent.ModSupport_VEF_DualCropExtension;
        
        private void TryPlantNew(IntVec3 c, ThingDef plantDef)
        {
            if (plantDef.blueprintDef != null && SeedsPleaseSupportActive && plantDef.blueprintDef.category == ThingCategory.Item)
            {
                //Only Plant if seed is available
                if (!SeedsPleaseSupport.TryPlantNew(plantDef, SeedsPleaseSupport.InputArea(this), Map)) return;
            }
            if (plantDef.CanEverPlantAt(c, Map) && PlantUtility.AdjacentSowBlocker(plantDef, c, Map) == null)
            {
                GenPlace.TryPlaceThing(ThingMaker.MakeThing(plantDef), c, Map, ThingPlaceMode.Direct);
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
            designator.DesignateMultiCell(from tempCell in Iter.CellPattern
                                          let pos = tempCell + Position
                                          where designator.CanDesignateCell(pos).Accepted
                                          select pos);
        }

        private bool ForbiddenCells(IntVec3 cell)
        {
            // Not on self
            return cell != Position;
        }

        protected virtual void CreatePlantProducts(Plant p)
        {
            var num2 = p.YieldNow();
            if (num2 > 0)
            {
                var thing = ThingMaker.MakeThing(p.def.plant.harvestedThingDef, null);
                thing.stackCount = num2;
                GenPlace.TryPlaceThing(thing, compOutputAdjustable.CurrentCell, Map, ThingPlaceMode.Near, null, ForbiddenCells);
            }
            if (SeedsPleaseSupportActive && p.def.blueprintDef != null)
            {
                SeedsPleaseSupport.CreatePlantSeeds(p, compOutputAdjustable.CurrentCell,Map);
            }

            //TODO 1.3 Maybe rename pawn?
            if (PRFGameComponent.PRF_StaticPawn == null) PRFGameComponent.GenStaticPawn();

            //Cache for PRF_StaticPawn Position & mapIndexOrState
            object prfStaticPawnState = null;
            var prfStaticPawnPos = IntVec3.Invalid;

            if (SeedLiteDualCropSupportActive)
            {
                //Cache PRF_StaticPawn Position & mapIndexOrState
                prfStaticPawnState = ReflectionUtility.MapIndexOrState.GetValue(PRFGameComponent.PRF_StaticPawn);
                prfStaticPawnPos = PRFGameComponent.PRF_StaticPawn!.Position;

                //Set PRF_StaticPawn.Position to the Output Cell -> Sets the placement position for the Seed
                PRFGameComponent.PRF_StaticPawn.Position = compOutputAdjustable.CurrentCell;
                //Set PRF_StaticPawn.mapIndexOrState to this.mapIndexOrState -> needed that PRF_StaticPawn.Map != null
                ReflectionUtility.MapIndexOrState.SetValue(PRFGameComponent.PRF_StaticPawn, ReflectionUtility.MapIndexOrState.GetValue(this));
            }
            p.PlantCollected(PRFGameComponent.PRF_StaticPawn, PlantDestructionMode.Chop);
            if (SeedLiteDualCropSupportActive)
            {
                //Reset PRF_StaticPawn Position & mapIndexOrState
                ReflectionUtility.MapIndexOrState.SetValue(PRFGameComponent.PRF_StaticPawn, prfStaticPawnState);
                PRFGameComponent.PRF_StaticPawn!.Position= prfStaticPawnPos;
            }
        }

        public override string DescriptionDetailed => base.DescriptionDetailed + " " +
            (SeedsPleaseSupportActive ? "CultivatorTools_SeedsPleaseActiveDesc".Translate() : "CultivatorTools_SeedsPleaseInactiveDesc".Translate());

        public override string DescriptionFlavor => base.DescriptionFlavor + " " +
            (SeedsPleaseSupportActive ? "CultivatorTools_SeedsPleaseActiveDesc".Translate() : "CultivatorTools_SeedsPleaseInactiveDesc".Translate());
    }

    public class DefModExtension_DoneBehavior : DefModExtension
    {
        public bool hauling = false;
    }

}
