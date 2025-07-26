using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    internal static class SeedsPleaseSupport
    {

        public static CellRect InputArea(Building building)
        {
            return building.OccupiedRect().ExpandedBy(1);
        }

        public static bool TryPlantNew(ThingDef plantDef, CellRect seedInputArea, Map map)
        {
            if (ProjectRimFactory_ModComponent.ModSupport_SeedsPlease || ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite)
            {
                return tryPlantNew(plantDef, seedInputArea, map);
            }

            //No Seeds Mod Active
            return true;
            
        }

        /// <summary>
        /// Check if a seed is available
        /// If yes Use that seed to Plant
        /// else don't plant
        /// </summary>
        /// <param name="plantDef"></param>
        /// <param name="seedInputArea"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        private static bool tryPlantNew(ThingDef plantDef,CellRect seedInputArea,Map map)
        {
            //Search for seeds in SeedInputArea
            Thing seed = null;
            foreach (var cell in seedInputArea)
            {
                seed = cell.GetThingList(map).Find(t => t.def == plantDef.blueprintDef);
                if (seed != null) break;
            }
            if (seed == null) return false;

            //remove the Seed
            seed.stackCount--;
            if (seed.stackCount <= 0)
            {
                seed.Destroy();
            }

            return true;
        }

        /// <summary>
        /// Spawns Seeds form harvesting Plants
        /// </summary>
        /// <param name="p"></param>
        /// <param name="outputCell"></param>
        /// <param name="map"></param>
        public static void CreatePlantSeeds(Plant p, IntVec3 outputCell, Map map)
        {
            if (ProjectRimFactory_ModComponent.ModSupport_SeedsPlease)
            {
                CreatePlantSeeds_SeedsPlease(p, outputCell, map);
            }
            /*
             * For ProjectRimFactory_ModComponent.ModSupport_SeedsPleaseLite
             * we use it's Patch on PlantCollected
             * For Compatibility we update the PRF_StaticPawn's Position & mapIndexOrState for the call then reset it to how it was before
            */
        }

        /// <summary>
        /// SeedsPlease activated code for creating plant products, credit to notfood for original mod
        /// </summary>
        private static void CreatePlantSeeds_SeedsPlease(Plant p, IntVec3 outputCell,Map map)
        {
            var seed = p.def.blueprintDef;
            var type = seed.GetType();
            var props = type.GetField("seed").GetValue(seed);
            var propType = props.GetType();
            int count;
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
                GenSpawn.Spawn(thing, outputCell, map);
            }
        }

    }
}
