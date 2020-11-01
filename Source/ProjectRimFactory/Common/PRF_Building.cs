using System;
using Verse;
using System.Collections.Generic;
using System.Linq;
namespace ProjectRimFactory.Common
{
    public abstract class PRF_Building : Building, IPRF_Building {
        // If something else wants to trade an item away, we don't
        //   know what we could do with it.
        public virtual bool AcceptsThing(Thing newThing, IPRF_Building giver) => false;
        // If something else wants to take an item from us
        public abstract Thing GetThingBy(Func<Thing, bool> optionalValidator = null);
        public virtual IEnumerable<Thing> AvailableThings {
            get => Enumerable.Empty<Thing>();
        }

        public virtual void EffectOnPlaceThing(Thing t) { }
        public virtual void EffectOnAcceptThing(Thing t) { }

        //TODO: make this like the next two?
        public virtual bool ForbidOnPlacing(Thing t) => false;
        public virtual bool ObeysStorageFilters {
            get => obeysStorageFilters;
            set => obeysStorageFilters = value;
        }
        public virtual bool OutputToEntireStockpile {
            get => outputToEntireStockpile;
            set => outputToEntireStockpile = value;
        }
        public virtual PRFBSetting SettingsOptions {
            get => PRFBSetting.optionObeysStorageFilters |
                PRFBSetting.optionOutputToEntireStockpie;
        }

        protected bool outputToEntireStockpile=false;
        protected bool obeysStorageFilters = true;

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref outputToEntireStockpile, "PRFOutputToEntireStockpile", false);
            Scribe_Values.Look(ref obeysStorageFilters, "PRFObeysStorageFilters", true);
        }
    }
    [Flags] // PRF Building Settinsg
    public enum PRFBSetting {
        optionOutputToEntireStockpie = 0x1, // if the Settings ITab can change this
        optionObeysStorageFilters    = 0x2, // if the Settings ITab can change this

    }
}
