using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common
{
    public abstract class PRF_Building : Building, IPRF_Building
    {
        protected bool obeysStorageFilters = true;

        protected bool outputToEntireStockpile;

        public virtual PRFBSetting SettingsOptions =>
            PRFBSetting.optionObeysStorageFilters |
            PRFBSetting.optionOutputToEntireStockpie;

        // If something else wants to trade an item away, we don't
        //   know what we could do with it.
        public virtual bool AcceptsThing(Thing newThing, IPRF_Building giver)
        {
            return false;
        }

        // If something else wants to take an item from us
        public abstract Thing GetThingBy(Func<Thing, bool> optionalValidator = null);

        public virtual IEnumerable<Thing> AvailableThings => Enumerable.Empty<Thing>();

        public virtual void EffectOnPlaceThing(Thing t)
        {
        }

        public virtual void EffectOnAcceptThing(Thing t)
        {
        }

        //TODO: make this like the next two?
        public virtual bool ForbidOnPlacing(Thing t)
        {
            return false;
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref outputToEntireStockpile, "PRFOutputToEntireStockpile");
            Scribe_Values.Look(ref obeysStorageFilters, "PRFObeysStorageFilters", true);
        }
    }

    [Flags] // PRF Building Settinsg
    public enum PRFBSetting
    {
        optionOutputToEntireStockpie = 0x1, // if the Settings ITab can change this
        optionObeysStorageFilters = 0x2 // if the Settings ITab can change this
    }
}