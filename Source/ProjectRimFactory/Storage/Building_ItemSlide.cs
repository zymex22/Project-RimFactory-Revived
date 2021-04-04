using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectRimFactory;
using ProjectRimFactory.Common;
using Verse;
using UnityEngine;

namespace ProjectRimFactory.Storage
{
    class Building_ItemSlide : Building_Crate, IPRF_Building
    {
        public IEnumerable<Thing> AvailableThings => throw new NotImplementedException();

        public bool ObeysStorageFilters => false;

        public bool OutputToEntireStockpile => false;

        public bool AcceptsThing(Thing newThing, IPRF_Building giver = null)
        {
            
            if (base.Accepts(newThing)){
                Notify_ReceivedThing(newThing);
                return true;
            }
            else
            {
                return false;
            }
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
            if (outputCell.GetThingList(Map).Where(type => type is IPRF_Building || type is Building_StorageUnitIOBase || type is Building_MassStorageUnit).Any<Thing>())
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public Thing GetThingBy(Func<Thing, bool> optionalValidator = null)
        {
            throw new NotImplementedException();
        }

        private IntVec3 outputCell => this.Position + this.Rotation.FacingCell;

        private void trySlideItem()
        {
            IPRF_Building target_IPRF_Building = (IPRF_Building)outputCell.GetThingList(Map).Where(type => type is IPRF_Building).FirstOrDefault<Thing>();
            if (target_IPRF_Building != null)
            {
                target_IPRF_Building.AcceptsThing(StoredItems[0], this);
            }
            else
            {
                this.PRFTryPlaceThing(StoredItems[0], outputCell, this.Map);
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (!this.Spawned) return;

            if (this.StoredItemsCount > 0)
            {
                trySlideItem();
            }


        }

    }

    class PlaceWorker_ItemSlide : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {

            IntVec3 outputCell = center + rot.FacingCell;


            GenDraw.DrawFieldEdges(new List<IntVec3> { outputCell }, Common.CommonColors.outputCell);

        }
    }
}
