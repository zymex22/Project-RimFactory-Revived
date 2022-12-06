using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    // Deprecated class.
    public class Building_Cultivator : Building_SquareCellIterator
    {

        public CompOutputAdjustable compOutputAdjustable;

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

        #region Abstract stuff
        public override int TickRate => def.GetModExtension<CultivatorDefModExtension>()?.TickFrequencyDivisor ?? 200;

        public override bool CellValidator(IntVec3 c) => base.CellValidator(c) && Utilities.GetIPlantToGrowSettable(c, Map) != null;

        public override bool DoIterationWork(IntVec3 c)
        {
            IPlantToGrowSettable plantToGrowSettable = Utilities.GetIPlantToGrowSettable(c, Map);
            var plantDef = plantToGrowSettable.GetPlantDefToGrow();
            foreach (var t in c.GetThingList(Map))
            {
                if (t is Plant p)
                {
                    if (t.def == plantDef)
                    {
                        if (p.Growth + 0.001f >= 1.00f)
                        {
                            //Harvests fully grown plants
                            CreatePlantProducts(p);
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        //Destroys foreign plants
                        CreatePlantProducts(p);
                        if (!p.Destroyed) p.Destroy();
                        return false;
                    }
                }
            }
            //If no plant of specified type, plants one
            if (plantDef != null && plantToGrowSettable.CanPlantRightNow())
            {
                TryPlantNew(c, plantDef);
            }
            return true;
        }
        #endregion

        public void TryPlantNew(IntVec3 c, ThingDef plantDef)
        {
            if (plantDef.blueprintDef != null && (ProjectRimFactory_ModComponent.ModSupport_SeedsPlease || ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite) && plantDef.blueprintDef.category == ThingCategory.Item)
            {
                //Only Plant if seed is available
                if (!SeedsPleaseSupport.TryPlantNew(plantDef, this.OccupiedRect().ExpandedBy(1),Map)) return;
            }
            if (plantDef.CanEverPlantAt(c, Map) && PlantUtility.AdjacentSowBlocker(plantDef, c, Map) == null)
                GenPlace.TryPlaceThing(ThingMaker.MakeThing(plantDef), c, Map, ThingPlaceMode.Direct);
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

        protected void MakeMatchingGrowZone()
        {
            Designator_ZoneAdd_Growing designator = new Designator_ZoneAdd_Growing();
            designator.DesignateMultiCell(from tempCell in iter.cellPattern
                                          let pos = tempCell + Position
                                          where designator.CanDesignateCell(pos).Accepted
                                          select pos);
        }

        private bool ForbiddenCells(IntVec3 cell)
        {
            if (cell == this.Position) return false;
            return true;
        }

        public virtual void CreatePlantProducts(Plant p)
        {
            int num2 = p.YieldNow();
            if (num2 > 0)
            {
                Thing thing = ThingMaker.MakeThing(p.def.plant.harvestedThingDef, null);
                thing.stackCount = num2;
                GenPlace.TryPlaceThing(thing, compOutputAdjustable.CurrentCell, Map, ThingPlaceMode.Near, null, ForbiddenCells);
            }
            if ((ProjectRimFactory_ModComponent.ModSupport_SeedsPlease || ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite) && p.def.blueprintDef != null)
            {
                SeedsPleaseSupport.CreatePlantSeeds(p, compOutputAdjustable.CurrentCell,Map);
            }

            //TODO 1.3 Maybe rename pawn?
            if (PRFGameComponent.PRF_StaticPawn == null) PRFGameComponent.GenStaticPawn();

            //Cache for PRF_StaticPawn Position & mapIndexOrState
            object PRF_StaticPawnState = null;
            IntVec3 PRF_StaticPawnPos = IntVec3.Invalid;

            if (ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite)
            {
                //Cache PRF_StaticPawn Position & mapIndexOrState
                PRF_StaticPawnState = ReflectionUtility.mapIndexOrState.GetValue(PRFGameComponent.PRF_StaticPawn);
                PRF_StaticPawnPos = PRFGameComponent.PRF_StaticPawn.Position;

                //Set PRF_StaticPawn.Position to the Output Cell -> Sets the placement position for the Seed
                PRFGameComponent.PRF_StaticPawn.Position = compOutputAdjustable.CurrentCell;
                //Set PRF_StaticPawn.mapIndexOrState to this.mapIndexOrState -> needed that PRF_StaticPawn.Map != null
                ReflectionUtility.mapIndexOrState.SetValue(PRFGameComponent.PRF_StaticPawn, ReflectionUtility.mapIndexOrState.GetValue(this));
            }
            p.PlantCollected(PRFGameComponent.PRF_StaticPawn, PlantDestructionMode.Chop);
            if (ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite)
            {
                //Reset PRF_StaticPawn Position & mapIndexOrState
                ReflectionUtility.mapIndexOrState.SetValue(PRFGameComponent.PRF_StaticPawn, PRF_StaticPawnState);
                PRFGameComponent.PRF_StaticPawn.Position= PRF_StaticPawnPos;
            }
        }

        public override string DescriptionDetailed => base.DescriptionDetailed + " " +
            ((ProjectRimFactory_ModComponent.ModSupport_SeedsPlease || ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite) ? "CultivatorTools_SeedsPleaseActiveDesc".Translate() : "CultivatorTools_SeedsPleaseInactiveDesc".Translate());

        public override string DescriptionFlavor => base.DescriptionFlavor + " " +
            ((ProjectRimFactory_ModComponent.ModSupport_SeedsPlease || ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite) ? "CultivatorTools_SeedsPleaseActiveDesc".Translate() : "CultivatorTools_SeedsPleaseInactiveDesc".Translate());
    }

    public class DefModExtension_DoneBehavior : DefModExtension
    {
        public bool hauling = false;
    }

}
