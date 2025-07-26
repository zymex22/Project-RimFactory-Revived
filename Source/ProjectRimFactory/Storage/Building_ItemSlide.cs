using ProjectRimFactory.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class Building_ItemSlide : Building_Crate, IPRF_Building
    {
        public IEnumerable<Thing> AvailableThings => throw new NotImplementedException();

        public virtual bool ForbidOnPlacingDefault { get; set; }

        public bool ObeysStorageFilters => false;

        public bool OutputToEntireStockpile => false;

        public bool AcceptsThing(Thing newThing, IPRF_Building giver = null)
        {
            if (!Accepts(newThing) || !CanStoreMoreItems) return false;
            Notify_ReceivedThing(newThing);
            return true;
        }

        public void EffectOnAcceptThing(Thing t)
        {

        }

        public void EffectOnPlaceThing(Thing t)
        {

        }

        //TODO
        public bool ForbidOnPlacing(Thing t)
        {
            if (OutputCell.GetThingList(Map).Any(type => type is IPRF_Building or Building_StorageUnitIOBase or Building_MassStorageUnit))
            {
                return ForbidOnPlacingDefault;
            }
            return true;
        }

        public Thing GetThingBy(Func<Thing, bool> optionalValidator = null)
        {
            throw new NotImplementedException();
        }

        private IntVec3 OutputCell => Position + Rotation.FacingCell;

        private void TrySlideItem()
        {
            var targetIprfBuilding = (IPRF_Building)OutputCell.GetThingList(Map).FirstOrDefault(type => type is IPRF_Building);
            if (targetIprfBuilding != null)
            {
                targetIprfBuilding.AcceptsThing(StoredItems[0], this);
            }
            else
            {
                this.PRFTryPlaceThing(StoredItems[0], OutputCell, Map);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;

            if (StoredItemsCount > 0)
            {
                TrySlideItem();
            }


        }

    }

    // ReSharper disable once UnusedType.Global
    class PlaceWorker_ItemSlide : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, 
            Thing thingToIgnore = null, Thing thing = null)
        {
            var acceptanceBase = base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore, thing);
            if (!acceptanceBase.Accepted) return acceptanceBase;
            
            //Check if the traget is another slide
            var outputCell = loc + rot.FacingCell;
            var thingList = map.thingGrid.ThingsListAt(outputCell);
            if (thingList.Any(t => t is Building_ItemSlide || t.def.entityDefToBuild == checkingDef))
            {
                return new AcceptanceReport("PRF_PlaceWorker_ItemSlide_Denied".Translate());
            }

            //Check if there is a slide placing there
            if (GenAdj.CellsAdjacentCardinal(loc, rot, checkingDef.Size)
                .Any(c => map.thingGrid.ThingsListAt(c)
                    .Any(t => (t is Building_ItemSlide || t.def.entityDefToBuild == checkingDef) && t.Position + t.Rotation.FacingCell == loc)))
            {
                return new AcceptanceReport("PRF_PlaceWorker_ItemSlide_Denied".Translate());
            }

            return acceptanceBase;
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var outputCell = center + rot.FacingCell;
            GenDraw.DrawFieldEdges([outputCell], CommonColors.OutputCell);

        }
    }
}
