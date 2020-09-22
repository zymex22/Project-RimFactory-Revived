using System;
using Verse;
namespace ProjectRimFactory.Common
{
    public abstract class PRF_Building : Building, IPRF_Building {
        // If something else wants to trade an item away, we don't
        //   know what we could do with it.
        public virtual bool AcceptsThing(Thing newThing, IPRF_Building giver) => false;
        // If something else wants to take an item from us
        public abstract Thing GetThingBy(Func<Thing, bool> optionalValidator = null);


        public virtual bool ForbidOnPlacing() => false;
        // List<Thing> AvailableThings(); // maybe?
        public virtual bool ObeysStorageFilters { get => true; }
        public virtual void EffectOnPlaceThing(Thing t) { }
        public virtual void EffectOnAcceptThing(Thing t) { }


    }
}
