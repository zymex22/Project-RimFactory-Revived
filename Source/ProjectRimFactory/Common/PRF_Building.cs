using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common
{
    public abstract class PRF_Building : Building, IPRF_Building, IProductionSettingsUser
    {
        // If something else wants to trade an item away, we don't
        //   know what we could do with it.
        public virtual bool AcceptsThing(Thing newThing, IPRF_Building giver) => false;
        // If something else wants to take an item from us
        public abstract Thing GetThingBy(Func<Thing, bool> optionalValidator = null);
        public virtual IEnumerable<Thing> AvailableThings => [];

        public virtual void EffectOnPlaceThing(Thing t) { }
        public virtual void EffectOnAcceptThing(Thing t) { }

        public virtual bool ForbidOnPlacing(Thing t) => ForbidOnPlacingDefault;

        public virtual IntVec3 OutputCell()
        {
            return IntVec3.Invalid;
        }

        public virtual bool ForbidOnPlacingDefault
        {
            get => forbidOnPlacingDefault;
            set => forbidOnPlacingDefault = value;
        }

        public virtual bool ObeysStorageFilters
        {
            get => obeysStorageFilters;
            set => obeysStorageFilters = value;
        }
        public virtual bool OutputToEntireStockpile
        {
            get => outputToEntireStockpile;
            set => outputToEntireStockpile = value;
        }
        public virtual PRFBSetting SettingsOptions =>
            PRFBSetting.optionObeysStorageFilters |
            PRFBSetting.optionOutputToEntireStockpie;

        protected bool outputToEntireStockpile;
        protected bool obeysStorageFilters = true;

        private bool forbidOnPlacingDefault;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref outputToEntireStockpile, "PRFOutputToEntireStockpile", false);
            Scribe_Values.Look(ref obeysStorageFilters, "PRFObeysStorageFilters", true);
            Scribe_Values.Look(ref forbidOnPlacingDefault, "PRFForbidOnPlacingDefault", false);
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();

            //Try highlight Output Area
            if (OutputToEntireStockpile && OutputCell() != IntVec3.Invalid)
            { 
                var slotGroup = OutputCell().GetSlotGroup(Map);
                if (slotGroup != null)
                {
                    GenDraw.DrawFieldEdges(slotGroup.CellsList, CommonColors.OutputZone);
                }
            }
        }
    }
    [Flags] // PRF Building Settinsg
    public enum PRFBSetting
    {
        optionOutputToEntireStockpie = 0x1, // if the Settings ITab can change this
        optionObeysStorageFilters = 0x2, // if the Settings ITab can change this

    }
}
