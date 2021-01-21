using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using UnityEngine;
using RimWorld;
using ProjectRimFactory.Common;

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
            if (plantDef.blueprintDef != null && Utilities.SeedsPleaseActive && plantDef.blueprintDef.category == ThingCategory.Item)
            {
                if (!TryPlantNewSeedsPleaseActive(plantDef))
                    return;
            }
            if (plantDef.CanEverPlantAt(c, Map) && PlantUtility.AdjacentSowBlocker(plantDef, c, Map) == null)
                GenPlace.TryPlaceThing(ThingMaker.MakeThing(plantDef), c, Map, ThingPlaceMode.Direct);
        }

        #region SeedsPlease activated stuff
        /// <summary>
        /// SeedsPlease activated code for trying to take seeds, credit to notfood for original mod
        /// </summary>
        private bool TryPlantNewSeedsPleaseActive(ThingDef plantDef)
        {
            var detectorCells = this.OccupiedRect().ExpandedBy(1);
            Thing seed = null;
            foreach (var cell in detectorCells)
            {
                var temp = cell.GetThingList(Map).Find(t => t.def == plantDef.blueprintDef);
                if (temp != null)
                {
                    seed = temp;
                    break;
                }
            }
            if (seed == null)
            {
                return false;
            }
            seed.stackCount--;
            if (seed.stackCount <= 0)
            {
                seed.Destroy();
            }
            return true;
        }

        /// <summary>
        /// SeedsPlease activated code for creating plant products, credit to notfood for original mod
        /// </summary>
        protected void CreatePlantProductsSeedsPleaseActive(Plant p)
        {
            var seed = p.def.blueprintDef;
            var type = seed.GetType();
            var props = type.GetField("seed").GetValue(seed);
            var propType = props.GetType();
            int count = 0;
            //This section of code adapted of notfood's original source
            float parameter = Mathf.Max(Mathf.InverseLerp(p.def.plant.harvestMinGrowth, 1.2f, p.Growth), 1f);
            if ((float)propType.GetField("seedFactor").GetValue(props) > 0f && Rand.Value < (float)propType.GetField("baseChance").GetValue(props) * parameter)
            {
                if (Rand.Value < (float)propType.GetField("extraChance").GetValue(props))
                {
                    count = 2;
                }
                else
                {
                    count = 1;
                }
                var thing = ThingMaker.MakeThing(seed);
                thing.stackCount = count;
                GenSpawn.Spawn(thing, compOutputAdjustable.CurrentCell, Map);
            }
        }
        #endregion

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
            if (Utilities.SeedsPleaseActive && p.def.blueprintDef != null)
                CreatePlantProductsSeedsPleaseActive(p);

            p.PlantCollected();
        }

        public override string DescriptionDetailed => base.DescriptionDetailed + " " +
            ((Utilities.SeedsPleaseActive) ? "CultivatorTools_SeedsPleaseActiveDesc".Translate() : "CultivatorTools_SeedsPleaseInactiveDesc".Translate());

        public override string DescriptionFlavor => base.DescriptionFlavor + " " +
            ((Utilities.SeedsPleaseActive) ? "CultivatorTools_SeedsPleaseActiveDesc".Translate() : "CultivatorTools_SeedsPleaseInactiveDesc".Translate());
    }

    public class DefModExtension_DoneBehavior : DefModExtension
    {
        public bool hauling = false;
    }

}
